using FModel.Forms;
using FModel.Methods;
using FModel.Methods.AESManager;
using FModel.Methods.Assets;
using FModel.Methods.BackupsManager;
using FModel.Methods.PAKs;
using FModel.Methods.SyntaxHighlighter;
using FModel.Methods.TreeViewModel;
using FModel.Methods.Utilities;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

            //TODO: INI VERSION + HYPERLINKS ARE TOO DARK
            AssetPropertiesBox_Main.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Json.xshd");

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
        private void Button_Export_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString();
                AssetsLoader.LoadSelectedAsset();
            }
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
            Button_Export.IsEnabled = ListBox_Main.SelectedIndex >= 0;
        }
        private void ListBox_Main_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {   
            if (ListBox_Main.SelectedIndex >= 0)
            {
                FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString();
                AssetsLoader.LoadSelectedAsset();
            }
        }
        #endregion
    }
}
