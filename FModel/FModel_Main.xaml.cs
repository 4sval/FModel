using FModel.Forms;
using FModel.Methods;
using FModel.Methods.AESManager;
using FModel.Methods.BackupsManager;
using FModel.Methods.PAKs;
using FModel.Methods.TreeViewModel;
using FModel.Methods.Utilities;
using Newtonsoft.Json;
using PakReader;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace FModel
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SortedTreeViewWindowViewModel ViewModel { get { return DataContext as SortedTreeViewWindowViewModel; } set { DataContext = value; } }

        public MainWindow()
        {
            InitializeComponent();
            FWindow.FMain = this;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FModelVersionLabel.Text += Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);

            await Task.Run(() => 
            {
                FoldersUtility.LoadFolders();
                RegisterFromPath.FilterPAKs();
                DynamicKeysChecker.SetDynamicKeys();
                RegisterDownloadedBackups.LoadBackupFiles();
            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });
        }

        #region BUTTON EVENTS
        private void Button_AESManager_Click(object sender, RoutedEventArgs e)
        {
            if (!FormsUtility.IsWindowOpen<Window>("AES Manager"))
            {
                new AESManager().Show();
            }
            else { FormsUtility.GetOpenedWindow<Window>("AES Manager").Focus(); }
        }
        #endregion

        #region MENU ITEM EVENTS
        public void MI_Pak_Click(object sender, RoutedEventArgs e)
        {
            FWindow.FCurrentPAK = (sender as MenuItem).Header.ToString();
            PAKsLoader.LoadOnePAK();
        }
        private void MI_LoadAllPAKs_Click(object sender, RoutedEventArgs e)
        {
            FWindow.FCurrentPAK = string.Empty;
            PAKsLoader.LoadAllPAKs();
        }
        private void MI_BackupPAKs_Click(object sender, RoutedEventArgs e)
        {
            BackupPAKs.CreateBackupFile();
        }
        private void MI_Settings_Click(object sender, RoutedEventArgs e)
        {
            if (!FormsUtility.IsWindowOpen<Window>("Settings"))
            {
                new FModel_Settings().Show();
            }
            else { FormsUtility.GetOpenedWindow<Window>("Settings").Focus(); }
        }
        private void MI_About_Click(object sender, RoutedEventArgs e)
        {
            if (!FormsUtility.IsWindowOpen<Window>("About"))
            {
                new FModel_About().Show();
            }
            else { FormsUtility.GetOpenedWindow<Window>("About").Focus(); }
        }
        private void MI_OpenOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            FoldersUtility.OpenOutputFolder();
        }
        #endregion

        #region TREEVIEW EVENTS
        private void NodeSelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem currContainer = e.OriginalSource as TreeViewItem;
            if (currContainer != null)
            {
                ListBoxUtility.PopulateListBox(currContainer);
            }

        }
        #endregion

        #region LISTBOX EVENTS
        private void ListBox_Main_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Button_Extract.IsEnabled = ListBox_Main.SelectedIndex >= 0;
        }
        private void ListBox_Main_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                PakReader.PakReader reader = AssetEntries.AssetEntriesDict
                    .Where(x => string.Equals(FoldersUtility.GetFullPathWithoutExtension(x.Key.Name), FWindow.FCurrentAssetParentPath + "/" + ListBox_Main.SelectedItem))
                    .Select(x => x.Value).FirstOrDefault();

                if (reader != null)
                {
                    Stream asset = null;
                    Stream exp = null;
                    Stream bulk = null;

                    foreach (FPakEntry entry in reader.FileInfos)
                    {
                        if (entry.Name.Contains(ListBox_Main.SelectedItem.ToString()))
                        {
                            if (entry.Name.Contains(".uasset"))
                            {
                                asset = reader.GetPackageStream(entry as BasePakEntry);
                            }
                            else if (entry.Name.Contains(".uexp"))
                            {
                                exp = reader.GetPackageStream(entry as BasePakEntry);
                            }
                            else if (entry.Name.Contains(".ubulk"))
                            {
                                bulk = reader.GetPackageStream(entry as BasePakEntry);
                            }
                        }
                    }

                    if (asset != null && exp != null)
                    {
                        AssetReader json = new AssetReader(asset, exp, bulk != null ? bulk : null);

                        FlowDocument myFlowDoc = new FlowDocument();
                        myFlowDoc.Blocks.Add(new Paragraph(new Run(JsonConvert.SerializeObject(json.Exports[0], Formatting.Indented))));
                        AssetPropertiesBox_Main.Document = myFlowDoc;
                    }
                }
            }
        }
        #endregion
    }
}
