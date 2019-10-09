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
            }
            return photo;
        }

        public static BitmapSource CreateTransparency(BitmapSource source, int opacity)
        {
            if (source.Format != PixelFormats.Bgra32)
            {
                return source;
            }

            int bytesPerPixel = (source.Format.BitsPerPixel + 7) / 8;
            int stride = bytesPerPixel * source.PixelWidth;
            byte[] buffer = new byte[stride * source.PixelHeight];

            source.CopyPixels(buffer, stride, 0);

            for (int y = 0; y < source.PixelHeight; y++)
            {
                for (int x = 0; x < source.PixelWidth; x++)
                {
                    int i = stride * y + bytesPerPixel * x;
                    if (buffer[i + 3] != 0x00) //do not change the pixels that are already transparent god dammit    
                    {
                        buffer[i + 3] = Convert.ToByte(opacity);
                    }
                }
            }

            return BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                source.Format, null, buffer, stride);
        }

        public static void LoadImageAfterExtraction(DrawingVisual image)
        {
            if (image != null)
            {
                string name = FWindow.FCurrentAsset; //FCurrentAsset isn't upated inside Dispatcher.InvokeAsync so we put this in another string outside of the dispatcher

                RenderTargetBitmap RTB = new RenderTargetBitmap(515, 515, 96, 96, PixelFormats.Pbgra32);
                RTB.Render(image);
                RTB.Freeze(); //We freeze to apply the RTB to our imagesource from the UI Thread

                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    FWindow.FMain.ImageBox_Main.Source = BitmapFrame.Create(RTB); //thread safe and fast af

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
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)FWindow.FMain.ImageBox_Main.Source));
                encoder.Save(fileStream);

                if (File.Exists(path))
                {
                    new UpdateMyConsole(Path.GetFileNameWithoutExtension(path), CColors.Blue).Append();
                    new UpdateMyConsole(" successfully saved", CColors.White, true).Append();
                }
                else //just in case
                {
                    new UpdateMyConsole("Bruh moment\nCouldn't save ", CColors.White).Append();
                    new UpdateMyConsole(Path.GetFileNameWithoutExtension(path), CColors.Blue, true).Append();
                }
            }
        }
    }
}
