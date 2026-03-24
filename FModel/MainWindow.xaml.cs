using System;
using System.ComponentModel;
using System.IO;
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
            if (UserSettings.Default.FeaturePreviewNewAssetExplorer && !_applicationView.IsAssetsExplorerVisible)
            {
                // back browsing the json view will reopen the assets explorer
                _applicationView.IsAssetsExplorerVisible = true;
                return;
            }

            if (LeftTabControl.SelectedIndex == 2)
            {
                LeftTabControl.SelectedIndex = 1;
            }
            else if (LeftTabControl.SelectedIndex == 1 && AssetsFolderName.SelectedItem is TreeItem { Parent: TreeItem parent })
            {
                AssetsFolderName.Focus();
                parent.IsSelected = true;
            }
        }));

        DataContext = _applicationView;
        InitializeComponent();

        AssetsExplorer.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        AssetsListName.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        AssetsExplorer.SelectionChanged += (_, e) => SyncSelection(AssetsListName, e);
        AssetsListName.SelectionChanged += (_, e) => SyncSelection(AssetsExplorer, e);

        FLogger.Logger = LogRtbName;
        YesWeCats = this;
    }

    // Hack to sync selection between packages tab and explorer
    private void SyncSelection(ListBox target, SelectionChangedEventArgs e)
    {
        foreach (var added in e.AddedItems.OfType<GameFileViewModel>())
        {
            if (!target.SelectedItems.Contains(added))
                target.SelectedItems.Add(added);
        }

        foreach (var removed in e.RemovedItems.OfType<GameFileViewModel>())
        {
            if (target.SelectedItems.Contains(removed))
                target.SelectedItems.Remove(removed);
        }
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

        await Task.WhenAll(
            ApplicationViewModel.InitOodle(),
            ApplicationViewModel.InitZlib()
        );

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
        else if (e.Key == Key.F3)
            OnOpenAvalonFinder();
        else if (e.Key == Key.Left && !_applicationView.IsAssetsExplorerVisible && _applicationView.CUE4Parse.TabControl.SelectedTab is { HasImage: true })
            _applicationView.CUE4Parse.TabControl.SelectedTab.GoPreviousImage();
        else if (e.Key == Key.Right && !_applicationView.IsAssetsExplorerVisible && _applicationView.CUE4Parse.TabControl.SelectedTab is { HasImage: true })
            _applicationView.CUE4Parse.TabControl.SelectedTab.GoNextImage();
        else if (_applicationView.Status.IsReady && _applicationView.IsAssetsExplorerVisible && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            CategoriesSelector.SelectedIndex = e.SystemKey switch
            {
                Key.D0 or Key.NumPad0 => 0,
                Key.D1 or Key.NumPad1 => 1,
                Key.D2 or Key.NumPad2 => 2,
                Key.D3 or Key.NumPad3 => 3,
                Key.D4 or Key.NumPad4 => 4,
                Key.D5 or Key.NumPad5 => 5,
                Key.D6 or Key.NumPad6 => 6,
                Key.D7 or Key.NumPad7 => 7,
                Key.D8 or Key.NumPad8 => 8,
                Key.D9 or Key.NumPad9 => 9,
                _ => CategoriesSelector.SelectedIndex
            };
        }
        else if (_applicationView.Status.IsReady && UserSettings.Default.FeaturePreviewNewAssetExplorer && UserSettings.Default.SwitchAssetExplorer.IsTriggered(e.Key))
            _applicationView.IsAssetsExplorerVisible = !_applicationView.IsAssetsExplorerVisible;
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
        var searchView = Helper.GetWindow<SearchView>("Search For Packages", () => new SearchView().Show());
        searchView.FocusTab(ESearchViewTab.SearchView);
    }

    private void OnRefViewClick(object sender, RoutedEventArgs e)
    {
        var searchView = Helper.GetWindow<SearchView>("Search For Packages", () => new SearchView().Show());
        searchView.FocusTab(ESearchViewTab.RefView);
    }

    private void OnTabItemChange(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource is not TabControl tabControl)
            return;

        switch (tabControl.SelectedIndex)
        {
            case 0:
                DirectoryFilesListBox.Focus();
                break;
            case 1:
                AssetsFolderName.Focus();
                break;
            case 2:
                AssetsListName.Focus();
                break;
        }
    }

    private async void OnMappingsReload(object sender, ExecutedRoutedEventArgs e)
    {
        await _applicationView.CUE4Parse.InitMappings(true);
    }

    private void OnOpenAvalonFinder()
    {
        if (_applicationView.IsAssetsExplorerVisible)
        {
            AssetsExplorerSearch.TextBox.Focus();
            AssetsExplorerSearch.TextBox.SelectAll();
        }
        else if (_applicationView.CUE4Parse.TabControl.SelectedTab is { } tab)
        {
            tab.HasSearchOpen = true;
            AvalonEditor.YesWeSearch.Focus();
            AvalonEditor.YesWeSearch.SelectAll();
        }
    }

    private void OnAssetsTreeMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TreeView { SelectedItem: TreeItem treeItem } || treeItem.Folders.Count > 0) return;

        _applicationView.SelectedLeftTabIndex++;
    }

    private void OnPreviewTexturesToggled(object sender, RoutedEventArgs e) => ItemContainerGenerator_StatusChanged(AssetsExplorer.ItemContainerGenerator, EventArgs.Empty);
    private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
    {
        if (sender is not ItemContainerGenerator { Status: GeneratorStatus.ContainersGenerated } generator)
            return;

        var foundVisibleItem = false;
        var itemCount = generator.Items.Count;

        for (var i = 0; i < itemCount; i++)
        {
            var container = generator.ContainerFromIndex(i);
            if (container == null)
            {
                if (foundVisibleItem) break; // we're past the visible range already
                continue; // keep scrolling to find visible items
            }

            if (container is FrameworkElement { IsVisible: true } && generator.Items[i] is GameFileViewModel file)
            {
                foundVisibleItem = true;
                file.OnIsVisible();
            }
        }
    }

    private void OnAssetsTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (sender is not TreeView { SelectedItem: TreeItem }) return;

        _applicationView.IsAssetsExplorerVisible = true;
        _applicationView.SelectedLeftTabIndex = 1;
    }

    private async void OnAssetsListMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox) return;

        var selectedItems = listBox.SelectedItems.OfType<GameFileViewModel>().Select(gvm => gvm.Asset).ToArray();
        if (selectedItems.Length == 0) return;

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
        if (!_applicationView.Status.IsReady || sender is not ListBox listBox)
            return;
        if (e.Key != Key.Enter)
            return;
        if (listBox.SelectedItem == null)
            return;

        switch (listBox.SelectedItem)
        {
            case GameFileViewModel file:
                _applicationView.IsAssetsExplorerVisible = false;
                ApplicationService.ApplicationView.SelectedLeftTabIndex = 2;
                await _threadWorkerView.Begin(cancellationToken => _applicationView.CUE4Parse.ExtractSelected(cancellationToken, [file.Asset]));
                break;
            case TreeItem folder:
                ApplicationService.ApplicationView.SelectedLeftTabIndex = 1;

                var parent = folder.Parent;
                while (parent != null)
                {
                    parent.IsExpanded = true;
                    parent = parent.Parent;
                }

                var childFolder = folder;
                while (childFolder.Folders.Count == 1 && childFolder.AssetsList.Assets.Count == 0)
                {
                    childFolder.IsExpanded = true;
                    childFolder = childFolder.Folders[0];
                }

                childFolder.IsExpanded = true;
                childFolder.IsSelected = true;
                break;
        }
    }

    private void FeaturePreviewOnUnchecked(object sender, RoutedEventArgs e)
    {
        _applicationView.IsAssetsExplorerVisible = false;
    }

    private async void OnFoldersPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TreeView treeView || treeView.SelectedItem is not TreeItem folder)
            return;

        if ((folder.IsExpanded || folder.Folders.Count == 0) && folder.AssetsList.Assets.Count > 0)
        {
            _applicationView.SelectedLeftTabIndex++;
            return;
        }

        var childFolder = folder;
        while (childFolder.Folders.Count == 1 && childFolder.AssetsList.Assets.Count == 0)
        {
            childFolder.IsExpanded = true;
            childFolder = childFolder.Folders[0];
        }

        childFolder.IsExpanded = true;
        childFolder.IsSelected = true;
    }
}
