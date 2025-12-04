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
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Textures;
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
    private static readonly Geometry _textureIcon = (Geometry) Application.Current.FindResource("TextureIconAlt");

    public string ResolvedAssetType { get; private set; }
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
            if (await LoadPreviewByExtension(Asset))
                return;

            bool canLoadPackage = Asset.IsUePackage && _applicationView?.CUE4Parse != null && _applicationView.IsAssetsExplorerVisible;

            if (canLoadPackage)
            {
                await Task.Run(() =>
                {
                    if (!_applicationView.CUE4Parse.Provider.TryLoadPackage(Asset, out var package))
                        return;

                    for (var i = 0; i < package.ExportMapLength; i++)
                    {
                        var pointer = new FPackageIndex(package, i + 1).ResolvedObject;
                        if (pointer?.Object is null)
                            continue;

                        var dummy = ((AbstractUePackage) package).ConstructObject(pointer.Class?.Object?.Value as UStruct, package);
                        ResolvedAssetType = dummy?.ExportType;

                        switch (dummy)
                        {
                            case UTexture when pointer.Object.Value is UTexture texture:
                                {
                                    if (!UserSettings.Default.PreviewTexturesAssetExplorer)
                                    {
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            IconGeometry = _textureIcon;
                                            IconColor = new SolidColorBrush(Color.FromRgb(201, 125, 239));
                                        });

                                        return;
                                    }

                                    var img = new CTexture[1];
                                    int targetSize = 128;
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

                                    Application.Current.Dispatcher.Invoke(() => PreviewImage = image);
                                    return; // Let's display first found texture
                                }
                            case UDataAsset:
                            case UDataTable:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _datatableIcon;
                                        IconColor = Brushes.White;
                                    });
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
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _audioIcon;
                                        IconColor = Brushes.White;
                                    });
                                    return;
                                }
                            case USkeleton:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _skeletonIcon;
                                        IconColor = Brushes.White;
                                    });
                                    return;
                                }
                            case UStaticMesh:
                            case USkeletalMesh:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _meshIcon;
                                        IconColor = Brushes.White;
                                    });
                                    return;
                                }
                            case UBlueprint:
                            case UBlueprintGeneratedClass:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _blueprintIcon;
                                        IconColor = Brushes.AliceBlue;
                                    });
                                    return;
                                }
                            case UMaterial:
                            case UMaterialInstance:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _materialIcon;
                                        IconColor = Brushes.White;
                                    });
                                    return;
                                }
                            case UPhysicsAsset:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _physicsIcon;
                                        IconColor = Brushes.White;
                                    });
                                    return;
                                }
                            case UAnimSequence:
                            case UAnimMontage:
                            case UAnimSequenceBase:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _animationIcon;
                                        IconColor = Brushes.White;
                                    });
                                    return;
                                }
                            case UFont:
                            case UFontFace:
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        IconGeometry = _fontIcon;
                                        IconColor = Brushes.White;
                                    });
                                    return;
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

    private Task<bool> LoadPreviewByExtension(GameFile gameFile)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            switch (gameFile.Extension)
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
                case "jpg":
                case "png":
                case "bmp":
                    {
                        var data = _applicationView.CUE4Parse.Provider.SaveAsset(gameFile);
                        using var stream = new MemoryStream(data) { Position = 0 };
                        var bitmap = SKBitmap.Decode(stream);

                        if (bitmap != null)
                        {
                            using var image = bitmap.Encode(gameFile.Extension == "jpg" ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 100);
                            using var ms = new MemoryStream(image.ToArray());

                            var bmpImage = new BitmapImage();
                            bmpImage.BeginInit();
                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                            bmpImage.StreamSource = ms;
                            bmpImage.EndInit();
                            bmpImage.Freeze();

                            PreviewImage = bmpImage;
                            return true;
                        }
                        return false;
                    }
                case "svg":
                    {
                        var data = _applicationView.CUE4Parse.Provider.SaveAsset(gameFile);
                        using var stream = new MemoryStream(data) { Position = 0 };
                        var svg = new SkiaSharp.Extended.Svg.SKSvg(new SKSize(512, 512));
                        svg.Load(stream);

                        var bitmap = new SKBitmap(512, 512);
                        using (var canvas = new SKCanvas(bitmap))
                        {
                            canvas.Clear(SKColors.Transparent);
                            canvas.DrawPicture(svg.Picture);
                        }

                        using var img = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                        using var ms = new MemoryStream(img.ToArray());

                        var bmpImage = new BitmapImage();
                        bmpImage.BeginInit();
                        bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                        bmpImage.StreamSource = ms;
                        bmpImage.EndInit();
                        bmpImage.Freeze();

                        PreviewImage = bmpImage;
                        return true;
                    }
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
