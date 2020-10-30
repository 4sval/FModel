using FModel.Logger;
using FModel.Utils;
using FModel.ViewModels.StatusBar;
using FModel.Windows.CustomNotifier;
using FModel.Windows.DarkMessageBox;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FModel.PakReader.Pak;
using FModel.PakReader.Parsers.Objects;
using FModel.PakReader.IO;

namespace FModel.ViewModels.MenuItem
{
    public class BackupMenuItemViewModel : PropertyChangedBase
    {
        private string _header;
        private bool _isCheckable;
        private bool _isChecked;
        private bool _isEnabled = true;
        private bool _staysOpenOnClick = false;
        private string _downloadUrl;
        private double _size;
        private string _inputGestureText;
        private Image _icon;
        private ObservableCollection<PakMenuItemViewModel> _childrens;
        public string Header
        {
            get { return _header; }

            set { this.SetProperty(ref this._header, value); }
        }
        public bool IsCheckable
        {
            get { return _isCheckable; }

            set { this.SetProperty(ref this._isCheckable, value); }
        }
        public bool IsChecked
        {
            get { return _isChecked; }

            set { this.SetProperty(ref this._isChecked, value); }
        }
        public bool IsEnabled
        {
            get { return _isEnabled; }

            set { this.SetProperty(ref this._isEnabled, value); }
        }
        public bool StaysOpenOnClick
        {
            get { return _staysOpenOnClick; }

            set { this.SetProperty(ref this._staysOpenOnClick, value); }
        }
        public string DownloadUrl
        {
            get { return _downloadUrl; }

            set { this.SetProperty(ref this._downloadUrl, value); }
        }
        public double Size
        {
            get { return _size; }

            set { this.SetProperty(ref this._size, value); }
        }
        public string InputGestureText
        {
            get { return Size > 0 ? Strings.GetReadableSize(Size) : _inputGestureText; }

            set { this.SetProperty(ref this._inputGestureText, value); }
        }
        public Image Icon
        {
            get { return _icon; }

            set { this.SetProperty(ref this._icon, value); }
        }
        public ObservableCollection<PakMenuItemViewModel> Childrens
        {
            get { return _childrens; }

            set { this.SetProperty(ref this._childrens, value); }
        }
        public ICommand Command
        {
            get
            {
                return BackupCanExecute()
                    ? new CommandHandler(async() => await Backup().ConfigureAwait(false), () => true)
                    : new CommandHandler(Download, DownloadCanExecute);
            }

            private set { }
        }

        private async void Download()
        {
            if (DarkMessageBoxHelper.ShowOKCancel(string.Format(Properties.Resources.AboutToDownload, Header, InputGestureText), Properties.Resources.Warning, Properties.Resources.OK, Properties.Resources.Cancel) == MessageBoxResult.OK)
            {
                Stopwatch downloadTimer = Stopwatch.StartNew();
                StatusBarVm.statusBarViewModel.Set($"{Properties.Resources.Downloading} {Header}", Properties.Resources.Waiting);

                string path = $"{Properties.Settings.Default.OutputPath}\\Backups\\{Header}";
                using var client = new HttpClientDownloadWithProgress(DownloadUrl, path);
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    StatusBarVm.statusBarViewModel.Set($"{Properties.Resources.Downloading} {Header}   🠞   {progressPercentage}%", Properties.Resources.Waiting);
                };

                await client.StartDownload().ConfigureAwait(false);

                downloadTimer.Stop();
                if (new FileInfo(path).Length > 0)
                {
                    DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[CDN]", $"Downloaded {Header} in {downloadTimer.ElapsedMilliseconds} ms");
                    StatusBarVm.statusBarViewModel.Set(string.Format(Properties.Resources.DownloadSuccess, Header), Properties.Resources.Success);
                    Globals.gNotifier.ShowCustomMessage(Properties.Resources.Success, string.Format(Properties.Resources.DownloadSuccess, Header), "/FModel;component/Resources/check-circle.ico", path);
                }
                else
                {
                    File.Delete(path);
                    DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[CDN]", $"Error while downloading {Header}, spent {downloadTimer.ElapsedMilliseconds} ms");
                    StatusBarVm.statusBarViewModel.Set(string.Format(Properties.Resources.DownloadError, Header), Properties.Resources.Error);
                    Globals.gNotifier.ShowCustomMessage(Properties.Resources.Error, string.Format(Properties.Resources.DownloadError, Header), "/FModel;component/Resources/alert.ico");
                }
            }
        }
        private bool DownloadCanExecute() => !Header.Equals(Properties.Resources.BackupPaks);

