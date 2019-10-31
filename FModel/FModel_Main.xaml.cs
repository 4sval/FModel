using AutoUpdaterDotNET;
using FModel.Forms;
using FModel.Forms.HexViewer;
using FModel.Methods;
using FModel.Methods.AESManager;
using FModel.Methods.Assets;
using FModel.Methods.BackupsManager;
using FModel.Methods.PAKs;
using FModel.Methods.TreeViewModel;
using FModel.Methods.Utilities;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FProp = FModel.Properties.Settings;

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
            this.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
            FWindow.FMain = this;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FModelVersionLabel.Text += Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);

            AutoUpdater.CheckForUpdateEvent += UIHelper.AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.Start("https://dl.dropbox.com/s/3kv2pukqu6tj1r0/FModel.xml?dl=0");

            // Copy user settings from previous application version if necessary
            if (FProp.Default.FUpdateSettings)
            {
                FProp.Default.Upgrade();
                FProp.Default.FUpdateSettings = false;
                FProp.Default.Save();
            }

            await Task.Run(() => 
            {
                FoldersUtility.LoadFolders();
                FoldersUtility.CheckWatermark();
                RegisterFromPath.FilterPAKs();
                DynamicKeysChecker.SetDynamicKeys();
                RegisterDownloadedBackups.LoadBackupFiles();
            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });

            AvalonEdit.SetAEConfig();
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
        private void Button_OpenImage_Click(object sender, RoutedEventArgs e)
        {
            if (ImageBox_Main.Source != null)
            {
                if (!FormsUtility.IsWindowOpen<Window>(FWindow.FCurrentAsset))
                {
                    Window win = new Window();
                    win.Title = FWindow.FCurrentAsset;
                    win.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
                    win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    win.Width = ImageBox_Main.Source.Width;
                    win.Height = ImageBox_Main.Source.Height;
                    if (ImageBox_Main.Source.Height > 1000)
                    {
                        win.WindowState = WindowState.Maximized;
                    }

                    DockPanel dockPanel = new DockPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    Image img = new Image();
                    img.UseLayoutRounding = true;
                    img.Source = ImageBox_Main.Source;
                    dockPanel.Children.Add(img);

                    win.Content = dockPanel;
                    win.Show();
                }
                else { FormsUtility.GetOpenedWindow<Window>(FWindow.FCurrentAsset).Focus(); }
            }
        }
        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            if (TasksUtility.CancellableTaskTokenSource != null)
            {
                TasksUtility.CancellableTaskTokenSource.Cancel();
                if (TasksUtility.CancellableTaskTokenSource.IsCancellationRequested)
                {
                    new UpdateMyProcessEvents("Canceled!", "Yikes").Update();
                }
                else { new UpdateMyProcessEvents("This is odd!\tCanceled but not requested. You should never see this tbh", "Yikes").Update(); }
            }
        }
        private async void Button_Extract_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                //FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString(); <-- already in the 'Load' loop
                await AssetsLoader.LoadSelectedAsset();
            }
        }
        #endregion

        #region MENU ITEM EVENTS
        public async void MI_Pak_Click(object sender, RoutedEventArgs e)
        {
            FWindow.FCurrentPAK = (sender as MenuItem).Header.ToString();
            await PAKsLoader.LoadOnePAK();
        }
        private async void MI_LoadAllPAKs_Click(object sender, RoutedEventArgs e)
        {
            FWindow.FCurrentPAK = string.Empty;

            //LOAD ALL
            if (!MI_DifferenceMode.IsChecked && !MI_UpdateMode.IsChecked)
            {
                await PAKsLoader.LoadAllPAKs();
            }
            
            //LOAD DIFF
            if (MI_DifferenceMode.IsChecked && !MI_UpdateMode.IsChecked)
            {
                await PAKsLoader.LoadDifference();
            }

            //LOAD AND EXTRACT DIFF
            if (MI_DifferenceMode.IsChecked && MI_UpdateMode.IsChecked)
            {
                //not done yet
            }
        }
        private void MI_ReloadAESs_Click(object sender, RoutedEventArgs e)
        {
            DynamicKeysChecker.SetDynamicKeys(true);
        }
        private async void MI_BackupPAKs_Click(object sender, RoutedEventArgs e)
        {
            await BackupPAKs.CreateBackupFile();
        }
        private void MI_Settings_Click(object sender, RoutedEventArgs e)
        {
            if (!FormsUtility.IsWindowOpen<Window>("Settings"))
            {
                new FModel_Settings().Show();
            }
            else { FormsUtility.GetOpenedWindow<Window>("Settings").Focus(); }
        }
        private void MI_Search_Click(object sender, RoutedEventArgs e)
        {
            if (!FormsUtility.IsWindowOpen<Window>("Search"))
            {
                new FModel_SearchFiles().Show();
            }
            else { FormsUtility.GetOpenedWindow<Window>("Search").Focus(); }
        }
        private void MI_HexViewer_Click(object sender, RoutedEventArgs e)
        {
            if (!FormsUtility.IsWindowOpen<Window>("Hex Viewer"))
            {
                new HexViewer().Show();
            }
            else { FormsUtility.GetOpenedWindow<Window>("Hex Viewer").Focus(); }
        }
        private void MI_ExportRaw_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString();
                AssetsUtility.ExportAssetData();
            }
        }
        private void MI_SaveJson_Click(object sender, RoutedEventArgs e)
        {
            AssetsUtility.SaveAssetProperties();
        }
        private void MI_OpenOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            FoldersUtility.OpenOutputFolder();
        }
        private void MI_Save_Image_Click(object sender, RoutedEventArgs e)
        {
            if (ImageBox_Main.Source != null)
            {
                ImagesUtility.SaveImageDialog();
            }
        }
        private void MI_MergeImages_Click(object sender, RoutedEventArgs e)
        {
            if (!FormsUtility.IsWindowOpen<Window>("Images Merger"))
            {
                new FModel_ImagesMerger().Show();
            }
            else { FormsUtility.GetOpenedWindow<Window>("Images Merger").Focus(); }
        }
        private void MI_About_Click(object sender, RoutedEventArgs e)
        {
            if (!FormsUtility.IsWindowOpen<Window>("About"))
            {
                new FModel_About().Show();
            }
            else { FormsUtility.GetOpenedWindow<Window>("About").Focus(); }
        }
        private void MI_Change_Header(object sender, RoutedEventArgs e)
        {
            //DIFFERENCE MODE
            if (MI_DifferenceMode.IsChecked)
            {
                MI_LoadOnePAK.IsEnabled = false;
                MI_LoadAllPAKs.Header = "Load Difference";
                MI_UpdateMode.IsEnabled = true;
            }
            if (!MI_DifferenceMode.IsChecked)
            {
                MI_LoadOnePAK.IsEnabled = true;
                MI_UpdateMode.IsEnabled = false;
                MI_UpdateMode.IsChecked = false;
            }

            //UPDATE MODE
            if (MI_UpdateMode.IsChecked)
            {
                MI_LoadAllPAKs.Header = "Load And Extract Difference";
                MI_UpdateMode.IsEnabled = true;
            }
            if (!MI_UpdateMode.IsChecked)
            {
                MI_LoadAllPAKs.Header = "Load Difference";
            }

            //BOTH
            if (!MI_DifferenceMode.IsChecked && !MI_UpdateMode.IsChecked)
            {
                MI_LoadAllPAKs.Header = "Load All PAKs";
            }
        }
        #endregion

        #region TREEVIEW EVENTS
        private async void NodeSelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem currContainer = e.OriginalSource as TreeViewItem;
            if (currContainer != null)
            {
                FWindow.TVItem = currContainer;
                await ListBoxUtility.PopulateListBox(currContainer);
            }

        }
        private async void RC_ExtractFolders_Click(object sender, RoutedEventArgs e)
        {
            if (TreeView_Main.SelectedItem != null)
            {
                string path = TreeViewUtility.GetFullPath(FWindow.TVItem);
                await AssetsLoader.ExtractFoldersAndSub(path);
            }
        }
        #endregion

        #region LISTBOX EVENTS
        private void ListBox_Main_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0) { ((ListBox)sender).ScrollIntoView(e.AddedItems[0]); }
            if (!AssetsLoader.isRunning) { Button_Extract.IsEnabled = ListBox_Main.SelectedIndex >= 0; }
        }
        private async void ListBox_Main_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {   
            if (!AssetsLoader.isRunning && ListBox_Main.SelectedIndex >= 0)
            {
                //FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString(); <-- already in the 'Load' loop
                await AssetsLoader.LoadSelectedAsset();
            }
        }
        private async void FilterTextBox_Main_TextChanged(object sender, TextChangedEventArgs e)
        {
            await ListBoxUtility.FilterListBox();
        }
        private async void RC_Extract_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                //FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString(); <-- already in the 'Load' loop
                await AssetsLoader.LoadSelectedAsset();
            }
        }
        private void RC_ExportData_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString();
                AssetsUtility.ExportAssetData();
            }
        }
        private void RC_SaveData_Click(object sender, RoutedEventArgs e)
        {
            AssetsUtility.SaveAssetProperties();
        }
        private void RC_Copy_FPath_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString();
                Clipboard.SetText(AssetsUtility.GetAssetPathToCopy());
            }
        }
        private void RC_Copy_FName_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString();
                Clipboard.SetText(AssetsUtility.GetAssetPathToCopy(true));
            }
        }
        private void RC_Copy_FPath_NoExt_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString();
                Clipboard.SetText(AssetsUtility.GetAssetPathToCopy(false, false));
            }
        }
        private void RC_Copy_FName_NoExt_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString();
                Clipboard.SetText(AssetsUtility.GetAssetPathToCopy(true, false));
            }
        }
        private void RC_Properties_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Main.SelectedIndex >= 0)
            {
                FWindow.FCurrentAsset = ListBox_Main.SelectedItem.ToString();
                AssetInformations.OpenAssetInfos();
            }
        }
        #endregion
    }
}
