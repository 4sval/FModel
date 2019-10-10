using FModel.Methods;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using PakReader;
using FModel.Methods.Utilities;
using System.IO;
using FModel.Methods.Assets;

namespace FModel.Forms
{
    /// <summary>
    /// Logique d'interaction pour FModel_SearchFiles.xaml
    /// </summary>
    public partial class FModel_SearchFiles : Window
    {
        private static List<FileInfo> FileNames { get; set; }

        public class FileInfo
        {
            public string Name { get; set; }
            public string Size { get; set; }
            public string PAK { get; set; }
        }

        public class GridViewItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                var handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        public FModel_SearchFiles()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (PAKEntries.PAKToDisplay != null)
            {
                FileNames = new List<FileInfo>();
                await PopulateDataGrid();
                DataGrid_Search.ItemsSource = FileNames;
            }
        }

        private static async Task PopulateDataGrid(bool filtered = false, string[] filters = null)
        {
            Dictionary<string, string> IfExistChecker = new Dictionary<string, string>();
            await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(FWindow.FCurrentPAK))
                {
                    if (filtered)
                    {
                        GetFilteredItems(PAKEntries.PAKToDisplay[FWindow.FCurrentPAK], IfExistChecker, filters);
                    }
                    else
                    {
                        FillList(PAKEntries.PAKToDisplay[FWindow.FCurrentPAK], IfExistChecker);
                    }
                }
                else
                {
                    foreach (FPakEntry[] PAKsFileInfos in PAKEntries.PAKToDisplay.Values)
                    {
                        if (filtered)
                        {
                            GetFilteredItems(PAKsFileInfos, IfExistChecker, filters);
                        }
                        else
                        {
                            FillList(PAKsFileInfos, IfExistChecker);
                        }
                    }
                }
            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });
        }

        private static void FillList(FPakEntry[] EntryArray, Dictionary<string, string> ExistChecker)
        {
            foreach (FPakEntry entry in EntryArray)
            {
                string filename = entry.Name;
                long size = entry.UncompressedSize;
                string pak = Path.GetFileName(AssetEntries.AssetEntriesDict[entry.Name].Name);

                if (filename.EndsWith(".uasset") || filename.EndsWith(".uexp") || filename.EndsWith(".ubulk"))
                {
                    filename = filename.Substring(0, filename.LastIndexOf('.')) + ".uasset";
                }

                if (!ExistChecker.ContainsKey(filename))
                {
                    ExistChecker.Add(filename, pak);

                    FileNames.Add(new FileInfo
                    {
                        Name = filename,
                        Size = AssetsUtility.GetReadableSize(size), //size of the uasset (or other non uexp/ubulk ext) only :(
                        PAK = pak
                    });
                }
            }
        }

        private async void FilterTextBox_Search_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (PAKEntries.PAKToDisplay != null)
            {
                FileNames = new List<FileInfo>();
                string[] filters = FilterTextBox_Search.Text.Trim().Split(' ');
                await PopulateDataGrid(true, filters);
                await Task.Delay(300);
                DataGrid_Search.ItemsSource = FileNames;
            }
        }

        private static void GetFilteredItems(FPakEntry[] EntryArray, Dictionary<string, string> ExistChecker, string[] filters)
        {
            foreach (FPakEntry entry in EntryArray)
            {
                string filename = entry.Name;
                long size = entry.UncompressedSize;
                string pak = Path.GetFileName(AssetEntries.AssetEntriesDict[entry.Name].Name);

                if (filename.EndsWith(".uasset") || filename.EndsWith(".uexp") || filename.EndsWith(".ubulk"))
                {
                    filename = filename.Substring(0, filename.LastIndexOf('.')) + ".uasset";
                }

                bool checkSearch = false;
                if (filters.Length > 1)
                {
                    foreach (string filter in filters)
                    {
                        checkSearch = ListBoxUtility.CaseInsensitiveContains(filename, filter);
                        if (!checkSearch) { break; }
                    }
                }
                else { checkSearch = ListBoxUtility.CaseInsensitiveContains(filename, filters[0]); }

                if (checkSearch)
                {
                    if (!ExistChecker.ContainsKey(filename))
                    {
                        ExistChecker.Add(filename, pak);

                        FileNames.Add(new FileInfo
                        {
                            Name = filename,
                            Size = AssetsUtility.GetReadableSize(size), //size of the uasset (or other non uexp/ubulk ext) only :(
                            PAK = pak
                        });
                    }
                }
            }
        }

        #region RIGHT CLICK
        private void RC_Copy_FPath_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_Search.SelectedIndex >= 0)
            {
                FileInfo item = (FileInfo)DataGrid_Search.SelectedItem;
                string selectedName = item.Name;

                Clipboard.SetText(selectedName.Substring(1));
            }
        }
        private void RC_Copy_FName_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_Search.SelectedIndex >= 0)
            {
                FileInfo item = (FileInfo)DataGrid_Search.SelectedItem;
                string selectedName = item.Name;

                Clipboard.SetText(Path.GetFileName(selectedName));
            }
        }
        private void RC_Copy_FPath_NoExt_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_Search.SelectedIndex >= 0)
            {
                FileInfo item = (FileInfo)DataGrid_Search.SelectedItem;
                string selectedName = item.Name;

                Clipboard.SetText(FoldersUtility.GetFullPathWithoutExtension(selectedName).Substring(1));
            }
        }
        private void RC_Copy_FName_NoExt_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_Search.SelectedIndex >= 0)
            {
                FileInfo item = (FileInfo)DataGrid_Search.SelectedItem;
                string selectedName = item.Name;

                Clipboard.SetText(Path.GetFileNameWithoutExtension(selectedName));
            }
        }
        private void RC_Properties_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_Search.SelectedIndex >= 0)
            {
                FileInfo item = (FileInfo)DataGrid_Search.SelectedItem;
                string selectedName = item.Name;
                if (selectedName.EndsWith(".uasset"))
                {
                    selectedName = selectedName.Substring(0, selectedName.LastIndexOf('.'));
                }

                FWindow.FCurrentAsset = selectedName;
                AssetInformations.OpenAssetInfos(true);
            }
        }
        #endregion
    }
}
