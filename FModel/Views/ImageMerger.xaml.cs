using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using AdonisUI.Controls;
using FModel.Extensions;
using FModel.Settings;
using FModel.Views.Resources.Controls;
using Microsoft.Win32;
using Serilog;
using SkiaSharp;

namespace FModel.Views
{
    public partial class ImageMerger
    {
        public ImageMerger()
        {
            InitializeComponent();
        }

        private async void DrawPreview(object sender, DragCompletedEventArgs dragCompletedEventArgs)
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
                var ms = new MemoryStream();
                var stream = new FileStream(item.ContentStringFormat, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                if (item.ContentStringFormat.EndsWith(".tif"))
                {
                    var mem = new MemoryStream();
                    await stream.CopyToAsync(mem);
                    System.Drawing.Image.FromStream(mem).Save(ms, ImageFormat.Png);
                }
                else
                {
                    await stream.CopyToAsync(ms);
                }

                var image = SKBitmap.Decode(ms.ToArray());
                positions[i] = new SKPoint(curW, curH);
                images[i] = image;

                if (image.Height > lineMaxHeight)
                    lineMaxHeight = image.Height;

                if (num % imagesPerRow == 0)
                {
                    maxWidth = curW + image.Width + margin;
                    curH += lineMaxHeight + margin;

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

            await Task.Run(() =>
            {
                using var ret = new SKBitmap(maxWidth - margin, maxHeight - margin, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                using var c = new SKCanvas(ret);

                for (var i = 0; i < images.Length; i++)
                {
                    using (images[i])
                    {
                        c.DrawBitmap(images[i], positions[i], new SKPaint {FilterQuality = SKFilterQuality.High, IsAntialias = true});
                    }
                }

                var image = SKImage.FromBitmap(ret);
                using var encoded = image.Encode();
                using var stream = encoded.AsStream();
                var photo = new BitmapImage();
                photo.BeginInit();
                photo.CacheOption = BitmapCacheOption.OnLoad;
                photo.StreamSource = stream;
                photo.EndInit();
                photo.Freeze();

                Application.Current.Dispatcher.Invoke(delegate { ImagePreview.Source = photo; });
            }).ContinueWith(t =>
            {
                AddButton.IsEnabled = true;
                UpButton.IsEnabled = true;
                DownButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
                ClearButton.IsEnabled = true;
                SizeSlider.IsEnabled = true;
                OpenImageButton.IsEnabled = true;
                SaveImageButton.IsEnabled = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void OnImageAdd(object sender, RoutedEventArgs e)
        {
            var fileBrowser = new OpenFileDialog
            {
                Title = "Add image(s)",
                InitialDirectory = $"{UserSettings.Default.OutputDirectory}\\Textures",
                Multiselect = true,
                Filter = "Image Files (*.png,*.bmp,*.jpg,*.jpeg,*.jfif,*.jpe,*.tiff,*.tif)|*.png;*.bmp;*.jpg;*.jpeg;*.jfif;*.jpe;*.tiff;*.tif|All Files (*.*)|*.*"
            };

            if (!(bool) fileBrowser.ShowDialog()) return;
            foreach (var file in fileBrowser.FileNames)
            {
                ImagesListBox.Items.Add(new ListBoxItem
                {
                    ContentStringFormat = file,
                    Content = Path.GetFileNameWithoutExtension(file)
                });
            }

            SizeSlider.Value = Math.Min(ImagesListBox.Items.Count, Math.Round(Math.Sqrt(ImagesListBox.Items.Count)));
            await DrawPreview().ConfigureAwait(false);
        }

        private async void ModifyItemInList(object sender, RoutedEventArgs e)
        {
            if (ImagesListBox.Items.Count <= 0 || ImagesListBox.SelectedItems.Count <= 0) return;
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
                            if (!indices.Contains(i)) continue;
                            var item = (ListBoxItem) ImagesListBox.Items[i];
                            ImagesListBox.Items.Remove(item);
                            ImagesListBox.Items.Insert(i - 1, item);
                            item.IsSelected = true;
                            reloadImage = true;
                        }
                    }

                    ImagesListBox.SelectedItems.Add(indices);
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
                            if (!indices.Contains(i)) continue;
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
            if (ImagePreview.Source == null) return;
            Helper.OpenWindow<AdonisWindow>("Merged Image", () =>
            {
                new ImagePopout
                {
                    Title = "Merged Image",
                    Width = ImagePreview.Source.Width,
                    Height = ImagePreview.Source.Height,
                    WindowState = ImagePreview.Source.Height > 1000 ? WindowState.Maximized : WindowState.Normal,
                    ImageCtrl = {Source = ImagePreview.Source}
                }.Show();
            });
        }

        private void OnSaveImage(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (ImagePreview.Source == null) return;
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Image",
                    FileName = "Preview.png",
                    InitialDirectory = UserSettings.Default.OutputDirectory,
                    Filter = "Png Files (*.png)|*.png|All Files (*.*)|*.*"
                };

                if (!(bool) saveFileDialog.ShowDialog()) return;
                using var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource) ImagePreview.Source));
                encoder.Save(fileStream);

                if (!File.Exists(saveFileDialog.FileName)) return;
                Log.Information("{FileName} successfully saved", saveFileDialog.FileName.SubstringAfterLast('\\'));
                FLogger.AppendInformation();
                FLogger.AppendText($"Successfully saved '{saveFileDialog.FileName.SubstringAfterLast('\\')}'", Constants.WHITE, true);
            });
        }

        private void OnCopyImage(object sender, RoutedEventArgs e) => Clipboard.SetImage((BitmapSource) ImagePreview.Source);
    }
}