using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
using FModel.Views;
using FModel.Views.Resources.Controls;
using AvaloniaEdit.Editing;

namespace FModel;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static MainWindow YesWeCats;
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
    private DiscordHandler _discordHandler => DiscordService.DiscordHandler;

    public MainWindow()
    {
        DataContext = _applicationView;
        InitializeComponent();

        AssetsExplorer.SelectionChanged += (_, e) => SyncSelection(AssetsListName, e);
        AssetsListName.SelectionChanged += (_, e) => SyncSelection(AssetsExplorer, e);

        FLogger.Logger = LogRtbName;
        YesWeCats = this;

        // Wire status-bar colour updates; replaces WPF DataTrigger-based style
        _applicationView.Status.PropertyChanged += OnStatusKindChanged;
        _threadWorkerView.PropertyChanged += OnThreadWorkerPropertyChanged;

        // Wire window title updates; replaces WPF DataTrigger on AdonisWindow style
        _applicationView.PropertyChanged += OnApplicationViewPropertyChanged;
        UpdateWindowTitle();

        // Windows-only: set up TaskbarItemInfo progress
        if (OperatingSystem.IsWindows())
            InitTaskbarInfo();
    }

    [SupportedOSPlatform("windows")]
    private void InitTaskbarInfo()
    {
        // TODO(P2-015): set up Windows taskbar progress indicator once
        // StatusToTaskbarStateConverter is migrated.
    }

    private void UpdateWindowTitle()
    {
        Title = string.IsNullOrEmpty(_applicationView.TitleExtra)
            ? _applicationView.InitialWindowTitle
            : $"{_applicationView.InitialWindowTitle} - {_applicationView.GameDisplayName} {_applicationView.TitleExtra}";
    }

    private void OnApplicationViewPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ApplicationViewModel.TitleExtra)
                           or nameof(ApplicationViewModel.GameDisplayName)
                           or nameof(ApplicationViewModel.InitialWindowTitle))
            Dispatcher.UIThread.InvokeAsync(UpdateWindowTitle);
    }

    // --- Status bar colour (replaces WPF DataTrigger style) ---

    private void OnStatusKindChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FStatus.Kind))
            Dispatcher.UIThread.InvokeAsync(UpdateStatusBarColor);
        else if (e.PropertyName == nameof(FStatus.Label))
            Dispatcher.UIThread.InvokeAsync(UpdateStatusLabel);
    }

    private void UpdateStatusBarColor()
    {
        if (StatusBarBorder is null)
            return;
        var brushKey = _applicationView.Status.Kind switch
        {
            EStatusKind.Ready or EStatusKind.Completed => "AccentColorBrush",
            EStatusKind.Loading or EStatusKind.Stopping => "AlertColorBrush",
            EStatusKind.Stopped or EStatusKind.Failed => "ErrorColorBrush",
            _ => (string?) null
        };
        if (brushKey is null)
            return;
        if (Application.Current!.TryGetResource(brushKey, ActualThemeVariant, out var resource)
            && resource is IBrush bg)
        {
            StatusBarBorder.Background = bg;
            StatusBarBorder.Foreground = Brushes.White;
        }
    }

    private void OnThreadWorkerPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // Flash animations for StatusChangeAttempted / OperationCancelled
        // are tracked as TODO: implement via DispatcherTimer in a later pass.
        if (e.PropertyName == nameof(ThreadWorkerViewModel.CanBeCanceled))
            Dispatcher.UIThread.InvokeAsync(UpdateStatusLabel);
    }

    private void UpdateStatusLabel()
    {
        if (StatusLabel is null)
            return;
        var kind = _applicationView.Status.Kind;
        var label = _applicationView.Status.Label;
        StatusLabel.Text = kind == EStatusKind.Loading && _threadWorkerView.CanBeCanceled
            ? $"{label} \u2026    ESC to Cancel"
            : kind == EStatusKind.Loading
                ? $"{label} \u2026"
                : label;
    }

    // --- Lifecycle ---

    private void OnClosing(object sender, CancelEventArgs e)
    {
        _discordHandler.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set initial window size to ~90%×95% of primary screen working area
        // (replaces WPF SystemParameters binding)
        var screen = Screens.Primary;
        if (screen != null)
        {
            Width = screen.WorkingArea.Width * 0.90 / screen.PixelDensity;
            Height = screen.WorkingArea.Height * 0.95 / screen.PixelDensity;
        }

        UpdateStatusBarColor();

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
    }

    private void OnGridSplitterDoubleClick(object sender, TappedEventArgs e)
    {
        RootGrid.ColumnDefinitions[0].Width = GridLength.Auto;
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Source is TextBox || e.Source is TextArea)
        {
            // Let Ctrl+key through to the text editing controls
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                return;
        }

        if (_threadWorkerView.CanBeCanceled && e.Key == Key.Escape)
        {
            _applicationView.Status.SetStatus(EStatusKind.Stopping);
            _threadWorkerView.Cancel();
        }
        else if (_applicationView.Status.IsReady && e.Key == Key.F
                 && e.KeyModifiers.HasFlag(KeyModifiers.Control)
                 && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            OnSearchViewClick(null, null);
        else if (_applicationView.Status.IsReady && e.Key == Key.R
                 && e.KeyModifiers.HasFlag(KeyModifiers.Control)
                 && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            OnRefViewClick(null, null);
        else if (e.Key == Key.F3)
            OnOpenAvalonFinder();
        else if (e.Key == Key.Left && !_applicationView.IsAssetsExplorerVisible
                 && _applicationView.CUE4Parse.TabControl.SelectedTab is { HasImage: true })
            _applicationView.CUE4Parse.TabControl.SelectedTab.GoPreviousImage();
        else if (e.Key == Key.Right && !_applicationView.IsAssetsExplorerVisible
                 && _applicationView.CUE4Parse.TabControl.SelectedTab is { HasImage: true })
            _applicationView.CUE4Parse.TabControl.SelectedTab.GoNextImage();
        else if (_applicationView.Status.IsReady && _applicationView.IsAssetsExplorerVisible
                 && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            CategoriesSelector.SelectedIndex = e.Key switch
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
        else if (_applicationView.Status.IsReady
                 && UserSettings.Default.FeaturePreviewNewAssetExplorer
                 && UserSettings.Default.SwitchAssetExplorer.IsTriggered(e.Key))
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

    private void OnMappingsReload(object sender, RoutedEventArgs e)
    {
        _ = _applicationView.CUE4Parse.InitMappings(true);
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
        if (e.Source is not TabControl tabControl)
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

    private void OnAssetsTreeMouseDoubleClick(object sender, TappedEventArgs e)
    {
        if (sender is not TreeView { SelectedItem: TreeItem treeItem } || treeItem.Folders.Count > 0)
            return;
        _applicationView.SelectedLeftTabIndex++;
    }

    private void OnPreviewTexturesToggled(object sender, RoutedEventArgs e) =>
        ItemContainerGenerator_StatusChanged(AssetsExplorer);

    private void ItemContainerGenerator_StatusChanged(ListBox listBox)
    {
        // Avalonia doesn't have ItemContainerGenerator; containers are always
        // available once rendered. Walk visible items instead.
        var foundVisibleItem = false;
        for (var i = 0; i < listBox.Items.Count; i++)
        {
            var container = listBox.ContainerFromIndex(i) as Control;
            if (container == null)
            {
                if (foundVisibleItem)
                    break;
                continue;
            }

            if (container.IsVisible && listBox.Items[i] is GameFileViewModel file)
            {
                foundVisibleItem = true;
                file.OnIsVisible();
            }
        }
    }

    private void OnAssetsTreeSelectedItemChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not TreeView { SelectedItem: TreeItem })
            return;

        _applicationView.IsAssetsExplorerVisible = true;
        _applicationView.SelectedLeftTabIndex = 1;
    }

    private async void OnAssetsListMouseDoubleClick(object sender, TappedEventArgs e)
    {
        if (sender is not ListBox listBox)
            return;

        var selectedItems = listBox.SelectedItems?.OfType<GameFileViewModel>().Select(gvm => gvm.Asset).ToArray();
        if (selectedItems == null || selectedItems.Length == 0)
            return;

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

    private void OnMouseDoubleClick(object sender, TappedEventArgs e)
    {
        if (!_applicationView.Status.IsReady || sender is not ListBox listBox)
            return;
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

    // Hack to sync selection between packages tab and explorer
    private void SyncSelection(ListBox target, SelectionChangedEventArgs e)
    {
        foreach (var added in e.AddedItems.OfType<GameFileViewModel>())
        {
            if (target.SelectedItems != null && !target.SelectedItems.Contains(added))
                target.SelectedItems.Add(added);
        }

        foreach (var removed in e.RemovedItems.OfType<GameFileViewModel>())
        {
            target.SelectedItems?.Remove(removed);
        }
    }
}
