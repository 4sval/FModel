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
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;
using FModel.Creator;
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
        if (_applicationView.CUE4Parse.GameDirectory.HasNoFile) return;
        if (_applicationView.CUE4Parse.Provider.Keys.Count == 0 && _applicationView.CUE4Parse.Provider.RequiredKeys.Count > 0)
        {
            FLogger.Append(ELog.Error, () =>
                FLogger.Text("An encrypted archive has been found. In order to decrypt it, please specify a working AES encryption key", Constants.WHITE, true));
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
            filter = new HashSet<string>();
            foreach (var directoryFile in directoryFiles)
            {
                if (!directoryFile.IsEnabled)
                    continue;

                filter.Add(directoryFile.Name);
            }
        }

        var hasFilter = filter != null && filter.Count != 0;
        var entries = new List<VfsEntry>();

        foreach (var asset in _applicationView.CUE4Parse.Provider.Files.Values)
        {
            cancellationToken.ThrowIfCancellationRequested(); // cancel if needed

            if (asset is not VfsEntry entry || entry.Path.EndsWith(".uexp") || entry.Path.EndsWith(".ubulk") || entry.Path.EndsWith(".uptnl"))
                continue;

            if (hasFilter)
            {
                if (filter.Contains(entry.Vfs.Name))
                {
                    entries.Add(entry);
                    _applicationView.Status.UpdateStatusLabel(entry.Vfs.Name);
                }
            }
            else
            {
                entries.Add(entry);
                _applicationView.Status.UpdateStatusLabel(entry.Vfs.Name);
            }
        }

        _applicationView.Status.UpdateStatusLabel("Folders & Packages");
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

        using var fileStream = new FileStream(openFileDialog.FileName, FileMode.Open);
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
        var entries = new List<VfsEntry>();

        var mode = UserSettings.Default.LoadingMode;
        switch (mode)
        {
            case ELoadingMode.AllButNew:
            {
                var paths = new Dictionary<string, int>();
                while (archive.Position < archive.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    archive.Position += 29;
                    paths[archive.ReadString().ToLower()[1..]] = 0;
                    archive.Position += 4;
                }

                foreach (var (key, value) in _applicationView.CUE4Parse.Provider.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (value is not VfsEntry entry || paths.ContainsKey(key) || entry.Path.EndsWith(".uexp") ||
                        entry.Path.EndsWith(".ubulk") || entry.Path.EndsWith(".uptnl")) continue;

                    entries.Add(entry);
                    _applicationView.Status.UpdateStatusLabel(entry.Vfs.Name);
                }

                break;
            }
            case ELoadingMode.AllButModified:
            {
                while (archive.Position < archive.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    archive.Position += 16;
                    var uncompressedSize = archive.Read<long>();
                    var isEncrypted = archive.ReadFlag();
                    archive.Position += 4;
                    var fullPath = archive.ReadString().ToLower()[1..];
                    archive.Position += 4;

                    if (fullPath.EndsWith(".uexp") || fullPath.EndsWith(".ubulk") || fullPath.EndsWith(".uptnl") ||
                        !_applicationView.CUE4Parse.Provider.Files.TryGetValue(fullPath, out var asset) || asset is not VfsEntry entry ||
                        entry.Size == uncompressedSize && entry.IsEncrypted == isEncrypted)
                        continue;

                    entries.Add(entry);
                    _applicationView.Status.UpdateStatusLabel(entry.Vfs.Name);
                }

                break;
            }
        }

        _applicationView.Status.UpdateStatusLabel($"{mode.ToString()[6..]} Folders & Packages");
        _applicationView.CUE4Parse.AssetsFolder.BulkPopulate(entries);
    }
}
