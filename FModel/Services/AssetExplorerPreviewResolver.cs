using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FModel.Extensions;
using FModel.Settings;
using FModel.ViewModels;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Serilog;
using Snooper.Rendering;

namespace FModel.Services;

public sealed class AssetExplorerPreviewResolver : IDisposable
{
    private readonly ApplicationViewModel _applicationView;
    private readonly (ListBox Grid, ListBox List, ListBox Packages) _views;
    private readonly UserSettings _settings = UserSettings.Default;

    private ListBox Explorer => _views.Grid.IsVisible ? _views.Grid : _views.List;
    private readonly DispatcherTimer _resolutionTimer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(100) };
    private int _assetResolutionVersion;
    private int _startedAssetResolutionVersion;
    private bool _resolvingAssets;
    private bool _closing;
    private GameFileViewModel _hoveredMeshPreview;
    private CancellationTokenSource _meshPreviewRequest;
    private bool _meshPreviewsPending;
    private bool _meshPreviewsUnavailable;

    public AssetExplorerPreviewResolver(ApplicationViewModel applicationView, ListBox grid, ListBox list, ListBox packages)
    {
        _applicationView = applicationView;
        _views = (grid, list, packages);

        _resolutionTimer.Tick += OnResolutionTick;
        _settings.PropertyChanged += OnSettingsChanged;
        foreach (var view in new[] { grid, list, packages })
        {
            view.ItemContainerGenerator.StatusChanged += OnContainersGenerated;
            view.IsVisibleChanged += OnVisibilityChanged;
            view.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));

            if (view == packages)
                continue;

            view.MouseMove += OnHoverChanged;
            view.MouseLeave += OnHoverChanged;
        }

        ScheduleAssetResolution();
        ScheduleMeshPreviews();
    }

    public void Dispose()
    {
        if (_closing)
            return;

        _closing = true;
        Interlocked.Increment(ref _assetResolutionVersion);
        _resolutionTimer.Stop();
        _resolutionTimer.Tick -= OnResolutionTick;
        _settings.PropertyChanged -= OnSettingsChanged;
        foreach (var view in new[] { _views.Grid, _views.List, _views.Packages })
        {
            view.ItemContainerGenerator.StatusChanged -= OnContainersGenerated;
            view.IsVisibleChanged -= OnVisibilityChanged;
            view.RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));
            if (view == _views.Packages)
                continue;

            view.MouseMove -= OnHoverChanged;
            view.MouseLeave -= OnHoverChanged;
        }

        _meshPreviewsUnavailable = true;
        _meshPreviewRequest?.Cancel();
        SnooperViewModel.Instance.CloseMeshPreviews();
    }

    private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ScheduleAssetResolution();
        if (!ReferenceEquals(sender, _views.Packages))
            ScheduleMeshPreviews();
    }

    private void OnContainersGenerated(object sender, EventArgs e)
    {
        if (sender is not ItemContainerGenerator { Status: GeneratorStatus.ContainersGenerated } generator)
            return;

        var (grid, list, packages) = _views;
        var view = packages;
        if (ReferenceEquals(generator, grid.ItemContainerGenerator))
        {
            view = grid;
        }
        else if (ReferenceEquals(generator, list.ItemContainerGenerator))
        {
            view = list;
        }

        if (!view.IsVisible)
            return;

        ScheduleAssetResolution();
        if (view != packages)
            ScheduleMeshPreviews();
    }

    private void ScheduleAssetResolution()
    {
        if (_closing)
            return;

        Interlocked.Increment(ref _assetResolutionVersion);
        RestartResolutionTimer();
    }

    private void RestartResolutionTimer()
    {
        _resolutionTimer.Stop();
        _resolutionTimer.Start();
    }

    private void OnResolutionTick(object sender, EventArgs e)
    {
        _resolutionTimer.Stop();
        if (_closing)
            return;

        if (!_resolvingAssets && _startedAssetResolutionVersion != _assetResolutionVersion)
            ResolveVisibleAssets();
        if (_meshPreviewsPending && _meshPreviewRequest == null)
            _ = ResolveMeshPreviewsAsync();
    }

    private void ResolveVisibleAssets()
    {
        var version = _startedAssetResolutionVersion = _assetResolutionVersion;
        var explorer = Explorer;
        var previewMode = _settings.MeshPreviewMode;
        var hovered = previewMode is EMeshPreviewMode.OnHover ? GetHoveredAsset(explorer) : null;
        var resolve = explorer.IsVisible && _settings.PreviewTexturesAssetExplorer ? EResolveCompute.All : EResolveCompute.Category;

        var requests = GetVisibleAssets(explorer, file => file.NeedsResolution(resolve))
            .Select(file => (File: file, Resolve: resolve))
            .Concat(GetVisibleAssets(_views.Packages, file => file.NeedsResolution(EResolveCompute.Category))
                .Select(file => (File: file, Resolve: EResolveCompute.Category)))
            .DistinctBy(request => request.File);

        var visible = (hovered == null ? requests : requests.OrderByDescending(request => ReferenceEquals(request.File, hovered))).ToArray();
        if (visible.Length == 0)
            return;

        var provider = _applicationView.CUE4Parse?.Provider;
        var dispatcher = explorer.Dispatcher;
        _resolvingAssets = true;
        _ = Task.Run(() =>
        {
            var foundMesh = false;
            var previewScheduled = false;
            try
            {
                foreach (var (file, flags) in visible)
                {
                    if (Volatile.Read(ref _assetResolutionVersion) != version)
                        break;

                    var categoryPending = file.NeedsResolution(EResolveCompute.Category);
                    file.Resolve(flags, provider);
                    if (!categoryPending || !file.CanResolveMeshPreview)
                        continue;

                    foundMesh = true;
                    if (previewScheduled || (previewMode != EMeshPreviewMode.Automatic && !ReferenceEquals(file, hovered)))
                        continue;

                    previewScheduled = true;
                    dispatcher.InvokeAsync(() =>
                    {
                        if (!_closing && version == _assetResolutionVersion)
                            ScheduleMeshPreviews(cancelRunning: false, debounce: false);
                    }, DispatcherPriority.Background);
                }
            }
            finally
            {
                dispatcher.InvokeAsync(() =>
                {
                    _resolvingAssets = false;
                    if (_closing)
                        return;

                    if (foundMesh)
                        ScheduleMeshPreviews(cancelRunning: false, debounce: version != _assetResolutionVersion);
                    if (version != _assetResolutionVersion && !_resolutionTimer.IsEnabled)
                        _resolutionTimer.Start();
                }, DispatcherPriority.Background);
            }
        });
    }

    private static IEnumerable<GameFileViewModel> GetVisibleAssets(ListBox list, Predicate<GameFileViewModel> include)
    {
        if (!list.IsVisible || list.FindVisualChild<ScrollContentPresenter>() is not { } viewport || viewport.FindVisualChild<VirtualizingPanel>() is not { } panel)
            yield break;

        var viewportBounds = new Rect(viewport.RenderSize);
        foreach (var child in panel.Children)
        {
            if (child is not ListBoxItem { IsVisible: true, DataContext: GameFileViewModel file } container || !include(file))
                continue;

            var bounds = container.TransformToAncestor(viewport).TransformBounds(new Rect(container.RenderSize));
            bounds.Intersect(viewportBounds);
            if (bounds.Width > 0 && bounds.Height > 0)
                yield return file;
        }
    }

    private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UserSettings.MeshPreviewMode) or nameof(UserSettings.PreviewMaterialThumbnails))
        {
            ScheduleMeshPreviews();
        }
        else if (e.PropertyName == nameof(UserSettings.PreviewTexturesAssetExplorer))
        {
            ScheduleAssetResolution();
        }
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is ListBox { IsVisible: true } list && (e.VerticalChange != 0 || e.HorizontalChange != 0 || e.ViewportHeightChange != 0 || e.ViewportWidthChange != 0))
        {
            ScheduleAssetResolution();
            if (!ReferenceEquals(list, _views.Packages))
                ScheduleMeshPreviews();
        }
    }

    private void OnHoverChanged(object sender, MouseEventArgs e)
    {
        if (_settings.MeshPreviewMode is not EMeshPreviewMode.OnHover || sender is not ListBox list)
            return;

        var file = e.RoutedEvent == Mouse.MouseLeaveEvent ? null : GetHoveredMeshPreview(list);
        if (ReferenceEquals(_hoveredMeshPreview, file))
            return;

        _hoveredMeshPreview = file;
        ScheduleMeshPreviews();
    }

    private static GameFileViewModel GetHoveredAsset(ListBox list) => list.IsMouseOver && Mouse.DirectlyOver is DependencyObject element &&
        ItemsControl.ContainerFromElement(list, element) is ListBoxItem { DataContext: GameFileViewModel file }
            ? file
            : null;

    private static GameFileViewModel GetHoveredMeshPreview(ListBox list) => GetHoveredAsset(list) is { CanResolveMeshPreview: true } file ? file : null;

    private void ScheduleMeshPreviews(bool cancelRunning = true, bool debounce = true)
    {
        if (cancelRunning)
            _meshPreviewRequest?.Cancel();

        _meshPreviewsPending = !_closing && !_meshPreviewsUnavailable && _applicationView.IsAssetsExplorerVisible && _settings.MeshPreviewMode is not EMeshPreviewMode.None;
        if (_meshPreviewsPending && _meshPreviewRequest == null)
        {
            if (debounce)
            {
                RestartResolutionTimer();
            }
            else
            {
                _ = ResolveMeshPreviewsAsync();
            }
        }
    }

    private async Task ResolveMeshPreviewsAsync()
    {
        _meshPreviewsPending = false;

        var list = Explorer;
        if (_settings.MeshPreviewMode is EMeshPreviewMode.None || !list.IsVisible || !_applicationView.IsAssetsExplorerVisible || _applicationView.CUE4Parse?.Provider is not { } provider)
            return;

        GameFileViewModel[] assets;
        if (_settings.MeshPreviewMode is EMeshPreviewMode.OnHover)
        {
            _hoveredMeshPreview = GetHoveredMeshPreview(list);
            assets = _hoveredMeshPreview is { } hovered ? [hovered] : [];
        }
        else
        {
            assets = [.. GetVisibleAssets(list, file => file.CanResolveMeshPreview)];
        }

        if (assets.Length == 0)
            return;

        using var request = new CancellationTokenSource();
        _meshPreviewRequest = request;
        try
        {
            var platform = _settings.CurrentDir.TexturePlatform;
            foreach (var file in assets)
            {
                if (request.IsCancellationRequested)
                    break;
                if (!file.TryBeginMeshPreview())
                    continue;

                try
                {
                    var pixels = await SnooperViewModel.Instance.RenderMeshPreviewAsync(provider, file.Asset, platform);
                    if (pixels == null || _closing)
                        continue;

                    const int size = MeshPreviewRenderer.MaxPreviewSize;
                    var image = BitmapSource.Create(size, size, 96, 96, PixelFormats.Pbgra32, null, pixels, size * 4);
                    image.Freeze();
                    file.MeshPreviewImage = image;
                }
                catch (GLFWException exception)
                {
                    _meshPreviewsUnavailable = true;
                    _meshPreviewsPending = false;
                    Log.Warning(exception, "Could not start the Snooper mesh preview context");
                    break;
                }
                catch (Exception exception)
                {
                    Log.Debug(exception, "Could not render the mesh preview for {Asset}", file.Asset.Path);
                }
            }
        }
        finally
        {
            _meshPreviewRequest = null;
            if (_meshPreviewsPending && !_closing && !_resolutionTimer.IsEnabled)
                _resolutionTimer.Start();
        }
    }
}
