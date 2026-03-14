using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FModel.Extensions;
using FModel.Settings;
using FModel.Views.Resources.Controls;
using Serilog;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
// TODO(P4-004): Microsoft.Win32.OpenFileDialog / SaveFileDialog not available on Linux — replace with StorageProvider
// TODO(P4-010): System.Drawing.Imaging not available on Linux — SkiaSharp handles TIF natively

namespace FModel.Views;

public partial class ImageMerger : Window
{
    private const string FILENAME = "Preview.png";
    private byte[] _imageBuffer;

    public ImageMerger()
    {
        InitializeComponent();
    }

    private async void DrawPreview(object sender, VectorEventArgs dragCompletedEventArgs)
    {
        if (ImagePreview.Source != null)
            await DrawPreview().ConfigureAwait(false);
    }

    private async void Click_DrawPreview(object sender, PointerReleasedEventArgs e)
    {
        if (ImagePreview.Source != null)
            await DrawPreview().ConfigureAwait(false);
    }

    private async Task DrawPreview()
    {
        AddButton.IsEnabled = false;
        UpButton.IsEnabled = false;
        DownButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;
        ClearButton.IsEnabled = false;
        SizeSlider.IsEnabled = false;
        OpenImageButton.IsEnabled = false;
        SaveImageButton.IsEnabled = false;

        var margin = UserSettings.Default.ImageMergerMargin;
        int num = 1, curW = 0, curH = 0, maxWidth = 0, maxHeight = 0, lineMaxHeight = 0, imagesPerRow = Convert.ToInt32(SizeSlider.Value);
        var positions = new Dictionary<int, SKPoint>();
        var images = new SKBitmap[ImagesListBox.Items.Count];
        for (var i = 0; i < images.Length; i++)
        {
            var item = (ListBoxItem) ImagesListBox.Items[i];
            await using var stream = new FileStream(item.ContentStringFormat, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var ms = new MemoryStream();
            // TODO(P4-010): Was converting TIF via System.Drawing to PNG; SkiaSharp decodes TIF natively
            await stream.CopyToAsync(ms);

            var image = SKBitmap.Decode(ms.ToArray());
            positions[i] = new SKPoint(curW, curH);
            images[i] = image;

            if (image.Height > lineMaxHeight)
                lineMaxHeight = image.Height;

            if (num % imagesPerRow == 0)
            {
                maxWidth = curW + image.Width + margin;
                curH += lineMaxHeight + margin;
                if (curH > maxHeight)
                    maxHeight = curH;

                curW = 0;
                lineMaxHeight = 0;
            }
            else
            {
                maxHeight = curH + lineMaxHeight + margin;
                curW += image.Width + margin;
                if (curW > maxWidth)
                    maxWidth = curW;
            }

            num++;
        }

        try
        {
            await Task.Run(async () =>
            {
                using var bmp = new SKBitmap(maxWidth - margin, maxHeight - margin, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var canvas = new SKCanvas(bmp);

                for (var i = 0; i < images.Length; i++)
                {
                    using (images[i])
                    {
                        canvas.DrawBitmap(images[i], positions[i], new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
                    }
                }

                using var data = bmp.Encode(SKEncodedImageFormat.Png, 100);
                _imageBuffer = data.ToArray();
                var photo = new AvaloniaBitmap(new MemoryStream(_imageBuffer));

                await Dispatcher.UIThread.InvokeAsync(() => { ImagePreview.Source = photo; });
            });
        }
        finally
        {
            AddButton.IsEnabled = true;
            UpButton.IsEnabled = true;
            DownButton.IsEnabled = true;
            DeleteButton.IsEnabled = true;
            ClearButton.IsEnabled = true;
            SizeSlider.IsEnabled = true;
            OpenImageButton.IsEnabled = true;
            SaveImageButton.IsEnabled = true;
        }
    }

    private async void OnImageAdd(object sender, RoutedEventArgs e)
    {
        // TODO(P4-004): OpenFileDialog not available on Linux — replace with StorageProvider.OpenFilePickerAsync
    }

    private async void ModifyItemInList(object sender, RoutedEventArgs e)
    {
        if (ImagesListBox.Items.Count <= 0 || ImagesListBox.SelectedItems.Count <= 0)
            return;
        var indices = ImagesListBox.SelectedItems.Cast<ListBoxItem>().Select(i => ImagesListBox.Items.IndexOf(i)).ToArray();
        var reloadImage = false;

        switch (((Button) sender).Name)
        {
            case "UpButton":
                {
                    if (indices.Length > 0 && indices[0] > 0)
                    {
                        for (var i = 0; i < ImagesListBox.Items.Count; i++)
                        {
                            if (!indices.Contains(i))
                                continue;
                            var item = (ListBoxItem) ImagesListBox.Items[i];
                            ImagesListBox.Items.Remove(item);
                            ImagesListBox.Items.Insert(i - 1, item);
                            item.IsSelected = true;
                            reloadImage = true;
                        }
                    }

                    if (reloadImage)
                    {
                        await DrawPreview().ConfigureAwait(false);
                    }

                    break;
                }
            case "DownButton":
                {
                    if (indices.Length > 0 && indices[^1] < ImagesListBox.Items.Count - 1)
                    {
                        for (var i = ImagesListBox.Items.Count - 1; i > -1; --i)
                        {
                            if (!indices.Contains(i))
                                continue;
                            var item = (ListBoxItem) ImagesListBox.Items[i];
                            ImagesListBox.Items.Remove(item);
                            ImagesListBox.Items.Insert(i + 1, item);
                            item.IsSelected = true;
                            reloadImage = true;
                        }
                    }

                    if (reloadImage)
                    {
                        await DrawPreview().ConfigureAwait(false);
                    }

                    break;
                }
            case "DeleteButton":
                {
                    if (ImagesListBox.Items.Count > 0 && ImagesListBox.SelectedItems.Count > 0)
                    {
                        for (var i = ImagesListBox.SelectedItems.Count - 1; i >= 0; --i)
                            ImagesListBox.Items.Remove(ImagesListBox.SelectedItems[i]);
                    }

                    await DrawPreview().ConfigureAwait(false);

                    break;
                }
        }
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        ImagesListBox.Items.Clear();
        ImagePreview.Source = null;
    }

    private void OnOpenImage(object sender, RoutedEventArgs e)
    {
        if (ImagePreview.Source == null)
            return;
        Helper.OpenWindow<Window>("Merged Image", () =>
        {
            new ImagePopout
            {
                Title = "Merged Image",
                Width = ImagePreview.Source.Size.Width,
                Height = ImagePreview.Source.Size.Height,
                WindowState = ImagePreview.Source.Size.Height > 1000 ? WindowState.Maximized : WindowState.Normal,
                ImageCtrl = { Source = ImagePreview.Source }
            }.Show();
        });
    }

    private void OnSaveImage(object sender, RoutedEventArgs e)
    {
        // TODO(P4-004): SaveFileDialog not available on Linux — replace with StorageProvider.SaveFilePickerAsync
    }

    private static void SaveCheck(string path, string fileName)
    {
        if (File.Exists(path))
        {
            Log.Information("{FileName} successfully saved", fileName);
            FLogger.Append(ELog.Information, () =>
            {
                FLogger.Text("Successfully saved ", Constants.WHITE);
                FLogger.Link(fileName, path, true);
            });
        }
        else
        {
            Log.Error("{FileName} could not be saved", fileName);
            FLogger.Append(ELog.Error, () => FLogger.Text($"Could not save '{fileName}'", Constants.WHITE, true));
        }
    }

    private void OnCopyImage(object sender, RoutedEventArgs e)
    {
        ClipboardExtensions.SetImage(_imageBuffer, FILENAME);
    }
}
