using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Utilities
{
    static class ImagesUtility
    {
        public static Color ParseColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        public static ImageSource GetImageSource(Stream stream)
        {
            BitmapImage photo = new BitmapImage();
            using (stream)
            {
                photo.BeginInit();
                photo.CacheOption = BitmapCacheOption.OnLoad;
                photo.StreamSource = stream;
                photo.EndInit();
                photo.Freeze();
            }
            return photo;
        }

        public static BitmapSource CreateTransparency(BitmapSource source, int opacity)
        {
            int pixelsCount = source.PixelWidth * source.PixelHeight;
            int[] pixels = new int[pixelsCount];
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            source.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixelsCount; i++)
            {
                int alpha = (pixels[i] >> 24) & 255;
                int red = (pixels[i] >> 16) & 255;
                int green = (pixels[i] >> 8) & 255;
                int blue = pixels[i] & 255;

                alpha = ChangeColorOpacity(alpha, opacity);

                int color = (alpha << 24) + (red << 16) + (green << 8) + blue;

                pixels[i] = color;
            }

            BitmapSource result = BitmapSource.Create(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);

            return result;
        }
        private static int ChangeColorOpacity(int color, int opacity)
        {
            color -= 255 - opacity;

            return AdjustColorValue(color);
        }
        private static int AdjustColorValue(int color)
        {
            if (color > 255)
            {
                color = 255;
            }
            else if (color < 0)
            {
                color = 0;
            }

            return color;
        }

        public static void LoadImageAfterExtraction(DrawingVisual image)
        {
            if (image != null)
            {
                string name = FWindow.FCurrentAsset; //FCurrentAsset isn't upated inside Dispatcher.InvokeAsync so we put this in another string outside of the dispatcher

                RenderTargetBitmap RTB = new RenderTargetBitmap((int)Math.Floor(image.DescendantBounds.Width), (int)Math.Floor(image.DescendantBounds.Height), 96, 96, PixelFormats.Pbgra32);
                RTB.Render(image);
                RTB.Freeze(); //We freeze to apply the RTB to our imagesource from the UI Thread

                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    FWindow.FMain.ImageBox_Main.Source = BitmapFrame.Create(RTB); //thread safe

                    if (FWindow.FMain.MI_Auto_Save_Images.IsChecked) //auto save images
                    {
                        SaveImage(FProp.Default.FOutput_Path + "\\Icons\\" + name + ".png");
                    }
                });
            }
        }

        public static void SaveImageDialog()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save Image";
            saveFileDialog.FileName = FWindow.FCurrentAsset;
            saveFileDialog.InitialDirectory = FProp.Default.FOutput_Path + "\\Icons\\";
            saveFileDialog.Filter = "PNG Files (*.png)|*.png";
            if (saveFileDialog.ShowDialog() == true)
            {
                SaveImage(saveFileDialog.FileName);
            }
        }

        public static void SaveImage(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)FWindow.FMain.ImageBox_Main.Source));
                encoder.Save(fileStream);

                if (File.Exists(path))
                {
                    DebugHelper.WriteLine("SaveImage: Successfully saved image of {0}", FWindow.FCurrentAsset);

                    new UpdateMyConsole(Path.GetFileNameWithoutExtension(path), CColors.Blue).Append();
                    new UpdateMyConsole(" successfully saved", CColors.White, true).Append();
                }
                else //just in case
                {
                    DebugHelper.WriteLine("SaveImage: Couldn't save image of {0}", FWindow.FCurrentAsset);

                    new UpdateMyConsole("Bruh moment\nCouldn't save ", CColors.White).Append();
                    new UpdateMyConsole(Path.GetFileNameWithoutExtension(path), CColors.Blue, true).Append();
                }
            }
        }
    }
}
