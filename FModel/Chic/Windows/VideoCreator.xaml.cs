using FModel.Properties;
using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Media.Devices;
using Path = System.IO.Path;

namespace FModel.Chic.Windows
{
    /// <summary>
    /// Interakční logika pro CreateVideo.xaml
    /// </summary>
    public partial class VideoCreator : Window
    {
        private SKBitmap image;
        private SKBitmap TheImage;

        private string sound;

        private string videosDir => Path.Combine(Settings.Default.OutputPath, "Videos");

        public VideoCreator()
        {
            InitializeComponent();
        }

        public VideoCreator(string soundPath)
        {
            InitializeComponent();

            sound = soundPath;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            GenerateImage();
        }

        private void TextInputChanged(object sender, TextChangedEventArgs e)
        {
            GenerateImage();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(videosDir)) Directory.CreateDirectory(videosDir);

            using var img = SKImage.FromBitmap(TheImage);
            using var data = img.Encode(SKEncodedImageFormat.Png, 80);
            using var stream = File.OpenWrite(Path.Combine(Settings.Default.OutputPath, "Icons", "video.png"));
                data.SaveTo(stream);

            var sfd = new SaveFileDialog
            {
                InitialDirectory = videosDir
            };

            if ((bool)sfd.ShowDialog())
            {
                Process.Start(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\ffmpeg.exe", $"-loop 1 -i \"{Settings.Default.OutputPath}\\Icons\\video.png\" -i \"{sound}\" -c:v libx264 -tune stillimage -c:a aac -b:a 192k -vf \"scale = 'iw-mod(iw,2)':'ih-mod(ih,2)',format = yuv420p\" -shortest -movflags +faststart \"{sfd.FileName}\"");
                Close();
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnAddImageClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = Properties.Resources.SelectFile,
                Filter = Properties.Resources.PngFilter,
                InitialDirectory = Path.Combine(Settings.Default.OutputPath, "Icons")
            };
            if ((bool)ofd.ShowDialog())
            {
                image = SKBitmap.Decode(ofd.FileName);
            }

            GenerateImage();
        }

        private void OnRemoveImageClick(object sener, RoutedEventArgs e)
        {
            image = null;

            GenerateImage();
        }

        private void GenerateImage()
        {
            if (image != null) TheImage = image.Copy();
            else
            {
                image = new SKBitmap(640, 360);
                using SKCanvas c = new SKCanvas(image);
                {
                    c.DrawRect(0, 0, 640, 360, new SKPaint
                    {
                        IsAntialias = true,
                        Color = SKColors.Black
                    });
                }

                TheImage = image.Copy();
            }

            using (SKCanvas c = new SKCanvas(TheImage))
            {
                ChicWatermark.DrawWatermark(c, TheImage.Width, true);

                var textPaint = new SKPaint
                {
                    IsAntialias = true,
                    Color = SKColors.White,
                    TextSize = 100,
                    ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 5, 5, SKColors.Black)
                };

                var width = textPaint.MeasureText(TextInput.Text);

                while (width > TheImage.Width)
                {
                    textPaint.TextSize--;
                    width = textPaint.MeasureText(TextInput.Text);
                }

                c.DrawText(TextInput.Text, 2, TheImage.Height - (100 - textPaint.TextSize) / 2, textPaint);
            }

            using var encoded = SKImage.FromBitmap(TheImage).Encode();
            using var stream = encoded.AsStream();
            BitmapImage photo = new BitmapImage();
            photo.BeginInit();
            photo.CacheOption = BitmapCacheOption.OnLoad;
            photo.StreamSource = stream;
            photo.EndInit();
            photo.Freeze();

            Application.Current.Dispatcher.Invoke(delegate
            {
                Preview_Img.Source = photo;
            });
        }
    }
}
