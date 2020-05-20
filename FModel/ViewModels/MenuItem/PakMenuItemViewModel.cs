using FModel.Discord;
using FModel.Logger;
using FModel.Utils;
using FModel.ViewModels.AvalonEdit;
using FModel.ViewModels.DataGrid;
using FModel.ViewModels.ImageBox;
using FModel.ViewModels.ListBox;
using FModel.ViewModels.StatusBar;
using FModel.ViewModels.TabControl;
using FModel.ViewModels.Treeview;
using K4os.Compression.LZ4.Streams;
using Microsoft.Win32;
using PakReader.Pak;
using PakReader.Parsers.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FModel.ViewModels.MenuItem
{
    public class PakMenuItemViewModel : PropertyChangedBase
    {
        private string _header;
        private bool _isCheckable;
        private bool _isChecked;
        private bool _isEnabled = true;
        private bool _staysOpenOnClick = false;
        private string _inputGestureText;
        private Image _icon;
        private PakFileReader _pakFile;
        private PakMenuItemViewModel _parent;
        private ObservableCollection<PakMenuItemViewModel> _childrens;

        public string Header
        {
            get { return PakFile != null ? PakFile.FileName : _header; }

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
        public string InputGestureText
        {
            get
            {
                long size = PakFile != null ? PakFile.Stream.Length : 0;
                if (size > 0)
                    return Strings.GetReadableSize(size);
                else
                    return _inputGestureText;
            }

            set { this.SetProperty(ref this._inputGestureText, value); }
        }
        public Image Icon
        {
            get { return _icon; }

            set { this.SetProperty(ref this._icon, value); }
        }
        public PakFileReader PakFile
        {
            get { return _pakFile ?? null; }

            set { this.SetProperty(ref this._pakFile, value); }
        }
        public PakMenuItemViewModel Parent
        {
            get { return _parent; }

            set { this.SetProperty(ref this._parent, value); }
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
                if (SwitchLoadingModeCanExecute())
                    return new CommandHandler(SwitchLoadingMode, () => true);
                else if (AllPaksLoaderCanExecute())
                    return new CommandHandler(AllPaksLoader, () => true);
                else if (NewFilesLoaderCanExecute())
                    return new CommandHandler(NewFilesLoader, () => true);
                else if (ModifiedFilesLoaderCanExecute())
                    return new CommandHandler(ModifiedFilesLoader, () => true);
                else if (NewModifiedFilesLoaderCanExecute())
                    return new CommandHandler(NewModifiedFilesLoader, () => true);
                else if (SinglePakLoaderCanExecute())
                    return new CommandHandler(SinglePakLoader, () => true);
                else
                    return null;
            }

            private set { }
        }

        private void SwitchLoadingMode()
        {
            Parent.Header = $"{Properties.Resources.LoadingMode}   🠞   {Header}";
            foreach (PakMenuItemViewModel item in Parent.Childrens)
            {
                item.IsChecked = item.Header == Header;
            }

            if (Header.Equals(Properties.Resources.Default))
                MenuItems.pakFiles[1].Header = Properties.Resources.LoadAll;
            else if (Header.Equals(Properties.Resources.NewFiles))
                MenuItems.pakFiles[1].Header = Properties.Resources.LoadNewFiles;
            else if (Header.Equals(Properties.Resources.ModifiedFiles))
                MenuItems.pakFiles[1].Header = Properties.Resources.LoadModifiedFiles;
            else if (Header.Equals(Properties.Resources.NewModifiedFiles))
                MenuItems.pakFiles[1].Header = Properties.Resources.LoadNewModifiedFiles;
            Keys.NoKeyGoodBye(!Header.Equals(Properties.Resources.Default));

            // this refresh everything especially the commands
            // that way i can do multiple commands on 1 item depending on its header
            CollectionViewSource.GetDefaultView(MenuItems.pakFiles).Refresh();
        }
        private bool SwitchLoadingModeCanExecute() =>
            Header.Equals(Properties.Resources.Default) ||
            Header.Equals(Properties.Resources.NewFiles) ||
            Header.Equals(Properties.Resources.ModifiedFiles) ||
            Header.Equals(Properties.Resources.NewModifiedFiles);

        private async void SinglePakLoader() => await LoadPakFiles(EPakLoader.Single).ConfigureAwait(false);
        private bool SinglePakLoaderCanExecute() => PakFile != null;
        private async void AllPaksLoader() => await LoadPakFiles(EPakLoader.All).ConfigureAwait(false);
        private bool AllPaksLoaderCanExecute() => Header.Equals(Properties.Resources.LoadAll);
        private async void NewFilesLoader() => await LoadPakFiles(EPakLoader.New).ConfigureAwait(false);
        private bool NewFilesLoaderCanExecute() => Header.Equals(Properties.Resources.LoadNewFiles);
        private async void ModifiedFilesLoader() => await LoadPakFiles(EPakLoader.Modified).ConfigureAwait(false);
        private bool ModifiedFilesLoaderCanExecute() => Header.Equals(Properties.Resources.LoadModifiedFiles);
        private async void NewModifiedFilesLoader() => await LoadPakFiles(EPakLoader.NewModified).ConfigureAwait(false);
        private bool NewModifiedFilesLoaderCanExecute() => Header.Equals(Properties.Resources.LoadNewModifiedFiles);

        private async Task LoadPakFiles(EPakLoader mode)
        {
            ListBoxVm.gameFiles.Clear();
            DataGridVm.dataGridViewModel.Clear();
            ImageBoxVm.imageBoxViewModel.Reset();
            StatusBarVm.statusBarViewModel.Reset();
            AvalonEditVm.avalonEditViewModel.Reset();
            PakPropertiesVm.pakPropertiesViewModel.Reset();
            SortedTreeviewVm.gameFilesPath.Childrens.Clear();
            AssetPropertiesVm.assetPropertiesViewModel.Reset();

            await Task.Run(async () =>
            {
                if (mode == EPakLoader.Single)
                {
                    StatusBarVm.statusBarViewModel.Set($"{Properties.Settings.Default.PakPath}\\{PakFile.FileName}", Properties.Resources.Loading);
                    DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[PakMenuItemViewModel]", "[Loader]", $"{PakFile.FileName} was selected ({mode})");
                }
                else
                {
                    StatusBarVm.statusBarViewModel.Set(Properties.Settings.Default.PakPath, Properties.Resources.Loading);
                    DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[PakMenuItemViewModel]", "[Loader]", $"All PAK files were selected ({mode})");
                }

                foreach (PakFileReader pakFile in MenuItems.pakFiles.GetPakFileReaders())
                {
                    if (pakFile.AesKey == null)
                        continue;

                    if (!Globals.CachedPakFiles.ContainsKey(pakFile.FileName))
                    {
                        pakFile.ReadIndex(pakFile.AesKey);
                        Globals.CachedPakFiles[pakFile.FileName] = pakFile;

                        if (mode != EPakLoader.Single)
                            StatusBarVm.statusBarViewModel.Set(string.Format(Properties.Resources.MountedPakTo, pakFile.FileName, pakFile.MountPoint), Properties.Resources.Loading);
                    }
                }

                if (mode == EPakLoader.Single) PakPropertiesVm.pakPropertiesViewModel.Set(PakFile);
                await Localizations.SetLocalization(Properties.Settings.Default.AssetsLanguage, false).ConfigureAwait(false);
                PopulateTreeviewViewModel(mode);
            }).ContinueWith(t =>
            {
                DiscordIntegration.Update(
                    $"{Globals.CachedPakFiles.Count}/{MenuItems.pakFiles.GetPakCount()} {Properties.Resources.PakFiles}",
                    string.Format("{0} - {1}", Globals.Game,
                        mode == EPakLoader.All ? Properties.Resources.AllFiles :
                        mode == EPakLoader.New ? Properties.Resources.NewFiles :
                        mode == EPakLoader.Modified ? Properties.Resources.ModifiedFiles :
                        mode == EPakLoader.NewModified ? Properties.Resources.NewModifiedFiles :
                        mode == EPakLoader.Single ? Header.Substring(0, Header.LastIndexOf("-WindowsClient.pak")) :
                        string.Empty
                        ));

                if (t.Exception != null) Tasks.TaskCompleted(t.Exception);
                else StatusBarVm.statusBarViewModel.Set(
                    mode == EPakLoader.Single ?
                    $"{Properties.Settings.Default.PakPath}\\{PakFile.FileName}" :
                    Properties.Settings.Default.PakPath,
                    Properties.Resources.Success);
            },
            TaskScheduler.FromCurrentSynchronizationContext());

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private Dictionary<string, FPakEntry> GetOldFiles(EPakLoader mode)
        {
            var diff = new Dictionary<string, FPakEntry>();
            var ofd = new OpenFileDialog()
            {
                Title = Properties.Resources.SelectFile,
                InitialDirectory = Properties.Settings.Default.OutputPath + "\\Backups\\",
                Filter = Properties.Resources.FbkpFilter,
                Multiselect = false
            };
            if ((bool)ofd.ShowDialog())
            {
                string n = Path.GetFileName(ofd.FileName);
                StatusBarVm.statusBarViewModel.Set(string.Format(Properties.Resources.Analyzing, n), Properties.Resources.Processing);
                DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[PakMenuItemViewModel]", "[Loader]", $"Backup file is {n}");

                var oldFilesTemp = new Dictionary<string, FPakEntry>();
                using FileStream fileStream = new FileStream(ofd.FileName, FileMode.Open);
                BinaryReader checkReader = new BinaryReader(fileStream);
                bool isLz4 = checkReader.ReadUInt32() == 0x184D2204u;
                fileStream.Seek(0, SeekOrigin.Begin);
                var target = new MemoryStream();
                if (isLz4)
                {
                    using LZ4DecoderStream compressionStream = LZ4Stream.Decode(fileStream);
                    compressionStream.CopyTo(target);
                }
                else
                {
                    fileStream.CopyTo(target);
                }
                using (target)
                {
                    target.Position = 0;
                    using BinaryReader reader = new BinaryReader(target);
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        // we must follow this order
                        long offset = reader.ReadInt64();
                        long size = reader.ReadInt64();
                        long uncompressedSize = reader.ReadInt64();
                        bool encrypted = reader.ReadBoolean();
                        long structSize = reader.ReadInt32();
                        string name = reader.ReadString();
                        int compressionMethodIndex = reader.ReadInt32();

                        // we only need name and uncompressedSize to compare
                        FPakEntry entry = new FPakEntry("CatsWillDominateTheWorld.pak", name, offset, size, uncompressedSize, new byte[20], null, 0, (uint)compressionMethodIndex, 0);
                        oldFilesTemp[entry.Name] = entry;
                    }
                }

                var newFiles = new Dictionary<string, FPakEntry>();
                foreach (var fileReader in Globals.CachedPakFiles)
                    foreach (var files in fileReader.Value)
                        newFiles[files.Key] = files.Value;

                Paks.Merge(oldFilesTemp, out var oldFiles, string.Empty);

                switch (mode)
                {
                    case EPakLoader.New:
                        foreach (var kvp in newFiles)
                        {
                            if (!oldFiles.TryGetValue(kvp.Key, out var entry))
                                diff.Add(kvp.Key, kvp.Value);
                        }
                        break;
                    case EPakLoader.Modified:
                        foreach (var kvp in newFiles)
                        {
                            if (oldFiles.TryGetValue(kvp.Key, out var entry))
                                if (entry.UncompressedSize != kvp.Value.UncompressedSize)
                                    diff.Add(kvp.Key, kvp.Value);
                        }
                        break;
                    case EPakLoader.NewModified:
                        foreach (var kvp in newFiles)
                        {
                            if (oldFiles.TryGetValue(kvp.Key, out var entry))
                            {
                                if (entry.UncompressedSize != kvp.Value.UncompressedSize)
                                    diff.Add(kvp.Key, kvp.Value);
                            }
                            else
                                diff.Add(kvp.Key, kvp.Value);
                        }
                        break;
                }

                var deleted = oldFiles.Where(kvp => !newFiles.TryGetValue(kvp.Key, out var _) && kvp.Key.StartsWith("/FortniteGame/Content/Athena/Items/Cosmetics/")).ToDictionary(x => x.Key, x => x.Value);
                if (deleted.Count > 0)
                {
                    FConsole.AppendText(Properties.Resources.RemovedRenamedCosmetics, FColors.Red, true);
                    foreach (var kvp in deleted)
                        FConsole.AppendText($"    - {kvp.Value.Name.Substring(1)}", FColors.LightGray, true);
                }
            }
            return diff;
        }

        private void PopulateTreeviewViewModel(EPakLoader mode)
        {
            switch (mode)
            {
                case EPakLoader.Single:
                    PopulateProcess(Globals.CachedPakFiles[PakFile.FileName]);
                    break;
                case EPakLoader.All:
                    foreach (var fileReader in Globals.CachedPakFiles)
                        PopulateProcess(fileReader.Value);
                    break;
                case EPakLoader.New:
                case EPakLoader.Modified:
                case EPakLoader.NewModified:
                    PopulateProcess(GetOldFiles(mode));
                    break;
            }
            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[PakMenuItemViewModel]", "[Loader]", "Treeview populated");
        }

        private void PopulateProcess(IReadOnlyDictionary<string, FPakEntry> array)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                foreach (KeyValuePair<string, FPakEntry> entry in array)
                {
                    string path = entry.Key.Substring(1) + entry.Value.GetExtension();
                    Populate(SortedTreeviewVm.gameFilesPath, path.Substring(0, path.LastIndexOf("/")), path, entry.Value);

                    // it slows down the process when loading all pak files (mainly)
                    // but there's no loading time when opening search files window afterward
                    DataGridVm.dataGridViewModel.Add(
                        path,
                        string.Join(" ",
                        entry.Value.GetExtension(),
                        entry.Value.Uexp?.GetExtension(),
                        entry.Value.Ubulk?.GetExtension()).TrimEnd(),
                        entry.Value.PakFileName);
                }
            });
        }
        private void Populate(dynamic nodeList, string pathWithoutFile, string seqPath, FPakEntry entry)
        {
            string folder;
            int p = seqPath.IndexOf('/');
            if (p == -1)
            {
                folder = seqPath;
                seqPath = string.Empty;
            }
            else
            {
                folder = seqPath.Substring(0, p);
                int p1 = p + 1;
                seqPath = seqPath[p1..];
            }

            TreeviewViewModel node = null;
            foreach (TreeviewViewModel item in nodeList.Childrens)
                if (string.Equals(item.Header, folder))
                    node = item;

            if (node == null && !string.IsNullOrEmpty(seqPath))
            {
                node = new TreeviewViewModel(folder, nodeList);
                nodeList.Childrens.Add(node);
            }

            if (!string.IsNullOrEmpty(seqPath))
                Populate(node, pathWithoutFile, seqPath, entry);
            else
            {
                if (!nodeList.GameFiles.ContainsKey(pathWithoutFile))
                    nodeList.GameFiles[pathWithoutFile] = new ObservableSortedList<ListBoxViewModel>();

                nodeList.GameFiles[pathWithoutFile].Add(new ListBoxViewModel
                {
                    Content = folder,
                    PakEntry = entry
                });
            }
        }
    }
}
