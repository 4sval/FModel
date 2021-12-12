using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using CUE4Parse.UE4.Vfs;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.Views.Resources.Controls;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using Serilog;

namespace FModel.ViewModels
{
    public class BackupManagerViewModel : ViewModel
    {
        private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
        private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;
        private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
        private readonly string _gameName;

        private Backup _selectedBackup;
        public Backup SelectedBackup
        {
            get => _selectedBackup;
            set => SetProperty(ref _selectedBackup, value);
        }

        public ObservableCollection<Backup> Backups { get; }
        public ICollectionView BackupsView { get; }

        public BackupManagerViewModel(string gameName)
        {
            _gameName = gameName;
            Backups = new ObservableCollection<Backup>();
            BackupsView = new ListCollectionView(Backups) {SortDescriptions = {new SortDescription("FileName", ListSortDirection.Ascending)}};
        }

        public async Task Initialize()
        {
            await _threadWorkerView.Begin(cancellationToken =>
            {
                var backups = _apiEndpointView.FModelApi.GetBackups(cancellationToken, _gameName);
                if (backups == null) return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var backup in backups) Backups.Add(backup);
                    SelectedBackup = Backups.LastOrDefault();
                });
            });
        }

        public async Task CreateBackup()
        {
            await _threadWorkerView.Begin(_ =>
            {
                var backupFolder = Path.Combine(UserSettings.Default.OutputDirectory, "Backups");
                var fileName = $"{_gameName}_{DateTime.Now.ToString("MM'-'dd'-'yyyy")}.fbkp";
                var fullPath = Path.Combine(backupFolder, fileName);

                using var fileStream = new FileStream(fullPath, FileMode.Create);
                using var compressedStream = LZ4Stream.Encode(fileStream, LZ4Level.L00_FAST);
                using var writer = new BinaryWriter(compressedStream);
                foreach (var asset in _applicationView.CUE4Parse.Provider.Files.Values)
                {
                    if (asset is not VfsEntry entry || entry.Path.EndsWith(".uexp") ||
                        entry.Path.EndsWith(".ubulk") || entry.Path.EndsWith(".uptnl"))
                        continue;

                    writer.Write((long) 0);
                    writer.Write((long) 0);
                    writer.Write(entry.Size);
                    writer.Write(entry.IsEncrypted);
                    writer.Write(0);
                    writer.Write($"/{entry.Path.ToLower()}");
                    writer.Write(0);
                }

                SaveCheck(fullPath, fileName, "created", "create");
            });
        }

        public async Task Download()
        {
            if (SelectedBackup == null) return;
            await _threadWorkerView.Begin(_ =>
            {
                var fullPath = Path.Combine(Path.Combine(UserSettings.Default.OutputDirectory, "Backups"), SelectedBackup.FileName);
                _apiEndpointView.BenbotApi.DownloadFile(SelectedBackup.DownloadUrl, fullPath);
                SaveCheck(fullPath, SelectedBackup.FileName, "downloaded", "download");
            });
        }

        private void SaveCheck(string fullPath, string fileName, string type1, string type2)
        {
            if (new FileInfo(fullPath).Length > 0)
            {
                Log.Information("{FileName} successfully {Type}", fileName, type1);
                FLogger.AppendInformation();
                FLogger.AppendText($"Successfully {type1} '{fileName}'", Constants.WHITE, true);
            }
            else
            {
                Log.Error("{FileName} could not be {Type}", fileName, type1);
                FLogger.AppendError();
                FLogger.AppendText($"Could not {type2} '{fileName}'", Constants.WHITE, true);
            }
        }
    }
}
