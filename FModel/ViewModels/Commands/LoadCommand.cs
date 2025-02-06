using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdonisUI.Controls;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.Views.Resources.Controls;
using K4os.Compression.LZ4.Streams;
using Microsoft.Win32;

namespace FModel.ViewModels.Commands;

/// <summary>
/// this will always load all files no matter the loading mode
/// however what this does is filtering what to show to the user
/// </summary>
public class LoadCommand : ViewModelCommand<LoadingModesViewModel>
{
    private const uint _IS_LZ4 = 0x184D2204u;

    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
    private DiscordHandler _discordHandler => DiscordService.DiscordHandler;

    public LoadCommand(LoadingModesViewModel contextViewModel) : base(contextViewModel) { }

    public override async void Execute(LoadingModesViewModel contextViewModel, object parameter)
    {
        if (_applicationView.CUE4Parse.Provider.Keys.Count == 0 && _applicationView.CUE4Parse.Provider.RequiredKeys.Count > 0)
        {
            FLogger.Append(ELog.Error, () =>
                FLogger.Text("An encrypted archive has been found. In order to decrypt it, please specify a working AES encryption key", Constants.WHITE, true));
            return;
        }
        if (_applicationView.CUE4Parse.Provider.Files.Count == 0)
        {
            FLogger.Append(ELog.Error, () => FLogger.Text("No files were found in the archives or the specified directory", Constants.WHITE, true));
            return;
        }

#if DEBUG
        var loadingTime = Stopwatch.StartNew();
#endif
        _applicationView.CUE4Parse.AssetsFolder.Folders.Clear();
        _applicationView.CUE4Parse.SearchVm.SearchResults.Clear();
        MainWindow.YesWeCats.LeftTabControl.SelectedIndex = 1; // folders tab
        Helper.CloseWindow<AdonisWindow>("Search View"); // close search window if opened

        await Task.WhenAll(
            _applicationView.CUE4Parse.LoadLocalizedResources(), // load locres if not already loaded,
            _applicationView.CUE4Parse.LoadVirtualPaths(), // load virtual paths if not already loaded
            _threadWorkerView.Begin(cancellationToken =>
            {
                // filter what to show
                _applicationView.Status.UpdateStatusLabel("Packages", "Filtering");
                switch (UserSettings.Default.LoadingMode)
                {
                    case ELoadingMode.Multiple:
                    {
                        var l = (IList) parameter;
                        if (l.Count < 1) return;

                        var directoryFilesToShow = l.Cast<FileItem>();
                        FilterDirectoryFilesToDisplay(cancellationToken, directoryFilesToShow);
                        break;
                    }
                    case ELoadingMode.All:
                    {
                        FilterDirectoryFilesToDisplay(cancellationToken, null);
                        break;
                    }
                    case ELoadingMode.AllButNew:
                    case ELoadingMode.AllButModified:
                    {
                        FilterNewOrModifiedFilesToDisplay(cancellationToken);
                        break;
                    }
                    default: throw new ArgumentOutOfRangeException();
                }

                _discordHandler.UpdatePresence(_applicationView.CUE4Parse);
            })
        ).ConfigureAwait(false);
#if DEBUG
        loadingTime.Stop();
        FLogger.Append(ELog.Debug, () =>
            FLogger.Text($"{_applicationView.CUE4Parse.SearchVm.SearchResults.Count} packages loaded in {loadingTime.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)} seconds", Constants.WHITE, true));
#endif
    }

    private void FilterDirectoryFilesToDisplay(CancellationToken cancellationToken, IEnumerable<FileItem> directoryFiles)
    {
        HashSet<string> filter;
        if (directoryFiles == null) filter = null;
        else
        {
            filter = [];
            foreach (var directoryFile in directoryFiles)
            {
                if (!directoryFile.IsEnabled) continue;
                filter.Add(directoryFile.Name);
            }
        }

        var hasFilter = filter != null && filter.Count != 0;
        var entries = new List<GameFile>();

        foreach (var asset in _applicationView.CUE4Parse.Provider.Files.Values)
        {
            cancellationToken.ThrowIfCancellationRequested(); // cancel if needed
            if (asset.IsUePackagePayload) continue;

            if (hasFilter)
            {
                if (asset is VfsEntry entry && filter.Contains(entry.Vfs.Name))
                {
                    entries.Add(asset);
                }
            }
            else
            {
                entries.Add(asset);
            }
        }

        _applicationView.Status.UpdateStatusLabel($"{entries.Count:### ### ###} Packages");
        _applicationView.CUE4Parse.AssetsFolder.BulkPopulate(entries);
    }

