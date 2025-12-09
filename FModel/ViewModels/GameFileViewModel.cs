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
using CUE4Parse.Utils;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using Serilog;
using SkiaSharp;
using Svg.Skia;

namespace FModel.ViewModels;

public class GameFileViewModel(GameFile asset) : ViewModel
{
    private const int MaxPreviewSize = 128; // TODO: get partial payload in C4P

    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
    private EResolveCompute _resolved = EResolveCompute.None;

    public GameFile Asset { get; } = asset;

    private IPackage? _package;
    public IPackage? Package
    {
        get => _package;
        private set => SetProperty(ref _package, value);
    }

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
        private set
        {
            if (SetProperty(ref _assetCategory, value))
            {
                _resolved |= EResolveCompute.Category;
            }
        }
    }

    private ImageSource _previewImage;
    public ImageSource PreviewImage
    {
        get => _previewImage;
        private set
        {
            if (SetProperty(ref _previewImage, value))
            {
                _resolved |= EResolveCompute.Preview;
            }
        }
    }

    public Task ExtractAsync()
        => ApplicationService.ThreadWorkerView.Begin(cancellationToken =>
            _applicationView.CUE4Parse.ExtractSelected(cancellationToken, [Asset]));

    public Task ResolveAsync(EResolveCompute resolve)
    {
        try
        {
            return ResolveInternalAsync(resolve);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Failed to resolve asset {AssetName} ({Resolver})", Asset.Path, resolve.ToStringBitfield());

            _resolved = EResolveCompute.All;
            return Task.CompletedTask;
        }
    }

    private Task ResolveInternalAsync(EResolveCompute resolve)
    {
        if (!_applicationView.IsAssetsExplorerVisible || !UserSettings.Default.PreviewTexturesAssetExplorer)
        {
            resolve &= ~EResolveCompute.Preview;
        }

        resolve &= ~_resolved;
        if (resolve == EResolveCompute.None)
            return Task.CompletedTask;

        if (!Asset.IsUePackage || _applicationView.CUE4Parse is null)
            return ResolveByExtensionAsync(resolve);

        return ResolveByPackageAsync(resolve);
    }

    private Task ResolveByPackageAsync(EResolveCompute resolve)
    {
        return Task.Run(() =>
        {
            Package ??= _applicationView.CUE4Parse?.Provider.LoadPackage(Asset);
            if (Package is null)
                throw new InvalidOperationException("Failed to load package.");

            var mainIndex = Package.GetExportIndex(Asset.NameWithoutExtension);
            if (mainIndex < 0) mainIndex = Package.GetExportIndex($"{Asset.NameWithoutExtension}_C");
            if (mainIndex < 0) mainIndex = 0;

            var pointer = new FPackageIndex(Package, mainIndex + 1).ResolvedObject;
            if (pointer?.Object is null)
                return;

            var dummy = ((AbstractUePackage) Package).ConstructObject(pointer.Class?.Object?.Value as UStruct, Package);
            ResolvedAssetType = dummy.ExportType;

            switch (dummy)
            {
                case UTexture when pointer.Object.Value is UTexture texture:
                {
                    AssetCategory = EAssetCategory.Texture;
                    if (!resolve.HasFlag(EResolveCompute.Preview))
                        break;

                    var mip = texture.GetMipByMaxSize(MaxPreviewSize);
                    var img = texture.Decode(mip, UserSettings.Default.CurrentDir.TexturePlatform);
                    if (img != null)
                    {
                        using var bitmap = img.ToSkBitmap();
                        using var image = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                        SetPreviewImage(image);
                    }
                    break;
                }
                case UDataAsset:
                case UDataTable:
                    AssetCategory = EAssetCategory.Data;
                    break;
                case USoundBase:
                case UAkMediaAssetData:
                case UAtomWaveBank:
                case USoundAtomCue:
                case UAtomCueSheet:
                case USoundAtomCueSheet:
                case UFMODBank:
                case UFMODEvent:
                case UAkAudioType:
                    AssetCategory = EAssetCategory.Audio;
                    break;
                case USkeleton:
                    AssetCategory = EAssetCategory.Skeleton;
                    break;
                case UStaticMesh:
                    AssetCategory = EAssetCategory.StaticMesh;
                    break;
                case USkeletalMesh:
                    AssetCategory = EAssetCategory.SkeletalMesh;
                    break;
                case UBlueprintCore:
                case UBlueprintGeneratedClass:
                    AssetCategory = EAssetCategory.Blueprint;
                    break;
                case UMaterialInterface:
                    AssetCategory = EAssetCategory.Material;
                    break;
                case UPhysicsAsset:
                    AssetCategory = EAssetCategory.PhysicsAsset;
                    break;
                case UAnimationAsset:
                    AssetCategory = EAssetCategory.Animation;
                    break;
                case UFont:
                case UFontFace:
                    AssetCategory = EAssetCategory.Font;
                    break;
                case UFileMediaSource:
                    AssetCategory = EAssetCategory.Video;
                    break;
                case UWorld:
                    AssetCategory = EAssetCategory.Map;
                    break;
            }
        });
    }

    private Task ResolveByExtensionAsync(EResolveCompute resolve)
    {
        switch (Asset.Extension)
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
            case "svg":
            {
                AssetCategory = EAssetCategory.Texture;
                if (!resolve.HasFlag(EResolveCompute.Preview))
                    break;

                return Task.Run(() =>
                {
                    var data = _applicationView.CUE4Parse.Provider.SaveAsset(Asset);
                    using var stream = new MemoryStream(data);
                    stream.Position = 0;

                    SKBitmap bitmap;
                    if (Asset.Extension == "svg")
                    {
                        var svg = new SKSvg();
                        svg.Load(stream);
                        if (svg.Picture == null)
                            return;

                        bitmap = new SKBitmap(MaxPreviewSize, MaxPreviewSize);
                        using var canvas = new SKCanvas(bitmap);
                        canvas.Clear(SKColors.Transparent);

                        var bounds = svg.Picture.CullRect;
                        float scale = Math.Min(MaxPreviewSize / bounds.Width, MaxPreviewSize / bounds.Height);
                        canvas.Scale(scale);
                        canvas.Translate(-bounds.Left, -bounds.Top);
                        canvas.DrawPicture(svg.Picture);
                    }
                    else
                    {
                        bitmap = SKBitmap.Decode(stream);
                    }

                    using var image = bitmap.Encode(Asset.Extension == "jpg" ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 100);
                    SetPreviewImage(image);

                    bitmap.Dispose();
                });
            }
        }

        return Task.CompletedTask;
    }

    private void SetPreviewImage(SKData data)
    {
        using var ms = new MemoryStream(data.ToArray());
        ms.Position = 0;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = ms;
        bitmap.EndInit();
        bitmap.Freeze();

        Application.Current.Dispatcher.InvokeAsync(() => PreviewImage = bitmap);
    }

    private CancellationTokenSource _previewCts;
    public void OnVisibleChanged(bool isVisible)
    {
        if (!isVisible || _resolved == EResolveCompute.All)
            return;

        _previewCts?.Cancel();
        _previewCts = new CancellationTokenSource();
        var token = _previewCts.Token;

        Task.Delay(100, token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            ResolveAsync(EResolveCompute.All);
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }
}

[Flags]
public enum EResolveCompute
{
    None = 0,
    Category = 1 << 0,
    Preview = 1 << 1,

    All = Category | Preview
}
