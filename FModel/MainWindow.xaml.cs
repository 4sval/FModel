using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
using FModel.Views;
using FModel.Views.Resources.Controls;
using ICSharpCode.AvalonEdit.Editing;

namespace FModel;

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
        CommandBindings.Add(new CommandBinding(new RoutedCommand("ReloadMappings", typeof(MainWindow), new InputGestureCollection { new KeyGesture(Key.F12) }), OnMappingsReload));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (_, _) => OnOpenAvalonFinder()));
        CommandBindings.Add(new CommandBinding(NavigationCommands.BrowseBack, (_, _) =>
        {
            if (!_applicationView.IsAssetsExplorerVisible)
            {
                // back browsing the json view will reopen the assets explorer
                _applicationView.IsAssetsExplorerVisible = true;
                return;
            }

            if (AssetsFolderName.SelectedItem is not TreeItem { Parent: { } parent })
                return;

            // back browsing the assets tree will select the parent folder
            parent.IsSelected = true;
        }));

        DataContext = _applicationView;
        InitializeComponent();

        AssetsExplorer.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        AssetsExplorer.SelectionChanged += (s, e) =>
        {
            foreach (var added in e.AddedItems.OfType<GameFileViewModel>())
            {
                if (!AssetsListName.SelectedItems.Contains(added))
                    AssetsListName.SelectedItems.Add(added);
            }

            foreach (var removed in e.RemovedItems.OfType<GameFileViewModel>())
            {
                if (AssetsListName.SelectedItems.Contains(removed))
                    AssetsListName.SelectedItems.Remove(removed);
            }
        };

        FLogger.Logger = LogRtbName;
        YesWeCats = this;
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        _discordHandler.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var newOrUpdated = UserSettings.Default.ShowChangelog;
#if !DEBUG
        ApplicationService.ApiEndpointView.FModelApi.CheckForUpdates(true);
#endif

        switch (UserSettings.Default.AesReload)
        {
            case EAesReload.Always:
                await _applicationView.CUE4Parse.RefreshAes();
                break;
            case EAesReload.OncePerDay when UserSettings.Default.CurrentDir.LastAesReload != DateTime.Today:
                UserSettings.Default.CurrentDir.LastAesReload = DateTime.Today;
                await _applicationView.CUE4Parse.RefreshAes();
                break;
        }

        await ApplicationViewModel.InitOodle();
        await ApplicationViewModel.InitZlib();
        await _applicationView.CUE4Parse.Initialize();
        await _applicationView.AesManager.InitAes();
        await _applicationView.UpdateProvider(true);
#if !DEBUG
        await _applicationView.CUE4Parse.InitInformation();
#endif
        await Task.WhenAll(
            _applicationView.CUE4Parse.VerifyConsoleVariables(),
            _applicationView.CUE4Parse.VerifyOnDemandArchives(),
            _applicationView.CUE4Parse.InitMappings(),
            ApplicationViewModel.InitDetex(),
            ApplicationViewModel.InitVgmStream(),
            ApplicationViewModel.InitImGuiSettings(newOrUpdated),
            Task.Run(() =>
            {
                if (UserSettings.Default.DiscordRpc == EDiscordRpc.Always)
                    _discordHandler.Initialize(_applicationView.GameDisplayName);
            })
        ).ConfigureAwait(false);

#if DEBUG
        // await _threadWorkerView.Begin(cancellationToken =>
        //     _applicationView.CUE4Parse.Extract(cancellationToken,
        //         _applicationView.CUE4Parse.Provider["Marvel/Content/Marvel/Wwise/Assets/Events/Music/music_new/event/Entry.uasset"]));
