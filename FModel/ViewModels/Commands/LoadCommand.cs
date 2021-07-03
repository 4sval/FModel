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
using CUE4Parse.UE4.Vfs;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.Views.Resources.Controls;
using K4os.Compression.LZ4.Streams;
using Microsoft.Win32;

namespace FModel.ViewModels.Commands
{
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

        public LoadCommand(LoadingModesViewModel contextViewModel) : base(contextViewModel)
        {
        }

        public override async void Execute(LoadingModesViewModel contextViewModel, object parameter)
        {
            if (_applicationView.CUE4Parse.GameDirectory.HasNoFile) return;
            if (_applicationView.CUE4Parse.Game == FGame.FortniteGame &&
                _applicationView.CUE4Parse.Provider.MappingsContainer == null)
            {
                FLogger.AppendError();
                FLogger.AppendText("Mappings could not get pulled, extracting assets might not work properly. If so, press F12 or please restart.", Constants.WHITE, true);
            }
#if DEBUG
            var loadingTime = Stopwatch.StartNew();
#endif
            _applicationView.CUE4Parse.AssetsFolder.Folders.Clear();
            _applicationView.CUE4Parse.SearchVm.SearchResults.Clear();
            MainWindow.YesWeCats.LeftTabControl.SelectedIndex = 1; // folders tab

            await _applicationView.CUE4Parse.LoadLocalizedResources(); // load locres if not already loaded
            Helper.CloseWindow<AdonisWindow>("Search View"); // close search window if opened

            await _threadWorkerView.Begin(async cancellationToken =>
            {
                // filter what to show
                switch (UserSettings.Default.LoadingMode)
                {
                    case ELoadingMode.Single:
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
                        await FilterNewOrModifiedFilesToDisplay(cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    default: throw new ArgumentOutOfRangeException();
                }

                _discordHandler.UpdatePresence(_applicationView.CUE4Parse);
            });
#if DEBUG
            loadingTime.Stop();
            FLogger.AppendDebug();
            FLogger.AppendText($"{_applicationView.CUE4Parse.SearchVm.SearchResults.Count} packages and a lot of localized resources loaded in {loadingTime.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)} seconds", Constants.WHITE, true);
#endif
        }

        private void FilterDirectoryFilesToDisplay(CancellationToken cancellationToken, IEnumerable<FileItem> directoryFiles)
        {
            HashSet<string> filter;
            if (directoryFiles == null)
                filter = null;
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
                        entries.Add(entry);
                }
                else
                    entries.Add(entry);
            }

            _applicationView.CUE4Parse.AssetsFolder.BulkPopulate(entries);
        }

        private async Task FilterNewOrModifiedFilesToDisplay(CancellationToken cancellationToken)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select a backup file older than your current game version",
                InitialDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Backups"),
                Filter = "FBKP Files (*.fbkp)|*.fbkp|All Files (*.*)|*.*",
                Multiselect = false
            };

            if (!(bool) openFileDialog.ShowDialog()) return;

            FLogger.AppendInformation();
            FLogger.AppendText($"Backup file older than current game version is '{openFileDialog.FileName.SubstringAfterLast("\\")}'", Constants.WHITE, true);

            await using var fileStream = new FileStream(openFileDialog.FileName, FileMode.Open);
            await using var memoryStream = new MemoryStream();

            if (fileStream.ReadUInt32() == _IS_LZ4)
            {
                fileStream.Position -= 4;
                await using var compressionStream = LZ4Stream.Decode(fileStream);
                await compressionStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            }
            else
                await fileStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

            memoryStream.Position = 0;
            await using var archive = new FStreamArchive(fileStream.Name, memoryStream);
            var entries = new List<VfsEntry>();

            switch (UserSettings.Default.LoadingMode)
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
                    }

                    break;
                }
            }

            _applicationView.CUE4Parse.AssetsFolder.BulkPopulate(entries);
        }
    }
}