        private static readonly string _backupFileName = Folders.GetGameName() + "_" + DateTime.Now.ToString("MMddyyyy") + ".fbkp";
        private static readonly string _backupFilePath =  Properties.Settings.Default.OutputPath + "\\Backups\\" + _backupFileName;
        private async Task Backup()
        {
            StatusBarVm.statusBarViewModel.Reset();
            await Task.Run(() =>
            {
                DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[BackupMenuItemViewModel]", "[Create]", $"{_backupFileName} is about to be created");
                StatusBarVm.statusBarViewModel.Set($"{Properties.Settings.Default.PakPath}", Properties.Resources.Loading);

                using FileStream fileStream = new FileStream(_backupFilePath, FileMode.Create);
                using LZ4EncoderStream compressionStream = LZ4Stream.Encode(fileStream, LZ4Level.L00_FAST);
                using BinaryWriter writer = new BinaryWriter(compressionStream);
                foreach (PakFileReader pakFile in MenuItems.pakFiles.GetPakFileReaders())
                {
                    if (pakFile.Info.bEncryptedIndex && pakFile.AesKey == null)
                        continue;

                    if (!Globals.CachedPakFiles.ContainsKey(pakFile.FileName))
                    {
                        pakFile.ReadIndex(pakFile.AesKey);
                        Globals.CachedPakFiles[pakFile.FileName] = pakFile;
                        StatusBarVm.statusBarViewModel.Set(string.Format(Properties.Resources.MountedPakTo, pakFile.FileName, pakFile.MountPoint), Properties.Resources.Loading);
                    }
                    
                    foreach (var (_, entry) in pakFile)
                    {
                        // uasset or umap or idk
                        writer.Write(entry.Offset);
                        writer.Write(entry.Size);
                        writer.Write(entry.UncompressedSize);
                        writer.Write(entry.Encrypted);
                        writer.Write(entry.StructSize);
                        writer.Write(pakFile.MountPoint + entry.Name);
                        writer.Write(entry.CompressionMethodIndex);

                        // uexp
                        if (entry.Uexp != null && entry.Uexp is FPakEntry uexp)
                        {
                            writer.Write(uexp.Offset);
                            writer.Write(uexp.Size);
                            writer.Write(uexp.UncompressedSize);
                            writer.Write(uexp.Encrypted);
                            writer.Write(uexp.StructSize);
                            writer.Write(pakFile.MountPoint + entry.Uexp.Name);
                            writer.Write(uexp.CompressionMethodIndex);
                        }
                        // ubulk
                        if (entry.Ubulk != null && entry.Ubulk is FPakEntry ubulk)
                        {
                            writer.Write(ubulk.Offset);
                            writer.Write(ubulk.Size);
                            writer.Write(ubulk.UncompressedSize);
                            writer.Write(ubulk.Encrypted);
                            writer.Write(ubulk.StructSize);
                            writer.Write(pakFile.MountPoint + entry.Ubulk.Name);
                            writer.Write(ubulk.CompressionMethodIndex);
                        }
                    }
                }
                FFileIoStoreReader globalReader = null;
                foreach (FFileIoStoreReader ioStore in MenuItems.pakFiles.GetIoStoreReaders())
                {
                    if (ioStore.IsEncrypted && ioStore.AesKey == null)
                        continue;

                    if (!Globals.CachedIoStores.ContainsKey(ioStore.FileName))
                    {
                        if (ioStore.FileName.Contains("global.ucas", StringComparison.OrdinalIgnoreCase))
                        {
                            globalReader = ioStore;
                            continue;
                        }
                        if (!ioStore.ReadDirectoryIndex())
                            continue;
                        Globals.CachedIoStores[ioStore.FileName] = ioStore;
                        StatusBarVm.statusBarViewModel.Set(string.Format(Properties.Resources.MountedPakTo, ioStore.FileName, ioStore.MountPoint), Properties.Resources.Loading);
                    }

                    foreach (var (_, entry) in ioStore)
                    {
                        // uasset or umap or idk
                        writer.Write(entry.Offset);
                        writer.Write(entry.Length);
                        writer.Write(entry.UncompressedSize);
                        writer.Write(entry.Encrypted);
                        writer.Write(entry.StructSize);
                        writer.Write(ioStore.MountPoint + entry.Name);
                        writer.Write(entry.CompressionMethodIndex);

                        // uexp
                        if (entry.Uexp != null && entry.Uexp is FIoStoreEntry uexp)
                        {
                            writer.Write(uexp.Offset);
                            writer.Write(uexp.Length);
                            writer.Write(uexp.UncompressedSize);
                            writer.Write(uexp.Encrypted);
                            writer.Write(uexp.StructSize);
                            writer.Write(ioStore.MountPoint + entry.Uexp.Name);
                            writer.Write(uexp.CompressionMethodIndex);
                        }
                        // ubulk
                        if (entry.Ubulk != null && entry.Ubulk is FIoStoreEntry ubulk)
                        {
                            writer.Write(ubulk.Offset);
                            writer.Write(ubulk.Length);
                            writer.Write(ubulk.UncompressedSize);
                            writer.Write(ubulk.Encrypted);
                            writer.Write(ubulk.StructSize);
                            writer.Write(ioStore.MountPoint + entry.Ubulk.Name);
                            writer.Write(ubulk.CompressionMethodIndex);
                        }
                    }
                }
            }).ContinueWith(t =>
            {
                if (t.Exception != null) Tasks.TaskCompleted(t.Exception);
                else if (new FileInfo(_backupFilePath).Length > 0)
                {
                    DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[BackupMenuItemViewModel]", "[Create]", $"{_backupFileName} successfully created");
                    StatusBarVm.statusBarViewModel.Set(string.Format(Properties.Resources.CreateSuccess, _backupFileName), Properties.Resources.Success);
                    Globals.gNotifier.ShowCustomMessage(Properties.Resources.Success, string.Format(Properties.Resources.CreateSuccess, _backupFileName), "/FModel;component/Resources/check-circle.ico", _backupFilePath);
                }
                else
                {
                    File.Delete(_backupFilePath);
                    DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[BackupMenuItemViewModel]", "[Create]", $"{_backupFileName} is empty, hence deleted");
                    StatusBarVm.statusBarViewModel.Set(string.Format(Properties.Resources.CreateError, _backupFileName), Properties.Resources.Error);
                    Globals.gNotifier.ShowCustomMessage(Properties.Resources.Error, string.Format(Properties.Resources.CreateError, _backupFileName), "/FModel;component/Resources/alert.ico");
                }
            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }
        private bool BackupCanExecute() => Header.Equals(Properties.Resources.BackupPaks);
    }
}
