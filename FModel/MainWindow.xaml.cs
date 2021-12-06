using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdonisUI.Controls;
using FModel.Extensions;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
using FModel.Views;
using FModel.Views.Resources.Controls;
using ICSharpCode.AvalonEdit.Editing;

namespace FModel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow YesWeCats;
        private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
        private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
        private DiscordHandler _discordHandler => DiscordService.DiscordHandler;

        public MainWindow()
        {
            CommandBindings.Add(new CommandBinding(new RoutedCommand("AutoExportData", typeof(MainWindow), new InputGestureCollection
                {new KeyGesture(UserSettings.Default.AutoExportData.Key, UserSettings.Default.AutoExportData.Modifiers)}), OnAutoTriggerExecuted));
            CommandBindings.Add(new CommandBinding(new RoutedCommand("AutoSaveProps", typeof(MainWindow), new InputGestureCollection
                {new KeyGesture(UserSettings.Default.AutoSaveProps.Key, UserSettings.Default.AutoSaveProps.Modifiers)}), OnAutoTriggerExecuted));
            CommandBindings.Add(new CommandBinding(new RoutedCommand("AutoSaveTextures", typeof(MainWindow), new InputGestureCollection
                {new KeyGesture(UserSettings.Default.AutoSaveTextures.Key, UserSettings.Default.AutoSaveTextures.Modifiers)}), OnAutoTriggerExecuted));
            CommandBindings.Add(new CommandBinding(new RoutedCommand("AutoSaveAnimations", typeof(MainWindow), new InputGestureCollection
                {new KeyGesture(UserSettings.Default.AutoSaveAnimations.Key, UserSettings.Default.AutoSaveAnimations.Modifiers)}), OnAutoTriggerExecuted));
            CommandBindings.Add(new CommandBinding(new RoutedCommand("AutoOpenSounds", typeof(MainWindow), new InputGestureCollection
                {new KeyGesture(UserSettings.Default.AutoOpenSounds.Key, UserSettings.Default.AutoOpenSounds.Modifiers)}), OnAutoTriggerExecuted));
            CommandBindings.Add(new CommandBinding(new RoutedCommand("ReloadMappings", typeof(MainWindow), new InputGestureCollection {new KeyGesture(Key.F12)}), OnMappingsReload));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (s, e) => OnOpenAvalonFinder()));

            DataContext = _applicationView;
            InitializeComponent();

            FLogger.Logger = LogRtbName;
            YesWeCats = this;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            _applicationView.CustomDirectories.Save();
            _discordHandler.Dispose();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            ApplicationService.ApiEndpointView.FModelApi.CheckForUpdates(UserSettings.Default.UpdateMode);