#endif
    }

    private void OnGridSplitterDoubleClick(object sender, MouseButtonEventArgs e)
    {
        RootGrid.ColumnDefinitions[0].Width = GridLength.Auto;
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is TextBox || e.OriginalSource is TextArea && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            return;

        if (_threadWorkerView.CanBeCanceled && e.Key == Key.Escape)
        {
            _applicationView.Status.SetStatus(EStatusKind.Stopping);
            _threadWorkerView.Cancel();
        }
        else if (_applicationView.Status.IsReady && e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            OnSearchViewClick(null, null);
        else if (_applicationView.Status.IsReady && e.Key == Key.R && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            OnRefViewClick(null, null);
        else if (e.Key == Key.Left && _applicationView.CUE4Parse.TabControl.SelectedTab.HasImage)
            _applicationView.CUE4Parse.TabControl.SelectedTab.GoPreviousImage();
        else if (e.Key == Key.Right && _applicationView.CUE4Parse.TabControl.SelectedTab.HasImage)
            _applicationView.CUE4Parse.TabControl.SelectedTab.GoNextImage();
        else if (UserSettings.Default.AssetAddTab.IsTriggered(e.Key))
            _applicationView.CUE4Parse.TabControl.AddTab();
        else if (UserSettings.Default.AssetRemoveTab.IsTriggered(e.Key))
            _applicationView.CUE4Parse.TabControl.RemoveTab();
        else if (UserSettings.Default.AssetLeftTab.IsTriggered(e.Key))
            _applicationView.CUE4Parse.TabControl.GoLeftTab();
        else if (UserSettings.Default.AssetRightTab.IsTriggered(e.Key))
            _applicationView.CUE4Parse.TabControl.GoRightTab();
        else if (UserSettings.Default.DirLeftTab.IsTriggered(e.Key) && _applicationView.SelectedLeftTabIndex > 0)
            _applicationView.SelectedLeftTabIndex--;
        else if (UserSettings.Default.DirRightTab.IsTriggered(e.Key) && _applicationView.SelectedLeftTabIndex < LeftTabControl.Items.Count - 1)
            _applicationView.SelectedLeftTabIndex++;
    }

    private void OnSearchViewClick(object sender, RoutedEventArgs e)
    {
        var searchView = Helper.GetWindow<SearchView>("Search View", () => new SearchView().Show());
        searchView.FocusTab(ESearchVeiwTab.SearchView);
    }

    private void OnRefViewClick(object sender, RoutedEventArgs e)
    {
        var searchView = Helper.GetWindow<SearchView>("Search View", () => new SearchView().Show());
        searchView.FocusTab(ESearchVeiwTab.RefView);
    }

    private void OnTabItemChange(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource is not TabControl tabControl)
            return;

        (tabControl.SelectedItem as System.Windows.Controls.TabItem)?.Focus();
    }

    private async void OnMappingsReload(object sender, ExecutedRoutedEventArgs e)
    {
        await _applicationView.CUE4Parse.InitMappings(true);
    }

    private void OnOpenAvalonFinder()
    {
        _applicationView.CUE4Parse.TabControl.SelectedTab.HasSearchOpen = true;
        AvalonEditor.YesWeSearch.Focus();
        AvalonEditor.YesWeSearch.SelectAll();
    }

    private void OnAssetsTreeMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TreeView { SelectedItem: TreeItem treeItem } || treeItem.Folders.Count > 0) return;

        _applicationView.SelectedLeftTabIndex++;
    }

    private void OnAssetsTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (sender is not TreeView { SelectedItem: TreeItem }) return;
        if (AssetsExplorer?.Template?.FindName("AssetPopup", AssetsExplorer) is Popup popup)
            popup.IsOpen = false;

        _applicationView.IsAssetsExplorerVisible = true;
        _applicationView.SelectedLeftTabIndex = 1;
    }

    private void OnPreviewTexturesToggled(object sender, RoutedEventArgs e) => ItemContainerGenerator_StatusChanged(AssetsExplorer.ItemContainerGenerator, EventArgs.Empty);
    private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
    {
        if (!_applicationView.IsAssetsExplorerVisible)
            return;

        var generator = AssetsExplorer.ItemContainerGenerator;
        if (generator.Status != GeneratorStatus.ContainersGenerated)
            return;

        for (int i = 0; i < AssetsExplorer.Items.Count; i++)
        {
            var item = AssetsExplorer.Items[i] as GameFileViewModel;
            if (item is not { PreviewImage: null })
                continue;

            if (generator.ContainerFromIndex(i) is FrameworkElement { IsVisible: true })
            {
                item.OnVisibleChanged(true);
            }
        }
    }

    private async void OnAssetsListMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox) return;

        var selectedItems = listBox.SelectedItems.Cast<GameFileViewModel>().Select(gvm => gvm.Asset).ToList();
        await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.ExtractSelected(cancellationToken, selectedItems); });
    }

    private void OnClearFilterClick(object sender, RoutedEventArgs e)
    {
        if (AssetsFolderName.SelectedItem is TreeItem folder)
        {
            folder.SearchText = string.Empty;
            folder.SelectedCategory = EAssetCategory.All;
        }
    }

    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!_applicationView.Status.IsReady || sender is not ListBox listBox) return;
        UserSettings.Default.LoadingMode = ELoadingMode.Multiple;
        _applicationView.LoadingModes.LoadCommand.Execute(listBox.SelectedItems);
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_applicationView.Status.IsReady || sender is not ListBox listBox) return;

        switch (e.Key)
        {
            case Key.Enter:
                _applicationView.IsAssetsExplorerVisible = false;
                var selectedItems = listBox.SelectedItems.Cast<GameFileViewModel>().Select(gvm => gvm.Asset).ToList();
                await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.ExtractSelected(cancellationToken, selectedItems); });
                break;
        }
    }
}
