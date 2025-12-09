using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.CriWare;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.Fmod;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.MediaAssets;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Textures;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using Serilog;
using SkiaSharp;
using Svg.Skia;

namespace FModel.ViewModels;

public class GameFileViewModel(GameFile asset) : ViewModel
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    private Task _resolveTask;
    private bool _resolved;

    public GameFile Asset { get; } = asset;

    private string _resolvedAssetType = asset.Extension;
    public string ResolvedAssetType
    {
        get => _resolvedAssetType;
        private set => SetProperty(ref _resolvedAssetType, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private EAssetCategory _assetCategory = EAssetCategory.All;
    public EAssetCategory AssetCategory
    {
        get => _assetCategory;
        private set => SetProperty(ref _assetCategory, value);
    }

    private ImageSource _previewImage;
    public ImageSource PreviewImage
    {
        get => _previewImage;
        set => SetProperty(ref _previewImage, value);
    }

    public Task ExtractAsync()
        => ApplicationService.ThreadWorkerView.Begin(cancellationToken =>
            _applicationView.CUE4Parse.ExtractSelected(cancellationToken, [Asset]));

    public Task ResolveAsset()
    {
        if (_resolved)
            return Task.CompletedTask;
        if (_resolveTask != null)
            return _resolveTask;

        return _resolveTask = ResolveCategoryAndLoadPreview();
    }

    private async Task ResolveCategoryAndLoadPreview()
    {
        try
        {
            if (!_applicationView.IsAssetsExplorerVisible) return;

            if (!Asset.IsUePackage || _applicationView.CUE4Parse is null)
            {
                ResolveCategoryAndLoadPreviewByExtension(Asset);
                return;
            }

            await Task.Run(() =>
            {
                if (!_applicationView.CUE4Parse.Provider.TryLoadPackage(Asset, out var package))
                    return;

                var mainIndex = package.GetExportIndex(Asset.NameWithoutExtension);
                if (mainIndex < 0) mainIndex = package.GetExportIndex($"{Asset.NameWithoutExtension}_C");
                if (mainIndex < 0) mainIndex = 0;

                var pointer = new FPackageIndex(package, mainIndex + 1).ResolvedObject;
                if (pointer?.Object is null)
                    return;

                var dummy = ((AbstractUePackage) package).ConstructObject(pointer.Class?.Object?.Value as UStruct, package);
                ResolvedAssetType = dummy.ExportType;

                switch (dummy)
                {
                    case UTexture when pointer.Object.Value is UTexture texture:
                    {
                        AssetCategory = EAssetCategory.Texture;
                        if (!UserSettings.Default.PreviewTexturesAssetExplorer)
                            break;

                        var img = new CTexture[1];
                        const int targetSize = 128;
                        var mip = texture.GetMipByMaxSize(targetSize);
                        img[0] = texture.Decode(mip, UserSettings.Default.CurrentDir.TexturePlatform);

                        using var ms = new MemoryStream();

                        if (img[0] == null)
                            break;

                        var bmp = img[0].ToSkBitmap();
                        byte[] imageData = bmp.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                        ms.Position = 0;

                        using var stream = new MemoryStream(imageData);
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();

                        Application.Current.Dispatcher.InvokeAsync(() => PreviewImage = image);
                        return; // Let's display first found texture
                    }
                    case UDataAsset:
                    case UDataTable:
                    {
                        AssetCategory = EAssetCategory.Data;
                        return;
                    }
                    case USoundCue:
                    case USoundWave:
                    case UAkMediaAssetData:
                    case UAtomWaveBank:
                    case USoundAtomCue:
                    case UAtomCueSheet:
                    case USoundAtomCueSheet:
                    case UFMODBank:
                    case UFMODEvent:
                    case UAkAudioEvent:
                    {
                        AssetCategory = EAssetCategory.Audio;
                        return;
                    }
                    case USkeleton:
                    {
                        AssetCategory = EAssetCategory.Skeleton;
                        return;
                    }
                    case UStaticMesh:
                    {
                        AssetCategory = EAssetCategory.StaticMesh;
                        return;
                    }
                    case USkeletalMesh:
                    {
                        AssetCategory = EAssetCategory.SkeletalMesh;
                        return;
                    }
                    case UBlueprint:
                    case UBlueprintGeneratedClass:
                    {
                        AssetCategory = EAssetCategory.Blueprint;
                        return;
                    }
                    case UMaterial:
                    case UMaterialInstance:
                    {
                        AssetCategory = EAssetCategory.Material;
                        return;
                    }
                    case UPhysicsAsset:
                    {
                        AssetCategory = EAssetCategory.PhysicsAsset;
                        return;
                    }
                    case UAnimSequence:
                    case UAnimMontage:
                    case UAnimSequenceBase:
                    {
                        AssetCategory = EAssetCategory.Animation;
                        return;
                    }
                    case UFont:
                    case UFontFace:
                    {
                        AssetCategory = EAssetCategory.Font;
                        return;
                    }
                    case UFileMediaSource:
                    {
                        AssetCategory = EAssetCategory.Video;
                        return;
                    }
                    case UWorld:
                    {
                        AssetCategory = EAssetCategory.Map;
                        return;
                    }
                }
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to load preview for {Path}", Asset.Path);
        }
        finally
        {
            _resolved = true;
        }
    }

    private async void ResolveCategoryAndLoadPreviewByExtension(GameFile gameFile)
    {
        switch (gameFile.Extension)
        {
            case "uplugin":
            case "ini":
            case "locmeta":
            case "locres":
            case "lua":
            case "luac":
            case "json5":
            case "json":
            case "txt":
            case "log":
            case "pem":
                AssetCategory = EAssetCategory.Data;
                break;
            case "wav":
            case "bank":
            case "bnk":
            case "pck":
            case "awb":
            case "acb":
            case "xvag":
            case "flac":
            case "at9":
            case "wem":
            case "ogg":
                AssetCategory = EAssetCategory.Audio;
                break;
            case "ufont":
            case "otf":
            case "ttf":
                AssetCategory = EAssetCategory.Font;
                break;
            case "mp4":
                AssetCategory = EAssetCategory.Video;
                break;
            case "jpg":
            case "png":
            case "bmp":
                {
                    AssetCategory = EAssetCategory.Texture;
                    if (!UserSettings.Default.PreviewTexturesAssetExplorer)
                        break;

                    await Task.Run(() =>
                    {
                        var data = _applicationView.CUE4Parse.Provider.SaveAsset(gameFile);
                        using var stream = new MemoryStream(data) { Position = 0 };
                        var bitmap = SKBitmap.Decode(stream);
                        if (bitmap == null) return;

                        using var image = bitmap.Encode(gameFile.Extension == "jpg" ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 100);
                        using var ms = new MemoryStream(image.ToArray());

                        var bmpImage = new BitmapImage();
                        bmpImage.BeginInit();
                        bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                        bmpImage.StreamSource = ms;
                        bmpImage.EndInit();
                        bmpImage.Freeze();

                        Application.Current.Dispatcher.InvokeAsync(() => PreviewImage = bmpImage);
                    });

                    break;
                }
            case "svg":
                {
                    AssetCategory = EAssetCategory.Texture;
                    if (!UserSettings.Default.PreviewTexturesAssetExplorer)
                        break;

                    await Task.Run(() =>
                    {
                        var data = _applicationView.CUE4Parse.Provider.SaveAsset(gameFile);
                        using var stream = new MemoryStream(data) { Position = 0 };
                        var svg = new SKSvg();
                        svg.Load(stream);
                        if (svg.Picture == null) return;

                        const int size = 128;
                        var bitmap = new SKBitmap(size, size);
                        using var canvas = new SKCanvas(bitmap);
                        canvas.Clear(SKColors.Transparent);

                        var bounds = svg.Picture.CullRect;
                        float scale = Math.Min(size / bounds.Width, size / bounds.Height);
                        canvas.Scale(scale);
                        canvas.Translate(-bounds.Left, -bounds.Top);
                        canvas.DrawPicture(svg.Picture);

                        using var ms = new MemoryStream();
                        using (var img = bitmap.Encode(SKEncodedImageFormat.Png, 100)) img.SaveTo(ms);
                        ms.Position = 0;

                        var bmpImage = new BitmapImage();
                        bmpImage.BeginInit();
                        bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                        bmpImage.StreamSource = ms;
                        bmpImage.EndInit();
                        bmpImage.Freeze();

                        Application.Current.Dispatcher.InvokeAsync(() => PreviewImage = bmpImage);
                    });

                    break;
                }
        }
    }

    private CancellationTokenSource _previewCts;
    private bool _once = false;
    public void OnVisibleChanged(bool isVisible)
    {
        if (_once || !isVisible)
            return;

        _once = true;
        _previewCts?.Cancel();
        _previewCts = new CancellationTokenSource();
        var token = _previewCts.Token;

        Task.Delay(100, token) // Slight delay so it won't start loading when user scrolls quickly
            .ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    _ = ResolveCategoryAndLoadPreview();
                    if (!UserSettings.Default.PreviewTexturesAssetExplorer)
                    {
                        _once = false; // Allow retrying if previews are disabled
                    }
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
