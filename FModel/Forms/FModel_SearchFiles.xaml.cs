using FModel.Methods;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using PakReader;
using FModel.Methods.Utilities;
using System.IO;
using FModel.Methods.Assets;
using FModel.Methods.TreeViewModel;
using System.Linq;

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
            public string Extension { get; set; }
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
                FilterTextBox_Search.IsReadOnly = true;
                FileNames = new List<FileInfo>();
                await PopulateDataGrid();
                DataGrid_Search.ItemsSource = FileNames;
                FilterTextBox_Search.IsReadOnly = false;
            }
        }

        private static async Task PopulateDataGrid()
        {
            Dictionary<string, string> IfExistChecker = new Dictionary<string, string>();
            await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(FWindow.FCurrentPAK))
                {
                    FillList(PAKEntries.PAKToDisplay[FWindow.FCurrentPAK], IfExistChecker);
                }
                else
                {
                    foreach (FPakEntry[] PAKsFileInfos in PAKEntries.PAKToDisplay.Values)
                    {
                        FillList(PAKsFileInfos, IfExistChecker);
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
                string ext = Path.GetExtension(entry.Name);
                string pak = Path.GetFileName(AssetEntries.AssetEntriesDict[entry.Name].Name);

                if (filename.EndsWith(".uasset") || filename.EndsWith(".uexp") || filename.EndsWith(".ubulk"))
                {
                    filename = filename.Substring(0, filename.LastIndexOf('.'));
                    if (AssetEntries.ArraySearcher.ContainsKey(filename + ".uexp"))
                    {
                        ext += " .uexp";
                    }
                    if (AssetEntries.ArraySearcher.ContainsKey(filename + ".ubulk"))
                    {
                        ext += " .ubulk";
                    }
                    filename += ".uasset";
                }

                if (!ExistChecker.ContainsKey(filename))
                {
                    ExistChecker.Add(filename, pak);

                    FileNames.Add(new FileInfo
                    {
                        Name = filename,
                        Extension = ext,
                        PAK = pak
                    });
                }
            }
        }

        private async void FilterTextBox_Search_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (PAKEntries.PAKToDisplay != null && FileNames != null)
            {
                List<FileInfo> filtered = new List<FileInfo>();
                string[] filters = FilterTextBox_Search.Text.Trim().Split(' ');

                await Task.Run(() =>
                {
                    foreach (FileInfo fi in FileNames)
                    {
                        bool checkSearch = false;
                        if (filters.Length > 1)
                        {
                            foreach (string filter in filters)
                            {
                                checkSearch = ListBoxUtility.CaseInsensitiveContains(fi.Name, filter);
                                if (!checkSearch) { break; }
                            }
                        }
                        else { checkSearch = ListBoxUtility.CaseInsensitiveContains(fi.Name, filters[0]); }

                        if (checkSearch)
                        {
                            filtered.Add(fi);
                        }
                    }
                });

                DataGrid_Search.ItemsSource = filtered;
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

        private void GoTo_Button_Click(object sender, RoutedEventArgs e)
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
                TreeViewUtility.JumpToFolder(selectedName.Substring(1, selectedName.LastIndexOf("/") - 1));
                FWindow.FMain.ListBox_Main.SelectedValue = selectedName.Substring(selectedName.LastIndexOf("/") + 1);
                Close();
            }
        }
    }
}
