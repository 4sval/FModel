using FModel.Discord;
using FModel.Grabber.Aes;
using FModel.Grabber.Cdn;
using FModel.Grabber.Paks;
using FModel.Logger;
using FModel.Utils;
using FModel.ViewModels.AvalonEdit;
using FModel.ViewModels.Buttons;
using FModel.ViewModels.ImageBox;
using FModel.ViewModels.ListBox;
using FModel.ViewModels.MenuItem;
using FModel.ViewModels.StatusBar;
using FModel.ViewModels.TabControl;
using FModel.ViewModels.Treeview;
using FModel.Windows.About;
using FModel.Windows.AESManager;
using FModel.Windows.AvalonEditFindReplace;
using FModel.Windows.CustomNotifier;
using FModel.Windows.DarkMessageBox;
using FModel.Windows.ImagesMerger;
using FModel.Windows.Launcher;
using FModel.Windows.Search;
using FModel.Windows.Settings;
using FModel.Windows.SoundPlayer;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FModel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            FConsole.fConsoleControl = FModel_Console;
        }

        private async void OnClosing(object sender, CancelEventArgs e)
        {
            Globals.gNotifier.Dispose();
            Tasks.TokenSource?.Dispose();
            DiscordIntegration.Dispose();
            DebugHelper.Logger.AsyncWrite = false;
            await Properties.Settings.SaveToFile().ConfigureAwait(false);
        }
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            FModelVersion_TxtBlck.Text += Assembly.GetExecutingAssembly().GetName().Version.ToString();
            FModel_StsBar.DataContext = StatusBarVm.statusBarViewModel;
            FModel_AvalonEdit.DataContext = AvalonEditVm.avalonEditViewModel;
            FModel_ImgBox.DataContext = ImageBoxVm.imageBoxViewModel;
            FModel_PakProps.DataContext = PakPropertiesVm.pakPropertiesViewModel;
            FModel_AssetProps.DataContext = AssetPropertiesVm.assetPropertiesViewModel;
            FModel_Extract_Btn.DataContext = ExtractStopVm.extractViewModel;
            FModel_Stop_Btn.DataContext = ExtractStopVm.stopViewModel;
            FModel_MI_Files_PAK.ItemsSource = MenuItems.pakFiles;
            FModel_MI_Files_Backups.ItemsSource = MenuItems.backupFiles;
            FModel_MI_Assets_GoTo.ItemsSource = MenuItems.customGoTos;
            FModel_AssetsPathTree.ItemsSource = SortedTreeviewVm.gameFilesPath.ChildrensView;

            if (!Properties.Settings.Default.SkipVersion) Updater.CheckForUpdate();
            DebugHelper.WriteUserSettings();
            Folders.CheckWatermarks();

            await Task.WhenAll(Init()).ContinueWith(t =>
            {
                Keys.NoKeyGoodBye();
                MenuItems.FeedCustomGoTos();
                AeConfiguration();

                if (t.Exception != null) Tasks.TaskCompleted(t.Exception);
                else StatusBarVm.statusBarViewModel.Set($"{Properties.Resources.Hello} {Environment.UserName}!", Properties.Resources.State);

                App.StartTimer.Stop();
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Startup Time]", $"{App.StartTimer.ElapsedMilliseconds}ms");
            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task Init()
        {
            await PaksGrabber.PopulateMenu().ConfigureAwait(false);
            if (Properties.Settings.Default.UseDiscordRpc) DiscordIntegration.StartClient();
            await AesGrabber.Load(Properties.Settings.Default.ReloadAesKeys).ConfigureAwait(false);
            await CdnDataGrabber.DoCDNStuff().ConfigureAwait(false);
            await Folders.DownloadAndExtractVgm().ConfigureAwait(false);
        }

        private void AeConfiguration()
        {
            AvalonEditFindReplaceHelper Frm = new AvalonEditFindReplaceHelper
            {
                CurrentEditor = new AvalonEditVm.TextEditorAdapter(FModel_AvalonEdit),
                ShowSearchIn = false,
                OwnerWindow = this
            };
            this.CommandBindings.Add(Frm.FindBinding);
        }

        #region MENU ITEMS
        private void OnAutoShortcutPressed(object sender, RoutedEventArgs e)
        {
            if (e is ExecutedRoutedEventArgs r && r.Command is RoutedUICommand command)
            {
                string name = string.Empty;
                string state = string.Empty;
                string[] states = new string[2] { Properties.Resources.Enabled, Properties.Resources.Disabled };
                switch (command.Name)
                {
                    case "AutoExport":
                        {
                            bool b = Properties.Settings.Default.AutoExport;
                            Properties.Settings.Default.AutoExport = !b;
                            name = Properties.Resources.Export;
                            state = states[Convert.ToInt32(b)];
                            break;
                        }
                    case "AutoSave":
                        {
                            bool b = Properties.Settings.Default.AutoSave;
                            Properties.Settings.Default.AutoSave = !b;
                            name = Properties.Resources.Save;
                            state = states[Convert.ToInt32(b)];
                            break;
                        }
                    case "AutoSaveImage":
                        {
                            bool b = Properties.Settings.Default.AutoSaveImage;
                            Properties.Settings.Default.AutoSaveImage = !b;
                            name = Properties.Resources.SaveImage;
                            state = states[Convert.ToInt32(b)];
                            break;
                        }
                }

                Properties.Settings.Default.Save();
                Globals.gNotifier.ShowCustomMessage(Properties.Resources.Auto, string.Format("{0}   🠞   {1}", name, state));
            }
        }
        private void FModel_MI_Files_AES_Click(object sender, RoutedEventArgs e)
        {
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Opening AES Manager");
            if (!FWindows.IsWindowOpen<Window>(Properties.Resources.AES))
            {
                new AESManager().Show();
            }
            else { FWindows.GetOpenedWindow<Window>(Properties.Resources.AES).Focus(); }
        }
        private void FModel_MI_Assets_Search_Click(object sender, RoutedEventArgs e)
        {
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Opening Searcher");
            if (!FWindows.IsWindowOpen<Window>(Properties.Resources.Search))
            {
                new Search().Show();
            }
            else
            {
                Window openedWindow = FWindows.GetOpenedWindow<Window>(Properties.Resources.Search);
                if (openedWindow.WindowState == WindowState.Minimized)
                    openedWindow.WindowState = WindowState.Normal;
                else openedWindow.Focus();
            }
        }
        private void FModel_MI_Assets_AudioPlayer_Click(object sender, RoutedEventArgs e)
        {
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Opening Audio Player");
            if (!FWindows.IsWindowOpen<Window>(Properties.Resources.AudioPlayer))
            {
                new AudioPlayer(); //no need to show, Show() is already in the constructor
            }
            else { FWindows.GetOpenedWindow<Window>(Properties.Resources.AudioPlayer).Focus(); }
        }
        private async void FModel_MI_Assets_Export_Click(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsList.HasItems && FModel_AssetsList.SelectedIndex >= 0 && FModel_AssetsList.SelectedItem is ListBoxViewModel selectedItem)
            {
                bool autoExport = FModel_AssetsList.SelectedItems.Count > 1;
                if (!autoExport) Assets.Export(selectedItem.PakEntry, false); // manual export if one
                else
                {
                    bool ret = Properties.Settings.Default.AutoExport;
                    Properties.Settings.Default.AutoExport = autoExport;

                    await Assets.GetUserSelection(FModel_AssetsList.SelectedItems); // auto export if multiple

                    Properties.Settings.Default.AutoExport = ret;
                    Properties.Settings.Default.Save();
                }
            }
            else Globals.gNotifier.ShowCustomMessage(Properties.Resources.Error, Properties.Resources.NoDataToExport);
        }
        private void FModel_MI_Assets_Save_Click(object sender, RoutedEventArgs e) => AvalonEditVm.avalonEditViewModel.Save(false);
        private void FModel_MI_Assets_CopyImage_Click(object sender, RoutedEventArgs e) => ImageBoxVm.imageBoxViewModel.Copy();
        private void FModel_MI_Assets_SaveImage_Click(object sender, RoutedEventArgs e) => ImageBoxVm.imageBoxViewModel.Save(false);
        private void FModel_MI_Assets_OpenOutputFolder_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = Properties.Settings.Default.OutputPath, UseShellExecute = true });
        private void FModel_MI_Assets_ImageMerger_Click(object sender, RoutedEventArgs e)
        {
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Opening Images Merger Settings");
            if (!FWindows.IsWindowOpen<Window>(Properties.Resources.ImagesMerger))
            {
                new ImagesMerger().Show();
            }
            else { FWindows.GetOpenedWindow<Window>(Properties.Resources.ImagesMerger).Focus(); }
        }
        private void FModel_MI_Settings_General_Click(object sender, RoutedEventArgs e)
        {
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Opening General Settings");
            if (!FWindows.IsWindowOpen<Window>(Properties.Resources.GeneralSettings))
            {
                new General().Show();
            }
            else { FWindows.GetOpenedWindow<Window>(Properties.Resources.GeneralSettings).Focus(); }
        }
        private void FModel_MI_Settings_IconCreator_Click(object sender, RoutedEventArgs e)
        {
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Opening Icon Creator Settings");
            if (!FWindows.IsWindowOpen<Window>(Properties.Resources.IconCreator))
            {
                new IconCreator().Show();
            }
            else { FWindows.GetOpenedWindow<Window>(Properties.Resources.IconCreator).Focus(); }
        }
        private void FModel_MI_Settings_ThemeCreator_Click(object sender, RoutedEventArgs e)
        {
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Opening Challenge Bundles Creator Settings");
            if (!FWindows.IsWindowOpen<Window>(Properties.Resources.ChallengesThemeCreator))
            {
                new ChallengeBundlesCreator().Show();
            }
            else { FWindows.GetOpenedWindow<Window>(Properties.Resources.ChallengesThemeCreator).Focus(); }
        }
        private void FModel_MI_Settings_RestoreLayout_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.GridParentTop = "11*";
            Properties.Settings.Default.GridParentBottom = "10*";
            Properties.Settings.Default.GridChildTopLeft = "2*";
            Properties.Settings.Default.GridChildTopCenter = "3*";
            Properties.Settings.Default.GridChildTopRight = "2*";
            Properties.Settings.Default.GridChildBottomLeft = "6*";
            Properties.Settings.Default.GridChildBottomCenter = "11*";
            Properties.Settings.Default.GridChildBottomRight = "6*";
        }
        private void FModel_MI_Settings_ChangeGame_Click(object sender, RoutedEventArgs e)
        {
            var launcher = new FLauncher();
            if ((bool)launcher.ShowDialog())
            {
                Properties.Settings.Default.PakPath = launcher.Path;

                DarkMessageBoxHelper.Show(Properties.Resources.PathChangedRestart, Properties.Resources.PathChanged, MessageBoxButton.OK, MessageBoxImage.Information);
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Restarting]", "Path(s) changed");

                Properties.Settings.Default.Save();
                Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                Application.Current.Shutdown();
            }
        }
        private void FModel_MI_Help_Trello_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://trello.com/b/DfmzkVQB/fmodel", UseShellExecute = true });
        private void FModel_MI_Help_Donate_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=EP9SSWG8MW4UC&source=url", UseShellExecute = true });
        private void FModel_MI_Help_Changelog_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://github.com/iAmAsval/FModel/releases/latest", UseShellExecute = true });
        private void FModel_MI_Help_BugsReport_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://github.com/iAmAsval/FModel/issues/new", UseShellExecute = true });
        private void FModel_MI_Help_Discord_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/fdkNYYQ", UseShellExecute = true });
        private void FModel_MI_Help_About_Click(object sender, RoutedEventArgs e)
        {
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Opening About");
            if (!FWindows.IsWindowOpen<Window>(Properties.Resources.AboutF))
            {
                new FAbout().Show();
            }
            else { FWindows.GetOpenedWindow<Window>(Properties.Resources.AboutF).Focus(); }
        }
        #endregion

        #region TREEVIEW
        private void OnSelectedPathChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            AssetFilter_TxtBox.Text = string.Empty;
            AssetPropertiesVm.assetPropertiesViewModel.Reset();
            if (sender is TreeView s && s.SelectedItem is TreeviewViewModel selectedItem)
            {
                string path = selectedItem.GetFullPath().Substring(1);
                if (selectedItem.GameFiles.ContainsKey(path))
                {
                    FModel_AssetsList.ItemsSource = // re-assigning ListBoxes.gameFiles delete the bind so we set it again
                        ListBoxVm.gameFiles = selectedItem.GameFiles[path]; // this might not be the best solution but idk what to do instead

                    if (Globals.bSearch && !string.IsNullOrEmpty(Globals.sSearch))
                    {
                        var selected = ListBoxVm.gameFiles.Where(x => x.Content.Equals(Globals.sSearch)).FirstOrDefault();
                        FModel_AssetsList.SelectedIndex = ListBoxVm.gameFiles.IndexOf(selected);
                        FModel_AssetsList.ScrollIntoView(selected);
                        Globals.bSearch = false;
                        Globals.sSearch = string.Empty;
                    }
                }
                else
                    FModel_AssetsList.ItemsSource = null;
            }
        }
        #endregion

        #region LISTBOX
        private void OnSelectedItemChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxViewModel selectedItem)
            {
                FModel_TabCtrl.SelectedIndex = 1;
                ExtractStopVm.extractViewModel.IsEnabled = true;
                AssetPropertiesVm.assetPropertiesViewModel.Set(selectedItem.PakEntry);
            }
            else
            {
                FModel_TabCtrl.SelectedIndex = 0;
                ExtractStopVm.extractViewModel.IsEnabled = false;
            }
        }
        private async void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedIndex >= 0)
                await Assets.GetUserSelection(listBox.SelectedItems);
        }
        private void OnDeleteFilterClick(object sender, RoutedEventArgs e)
        {
            AssetFilter_TxtBox.Text = string.Empty;
            if (FModel_AssetsList.HasItems && FModel_AssetsList.SelectedIndex >= 0 && FModel_AssetsList.SelectedItem is ListBoxViewModel selectedItem)
                FModel_AssetsList.ScrollIntoView(selectedItem);
        }
        private async void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string[] filters = textBox.Text.Trim().Split(' ');
                if (!string.IsNullOrEmpty(filters[0]))
                {
                    FilterDelete_Img.Visibility = Visibility.Visible;
                    var filtered = new ObservableSortedList<ListBoxViewModel>();
                    await Task.Run(() =>
                    {
                        foreach (ListBoxViewModel item in ListBoxVm.gameFiles)
                        {
                            bool bSearch = false;
                            if (filters.Length > 1)
                            {
                                foreach (string filter in filters)
                                {
                                    Assets.Filter(filter, item.Content, out bSearch);
                                    if (!bSearch)
                                        break;
                                }
                            }
                            else
                            {
                                Assets.Filter(filters[0], item.Content, out bSearch);
                            }

                            if (bSearch)
                                filtered.Add(item);
                        }
                    }).ContinueWith(t =>
                    {
                        if (t.Exception != null) Tasks.TaskCompleted(t.Exception);
                        else FModel_AssetsList.ItemsSource = filtered;
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    FilterDelete_Img.Visibility = Visibility.Hidden;
                    FModel_AssetsList.ItemsSource = ListBoxVm.gameFiles;
                }
            }
        }
        #endregion

        #region BUTTONS
        private void OnImageOpenClick(object sender, RoutedEventArgs e) => ImageBoxVm.imageBoxViewModel.OpenImage();
        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            if (Tasks.TokenSource != null)
            {
                Tasks.TokenSource.Cancel();
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Thread]", "Canceled by user");
            }
        }
        private async void OnExtractClick(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsList.SelectedIndex >= 0)
                await Assets.GetUserSelection(FModel_AssetsList.SelectedItems);
        }
        #endregion

        #region RIGHT CLICK MENUS
        private async void FModel_MI_Directory_Extract_Click(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsPathTree.HasItems && FModel_AssetsPathTree.SelectedItem is TreeviewViewModel treeItem)
                await SortedTreeviewVm.ExtractFolder(treeItem).ConfigureAwait(false);
        }
        private async void FModel_MI_Directory_Export_Click(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsPathTree.HasItems && FModel_AssetsPathTree.SelectedItem is TreeviewViewModel treeItem)
                await SortedTreeviewVm.ExportFolder(treeItem).ConfigureAwait(false);
        }
        private void FModel_MI_Directory_Save_Click(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsPathTree.HasItems && FModel_AssetsPathTree.SelectedItem is TreeviewViewModel treeItem)
                MenuItems.AddCustomGoTo(treeItem.Header, treeItem.GetFullPath().Substring(1) + "/");
        }
        private async void CM_Asset_Save_Click(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsList.HasItems && FModel_AssetsList.SelectedIndex >= 0 && FModel_AssetsList.SelectedItem is ListBoxViewModel selectedItem)
            {
                if (selectedItem.Content.Equals(AvalonEditVm.avalonEditViewModel.OwnerName)) // if selected item is actually displayed, just save
                    AvalonEditVm.avalonEditViewModel.Save(false);
                else // extract (aka display) and save
                {
                    bool autoSave = FModel_AssetsList.SelectedItems.Count > 1;
                    bool ret = Properties.Settings.Default.AutoSave;

                    Properties.Settings.Default.AutoSave = autoSave;
                    await Assets.GetUserSelection(FModel_AssetsList.SelectedItems); // auto save if multiple
                    if (!autoSave) AvalonEditVm.avalonEditViewModel.Save(false); // manual save if one

                    Properties.Settings.Default.AutoSave = ret;
                    Properties.Settings.Default.Save();
                }
            }
            else Globals.gNotifier.ShowCustomMessage(Properties.Resources.Error, Properties.Resources.NoDataToSave);
        }
        private void CM_Copy_DPath_Click(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsPathTree.HasItems && FModel_AssetsPathTree.SelectedItem is TreeviewViewModel treeItem)
                Assets.Copy(treeItem.GetFullPath().Substring(1) + "/");
            else if (FModel_AssetsList.HasItems && FModel_AssetsList.SelectedIndex >= 0)
                Assets.Copy(FModel_AssetsList.SelectedItems, ECopy.PathNoFile);
        }
        private void CM_Copy_FPath_Click(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsList.HasItems && FModel_AssetsList.SelectedIndex >= 0)
                Assets.Copy(FModel_AssetsList.SelectedItems, ECopy.Path);
        }
        private void CM_Copy_FName_Click(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsList.HasItems && FModel_AssetsList.SelectedIndex >= 0)
                Assets.Copy(FModel_AssetsList.SelectedItems, ECopy.File);
        }
        private void CM_Copy_FPath_NoExt_Click(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsList.HasItems && FModel_AssetsList.SelectedIndex >= 0)
                Assets.Copy(FModel_AssetsList.SelectedItems, ECopy.PathNoExt);
        }
        private void CM_Copy_FName_NoExt_Click(object sender, RoutedEventArgs e)
        {
            if (FModel_AssetsList.HasItems && FModel_AssetsList.SelectedIndex >= 0)
                Assets.Copy(FModel_AssetsList.SelectedItems, ECopy.FileNoExt);
        }
        #endregion
    }
}
