using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Texture;
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

    public GameFile Asset { get; }
    private ImageSource _previewImage;
    public ImageSource PreviewImage
    {
        get => _previewImage;
        set => SetProperty(ref _previewImage, value);
    }

    public GameFileViewModel(GameFile asset)
    {
        Asset = asset;
    }

    private async void LoadPreviewAsync()
    {
        if (!Asset.IsUePackage || _applicationView?.CUE4Parse == null)
            return;

        try
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
                                break;
                            }
                        default:
                            break;
                    }
                }
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to load preview for {Path}", Asset.Path);
        }
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