#endif

            switch (UserSettings.Default.AesReload)
            {
                case EAesReload.Always:
                    await _applicationView.CUE4Parse.RefreshAes();
                    break;
                case EAesReload.OncePerDay when UserSettings.Default.LastAesReload != DateTime.Today:
                    UserSettings.Default.LastAesReload = DateTime.Today;
                    await _applicationView.CUE4Parse.RefreshAes();
                    break;
            }

            await _applicationView.CUE4Parse.InitInformation();
            await _applicationView.CUE4Parse.Initialize();
            await _applicationView.AesManager.InitAes();
            await _applicationView.AesManager.UpdateProvider(true);
            await _applicationView.CUE4Parse.InitBenMappings();
            await _applicationView.InitVgmStream();
            await _applicationView.InitOodle();

            if (UserSettings.Default.DiscordRpc == EDiscordRpc.Always)
                _discordHandler.Initialize(_applicationView.CUE4Parse.Game);
        }

        private void OnGridSplitterDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RootGrid.ColumnDefinitions[0].Width = GridLength.Auto;
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is TextArea or TextBox)
                return;

            if (_threadWorkerView.CanBeCanceled && e.Key == Key.Escape)
            {
                _applicationView.Status = EStatusKind.Stopping;
                _threadWorkerView.Cancel();
            }
            else if (_applicationView.IsReady && e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                OnSearchViewClick(null, null);
            else if (UserSettings.Default.AssetAddTab.IsTriggered(e.Key))
                _applicationView.CUE4Parse.TabControl.AddTab();
            else if (UserSettings.Default.AssetRemoveTab.IsTriggered(e.Key))
                _applicationView.CUE4Parse.TabControl.RemoveTab();
            else if (UserSettings.Default.AssetLeftTab.IsTriggered(e.Key))
                _applicationView.CUE4Parse.TabControl.GoLeftTab();
            else if (UserSettings.Default.AssetRightTab.IsTriggered(e.Key))
                _applicationView.CUE4Parse.TabControl.GoRightTab();
            else if (UserSettings.Default.DirLeftTab.IsTriggered(e.Key) && LeftTabControl.SelectedIndex > 0)
                LeftTabControl.SelectedIndex--;
            else if (UserSettings.Default.DirRightTab.IsTriggered(e.Key) && LeftTabControl.SelectedIndex < LeftTabControl.Items.Count - 1)
                LeftTabControl.SelectedIndex++;
        }

        private void OnSearchViewClick(object sender, RoutedEventArgs e)
        {
            Helper.OpenWindow<AdonisWindow>("Search View", () => new SearchView().Show());
        }

        private void OnTabItemChange(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is not TabControl tabControl)
                return;

            (tabControl.SelectedItem as System.Windows.Controls.TabItem)?.Focus();
        }

        private async void OnMappingsReload(object sender, ExecutedRoutedEventArgs e)
        {
            await _applicationView.CUE4Parse.InitBenMappings();
        }

        private void OnAutoTriggerExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            switch ((e.Command as RoutedCommand)?.Name)
            {
                case "AutoExportData":
                    UserSettings.Default.IsAutoExportData = !UserSettings.Default.IsAutoExportData;
                    break;
                case "AutoSaveProps":
                    UserSettings.Default.IsAutoSaveProps = !UserSettings.Default.IsAutoSaveProps;
                    break;
                case "AutoSaveTextures":
                    UserSettings.Default.IsAutoSaveTextures = !UserSettings.Default.IsAutoSaveTextures;
                    break;
                case "AutoSaveAnimations":
                    UserSettings.Default.IsAutoSaveAnimations = !UserSettings.Default.IsAutoSaveAnimations;
                    break;
                case "AutoOpenSounds":
                    UserSettings.Default.IsAutoOpenSounds = !UserSettings.Default.IsAutoOpenSounds;
                    break;
            }
        }

        private void OnOpenAvalonFinder()
        {
            _applicationView.CUE4Parse.TabControl.SelectedTab.HasSearchOpen = true;
            AvalonEditor.YesWeSearch.Focus();
            AvalonEditor.YesWeSearch.SelectAll();
        }

        private void OnAssetsTreeMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TreeView {SelectedItem: TreeItem treeItem} || treeItem.Folders.Count > 0) return;

            LeftTabControl.SelectedIndex++;
        }

        private async void OnAssetsListMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox listBox) return;

            var selectedItems = listBox.SelectedItems.Cast<AssetItem>().ToList();
            await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.ExtractSelected(cancellationToken, selectedItems); });
        }

        private async void OnFolderExtractClick(object sender, RoutedEventArgs e)
        {
            if (AssetsFolderName.SelectedItem is TreeItem folder)
            {
                await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.ExtractFolder(cancellationToken, folder); });
            }
        }

        private async void OnFolderExportClick(object sender, RoutedEventArgs e)
        {
            if (AssetsFolderName.SelectedItem is TreeItem folder)
            {
                await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.ExportFolder(cancellationToken, folder); });
            }
        }

        private async void OnFolderSaveClick(object sender, RoutedEventArgs e)
        {
            if (AssetsFolderName.SelectedItem is TreeItem folder)
            {
                await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.SaveFolder(cancellationToken, folder); });
            }
        }

        private void OnSaveDirectoryClick(object sender, RoutedEventArgs e)
        {
            if (AssetsFolderName.SelectedItem is not TreeItem folder) return;

            _applicationView.CustomDirectories.Add(new CustomDirectory(folder.Header, folder.PathAtThisPoint));
            FLogger.AppendInformation();
            FLogger.AppendText($"Successfully saved '{folder.PathAtThisPoint}' as a new custom directory", Constants.WHITE, true);
        }

        private void OnCopyDirectoryPathClick(object sender, RoutedEventArgs e)
        {
            if (AssetsFolderName.SelectedItem is not TreeItem folder) return;
            Clipboard.SetText(folder.PathAtThisPoint);
        }

        private void OnDeleteSearchClick(object sender, RoutedEventArgs e)
        {
            AssetsSearchName.Text = string.Empty;
            AssetsListName.ScrollIntoView(AssetsListName.SelectedItem);
        }

        private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || AssetsFolderName.SelectedItem is not TreeItem folder)
                return;

            var filters = textBox.Text.Trim().Split(' ');
            folder.AssetsList.AssetsView.Filter = o =>
            {
                return o is AssetItem assetItem && filters.All(x => assetItem.FullPath.SubstringAfterLast('/').Contains(x, StringComparison.OrdinalIgnoreCase));
            };
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!_applicationView.IsReady || sender is not ListBox listBox) return;
            UserSettings.Default.LoadingMode = ELoadingMode.Multiple;
            _applicationView.LoadingModes.LoadCommand.Execute(listBox.SelectedItems);
        }

        private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_applicationView.IsReady || sender is not ListBox listBox) return;

            switch (e.Key)
            {
                case Key.Enter:
                    var selectedItems = listBox.SelectedItems.Cast<AssetItem>().ToList();
                    await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.ExtractSelected(cancellationToken, selectedItems); });
                    break;
            }
        }
    }
}
