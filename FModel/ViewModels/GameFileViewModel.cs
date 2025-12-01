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
using CUE4Parse.UE4.Assets.Exports.Fmod;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Textures;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using Serilog;
using SkiaSharp;

namespace FModel.ViewModels;

public class GameFileViewModel : ViewModel
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    private static readonly Geometry _defaultIcon = (Geometry) Application.Current.FindResource("AssetIcon");
    private static readonly Geometry _datatableIcon = (Geometry) Application.Current.FindResource("DataTableIcon");
    private static readonly Geometry _mapIcon = (Geometry) Application.Current.FindResource("MapIconAlt");
    private static readonly Geometry _pluginIcon = (Geometry) Application.Current.FindResource("PluginIcon");
    private static readonly Geometry _configIcon = (Geometry) Application.Current.FindResource("ConfigIcon");
    private static readonly Geometry _audioIcon = (Geometry) Application.Current.FindResource("AudioIconAlt");
    private static readonly Geometry _meshIcon = (Geometry) Application.Current.FindResource("MeshIconAlt");
    private static readonly Geometry _blueprintIcon = (Geometry) Application.Current.FindResource("BlueprintIcon");
    private static readonly Geometry _materialIcon = (Geometry) Application.Current.FindResource("MaterialIcon");
    private static readonly Geometry _skeletonIcon = (Geometry) Application.Current.FindResource("SkeletonIcon");
    private static readonly Geometry _physicsIcon = (Geometry) Application.Current.FindResource("PhysicsIcon");
    private static readonly Geometry _localeIcon = (Geometry) Application.Current.FindResource("LocaleIcon");
    private static readonly Geometry _fontIcon = (Geometry) Application.Current.FindResource("FontIcon");
    private static readonly Geometry _luaIcon = (Geometry) Application.Current.FindResource("LuaIcon");
    private static readonly Geometry _jsonIcon = (Geometry) Application.Current.FindResource("JsonIcon");
    private static readonly Geometry _txtIcon = (Geometry) Application.Current.FindResource("TxtIcon");
    private static readonly Geometry _animationIcon = (Geometry) Application.Current.FindResource("AnimationIconAlt");

    public GameFile Asset { get; }
    private ImageSource _previewImage;
    public ImageSource PreviewImage
    {
        get => _previewImage;
        set => SetProperty(ref _previewImage, value);
    }

    private Geometry _iconGeometry;
    public Geometry IconGeometry
    {
        get => _iconGeometry;
        set => SetProperty(ref _iconGeometry, value);
    }

    private Brush _iconColor = Brushes.White;
    public Brush IconColor
    {
        get => _iconColor;
        set => SetProperty(ref _iconColor, value);
    }

    public GameFileViewModel(GameFile asset)
    {
        Asset = asset;
    }

    private async void LoadPreviewAsync()
    {
        try
        {
            if (await LoadPreviewByExtension(Asset.Extension))
                return;

            bool canLoadPackage = Asset.IsUePackage && _applicationView?.CUE4Parse != null;

            if (canLoadPackage)
            {
                await Task.Run(() =>
                {
                    var result = _applicationView.CUE4Parse.Provider.GetLoadPackageResult(Asset);
                    var package = result?.Package;

                    if (package == null)
                        return;

                    for (var i = result.InclusiveStart; i < result.ExclusiveEnd; i++)
                    {
                        var pointer = new FPackageIndex(package, i + 1).ResolvedObject;
                        var dummy = ((AbstractUePackage) package).ConstructObject(pointer.Class?.Object?.Value as UStruct, package);

                        switch (dummy)
                        {
                            case UTexture when pointer.Object.Value is UTexture texture:
                                {
                                    var img = new CTexture[1];
                                    img[0] = texture.Decode(UserSettings.Default.CurrentDir.TexturePlatform);

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

                                    Application.Current.Dispatcher.Invoke(() => PreviewImage = image);
                                    i = result.ExclusiveEnd; // Let's display first found texture
                                    break;
                                }
                            case UDataAsset:
                            case UDataTable:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _datatableIcon;
                                        IconColor = Brushes.White;
                                    });
                                    break;
                                }
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
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _audioIcon;
                                        IconColor = Brushes.White;
                                    });
                                    break;
                                }
                            case USkeleton:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _skeletonIcon;
                                        IconColor = Brushes.White;
                                    });
                                    break;
                                }
                            case UStaticMesh:
                            case USkeletalMesh:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _meshIcon;
                                        IconColor = Brushes.White;
                                    });
                                    break;
                                }
                            case UBlueprint:
                            case UBlueprintGeneratedClass:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _blueprintIcon;
                                        IconColor = Brushes.AliceBlue;
                                    });
                                    break;
                                }
                            case UMaterial:
                            case UMaterialInstance:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _materialIcon;
                                        IconColor = Brushes.White;
                                    });
                                    break;
                                }
                            case UPhysicsAsset:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _physicsIcon;
                                        IconColor = Brushes.White;
                                    });
                                    break;
                                }
                            case UAnimSequenceBase:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _animationIcon;
                                        IconColor = Brushes.White;
                                    });
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                });
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to load preview for {Path}", Asset.Path);
        }

        // Don't load default icon immediately because it will look clunky when async icon is being loaded
        if (IconGeometry == null && PreviewImage == null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IconGeometry = _defaultIcon;
            });
        }
    }

    private Task<bool> LoadPreviewByExtension(string extension)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            switch (extension)
            {
                case "uplugin":
                    IconGeometry = _pluginIcon;
                    IconColor = Brushes.GreenYellow;
                    return true;
                case "ini":
                    IconGeometry = _configIcon;
                    IconColor = Brushes.LightGray;
                    return true;
                case "umap":
                    IconGeometry = _mapIcon;
                    IconColor = new SolidColorBrush(Color.FromRgb(244, 164, 96));
                    return true;
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
                    IconGeometry = _audioIcon;
                    IconColor = Brushes.White;
                    return true;
                case "locmeta":
                case "locres":
                    IconGeometry = _localeIcon;
                    IconColor = new SolidColorBrush(Color.FromRgb(82, 144, 245));
                    return true;
                case "ufont":
                case "otf":
                case "ttf":
                    IconGeometry = _fontIcon;
                    IconColor = Brushes.White;
                    return true;
                case "lua":
                case "luac":
                    IconGeometry = _luaIcon;
                    IconColor = Brushes.White;
                    return true;
                case "json5":
                case "json":
                    IconGeometry = _jsonIcon;
                    IconColor = Brushes.LightGreen;
                    return true;
                case "txt":
                case "log":
                case "pem":
                    IconGeometry = _txtIcon;
                    IconColor = Brushes.White;
                    return true;
                default:
                    return false;
            }
        }).Task;
    }

    private CancellationTokenSource _previewCts;
    public void OnVisibleChanged(bool isVisible)
    {
        if (!isVisible || PreviewImage != null)
            return;

        _previewCts?.Cancel();
        _previewCts = new CancellationTokenSource();
        var token = _previewCts.Token;

        Task.Delay(100, token) // Slight delay so it won't start loading when user scrolls quickly
            .ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    LoadPreviewAsync();
            }, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
