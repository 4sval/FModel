using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.GameTypes.SMG.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.CriWare;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.Fmod;
using CUE4Parse.UE4.Assets.Exports.Foliage;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Exports.LevelSequence;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Material.Editor;
using CUE4Parse.UE4.Assets.Exports.Niagara;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.MediaAssets;
using CUE4Parse.UE4.Objects.Niagara;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.RigVM;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Objects.UObject.Editor;
using CUE4Parse.Utils;

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
    private const int MaxPreviewSize = 128;

    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public EResolveCompute Resolved { get; private set; } = EResolveCompute.None;
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
        private set
        {
            SetProperty(ref _assetCategory, value);
            Resolved |= EResolveCompute.Category; // blindly assume category is resolved when set, even if unchanged
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
                Resolved |= EResolveCompute.Preview;
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
            Log.Error(e, "Failed to resolve asset {AssetName} ({Resolver})", Asset.Path, resolve.ToStringBitfield());

            Resolved = EResolveCompute.All;
            return Task.CompletedTask;
        }
    }

    private Task ResolveInternalAsync(EResolveCompute resolve)
    {
        if (!_applicationView.IsAssetsExplorerVisible || !UserSettings.Default.PreviewTexturesAssetExplorer)
        {
            resolve &= ~EResolveCompute.Preview;
        }

        resolve &= ~Resolved;
        if (resolve == EResolveCompute.None)
            return Task.CompletedTask;

        if (!Asset.IsUePackage || _applicationView.CUE4Parse is null)
            return ResolveByExtensionAsync(resolve);

        return ResolveByPackageAsync(resolve);
    }

    private Task ResolveByPackageAsync(EResolveCompute resolve)
    {
        if (Asset.Extension is "umap")
        {
            AssetCategory = EAssetCategory.World;
            ResolvedAssetType = "World";
            Resolved |= EResolveCompute.Preview;
            return Task.CompletedTask;
        }
        if (Asset.NameWithoutExtension.EndsWith("_BuiltData"))
        {
            AssetCategory = EAssetCategory.BuildData;
            ResolvedAssetType = "MapBuildDataRegistry";
            Resolved |= EResolveCompute.Preview;
            return Task.CompletedTask;
        }

        return Task.Run(() =>
        {
            // TODO: cache and reuse packages
            var pkg = _applicationView.CUE4Parse?.Provider.LoadPackage(Asset);
            if (pkg is null)
                throw new InvalidOperationException($"Failed to load {Asset.Path} as UE package.");

            var mainIndex = pkg.GetExportIndex(Asset.NameWithoutExtension, StringComparison.OrdinalIgnoreCase);
            if (mainIndex < 0) mainIndex = pkg.GetExportIndex($"{Asset.NameWithoutExtension}_C", StringComparison.OrdinalIgnoreCase);
            if (mainIndex < 0) mainIndex = 0;

            var pointer = new FPackageIndex(pkg, mainIndex + 1).ResolvedObject;
            if (pointer?.Object is null)
                return;

            var dummy = ((AbstractUePackage) pkg).ConstructObject(pointer.Class?.Object?.Value as UStruct, pkg);
            ResolvedAssetType = dummy.ExportType;

            AssetCategory = dummy switch
            {
                URigVMBlueprintGeneratedClass => EAssetCategory.RigVMBlueprintGeneratedClass,
                UAnimBlueprintGeneratedClass => EAssetCategory.AnimBlueprintGeneratedClass,
                UWidgetBlueprintGeneratedClass => EAssetCategory.WidgetBlueprintGeneratedClass,
                UBlueprintGeneratedClass or UFunction => EAssetCategory.BlueprintGeneratedClass,
                UUserDefinedEnum => EAssetCategory.UserDefinedEnum,
                UUserDefinedStruct => EAssetCategory.UserDefinedStruct,
                UBlueprintCore => EAssetCategory.Blueprint,
                UClassCookedMetaData or UStructCookedMetaData or UEnumCookedMetaData => EAssetCategory.CookedMetaData,

                UStaticMesh => EAssetCategory.StaticMesh,
                USkeletalMesh => EAssetCategory.SkeletalMesh,
                UPhysicsAsset => EAssetCategory.PhysicsAsset,

                UTexture => EAssetCategory.Texture,

                UMaterialInterface => EAssetCategory.Material,
                UMaterialInterfaceEditorOnlyData => EAssetCategory.MaterialEditorData,
                UMaterialFunction => EAssetCategory.MaterialFunction,
                UMaterialParameterCollection => EAssetCategory.MaterialParameterCollection,

                UAnimationAsset => EAssetCategory.Animation,
                USkeleton => EAssetCategory.Skeleton,

                UWorld => EAssetCategory.World,
                UMapBuildDataRegistry => EAssetCategory.BuildData,
                ULevelSequence => EAssetCategory.LevelSequence,
                UFoliageType => EAssetCategory.Foliage,

                UItemDefinitionBase => EAssetCategory.ItemDefinitionBase,
                UDataAsset or UDataTable or UCurveTable or UStringTable => EAssetCategory.Data,
                UCurveBase => EAssetCategory.CurveBase,

                UWwiseAssetLibrary or USoundBase or UAkMediaAssetData or UAtomWaveBank or USoundAtomCue
                    or UAtomCueSheet or USoundAtomCueSheet or UFMODBank or UFMODEvent or UAkAudioType
                    or UExternalSource or UExternalSourceBank => EAssetCategory.Audio,
                UFileMediaSource => EAssetCategory.Video,
                UFont or UFontFace => EAssetCategory.Font,

                UNiagaraSystem or UNiagaraScriptBase or UParticleSystem => EAssetCategory.Particle,

                _ => EAssetCategory.All
            };

            switch (AssetCategory)
            {
                case EAssetCategory.Texture when pointer.Object.Value is UTexture texture:
                {
                    if (!resolve.HasFlag(EResolveCompute.Preview))
                        break;

                    var img = texture.Decode(MaxPreviewSize, UserSettings.Default.CurrentDir.TexturePlatform);
                    if (img != null)
                    {
                        using var bitmap = img.ToSkBitmap();
                        using var image = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                        SetPreviewImage(image);
                    }
                    break;
                }
                case EAssetCategory.ItemDefinitionBase:
                    if (!resolve.HasFlag(EResolveCompute.Preview))
                        break;

                    if (pointer.Object.Value is UItemDefinitionBase itemDef)
                    {
                        if (LookupPreview(itemDef.DataList)) break;

                        if (itemDef is UAthenaPickaxeItemDefinition pickaxe && pickaxe.WeaponDefinition.TryLoad(out UItemDefinitionBase weaponDef))
                        {
                            LookupPreview(weaponDef.DataList);
                        }

                        bool LookupPreview(FInstancedStruct[] dataList)
                        {
                            foreach (var data in dataList)
                            {
                                if (!data.NonConstStruct.TryGetValue(out FSoftObjectPath icon, "Icon", "LargeIcon") ||
                                    !icon.TryLoad<UTexture2D>(out var texture))
                                    continue;

                                var img = texture.Decode(MaxPreviewSize, UserSettings.Default.CurrentDir.TexturePlatform);
                                if (img == null) return false;

                                using var bitmap = img.ToSkBitmap();
                                using var image = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                                SetPreviewImage(image);
                                return true;
                            }
                            return false;
                        }
                    }
                    break;
                default:
                    Resolved |= EResolveCompute.Preview;
                    break;
            }
        });
    }

    private Task ResolveByExtensionAsync(EResolveCompute resolve)
    {
        Resolved |= EResolveCompute.Preview;
        switch (Asset.Extension)
        {
            case "uproject":
            case "uefnproject":
            case "upluginmanifest":
            case "uplugin":
            case "ini":
            case "locmeta":
            case "locres":
            case "verse":
            case "lua":
            case "luac":
            case "json5":
            case "json":
            case "bin":
            case "txt":
            case "log":
            case "pem":
            case "xml":
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
                Resolved |= ~EResolveCompute.Preview;
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
            default:
                AssetCategory = EAssetCategory.All; // just so it sets resolved
                break;
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
    public void OnIsVisible()
    {
        if (Resolved == EResolveCompute.All)
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