    private void FilterNewOrModifiedFilesToDisplay(CancellationToken cancellationToken)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select a backup file older than your current game version",
            InitialDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Backups"),
            Filter = "FBKP Files (*.fbkp)|*.fbkp|All Files (*.*)|*.*",
            Multiselect = false
        };

        if (!openFileDialog.ShowDialog().GetValueOrDefault()) return;

        FLogger.Append(ELog.Information, () =>
            FLogger.Text($"Backup file older than current game is '{openFileDialog.FileName.SubstringAfterLast("\\")}'", Constants.WHITE, true));

        var mode = UserSettings.Default.LoadingMode;
        var entries = ParseBackup(openFileDialog.FileName, mode, cancellationToken);

        _applicationView.Status.UpdateStatusLabel($"{entries.Count:### ### ###} Packages");
        _applicationView.CUE4Parse.AssetsFolder.BulkPopulate(entries);
    }

    private List<GameFile> ParseBackup(string path, ELoadingMode mode, CancellationToken cancellationToken = default)
    {
        using var fileStream = new FileStream(path, FileMode.Open);
        using var memoryStream = new MemoryStream();

        if (fileStream.ReadUInt32() == _IS_LZ4)
        {
            fileStream.Position -= 4;
            using var compressionStream = LZ4Stream.Decode(fileStream);
            compressionStream.CopyTo(memoryStream);
        }
        else fileStream.CopyTo(memoryStream);

        memoryStream.Position = 0;
        using var archive = new FStreamArchive(fileStream.Name, memoryStream);
        var entries = new List<GameFile>();

        switch (mode)
        {
            case ELoadingMode.AllButNew:
            {
                var paths = new HashSet<string>();
                var magic = archive.Read<uint>();
                if (magic != BackupManagerViewModel.FBKP_MAGIC)
                {
                    archive.Position -= sizeof(uint);
                    while (archive.Position < archive.Length)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        archive.Position += 29;
                        paths.Add(archive.ReadString().ToLower()[1..]);
                        archive.Position += 4;
                    }
                }
                else
                {
                    var version = archive.Read<EBackupVersion>();
                    var count = archive.Read<int>();
                    for (var i = 0; i < count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        archive.Position += sizeof(long) + sizeof(byte);
                        paths.Add(archive.ReadString().ToLower()[1..]);
                    }
                }

                foreach (var (key, asset) in _applicationView.CUE4Parse.Provider.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (asset.IsUePackagePayload || paths.Contains(key)) continue;

                    entries.Add(asset);
                }

                break;
            }
            case ELoadingMode.AllButModified:
            {
                var magic = archive.Read<uint>();
                if (magic != BackupManagerViewModel.FBKP_MAGIC)
                {
                    archive.Position -= sizeof(uint);
                    while (archive.Position < archive.Length)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        archive.Position += 16;
                        var uncompressedSize = archive.Read<long>();
                        var isEncrypted = archive.ReadFlag();
                        archive.Position += 4;
                        var fullPath = archive.ReadString().ToLower()[1..];
                        archive.Position += 4;

                        AddEntry(fullPath, uncompressedSize, isEncrypted, entries);
                    }
                }
                else
                {
                    var version = archive.Read<EBackupVersion>();
                    var count = archive.Read<int>();
                    for (var i = 0; i < count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var uncompressedSize = archive.Read<long>();
                        var isEncrypted = archive.ReadFlag();
                        var fullPath = archive.ReadString().ToLower()[1..];

                        AddEntry(fullPath, uncompressedSize, isEncrypted, entries);
                    }
                }
                break;
            }
        }

        return entries;
    }

    private void AddEntry(string path, long uncompressedSize, bool isEncrypted, List<GameFile> entries)
    {
        if (!_applicationView.CUE4Parse.Provider.Files.TryGetValue(path, out var asset) ||
            asset.IsUePackagePayload || asset.Size == uncompressedSize && asset.IsEncrypted == isEncrypted)
            return;

        entries.Add(asset);
    }
}
