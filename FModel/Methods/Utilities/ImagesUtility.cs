using Microsoft.Win32;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Utilities
{
    class ImagesUtility
    {
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

        public static void LoadImageAfterExtraction(ImageSource image)
        {
            if (image != null)
            {
                string name = FWindow.FCurrentAsset; //FCurrentAsset isn't upated inside Dispatcher.InvokeAsync so we put this in another string outside of the dispatcher
                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    FWindow.FMain.ImageBox_Main.Source = BitmapFrame.Create((BitmapSource)image); //thread safe and fast af

                    if (FWindow.FMain.MI_Auto_Save_Images.IsChecked) //auto save images
                        SaveImage(FProp.Default.FOutput_Path + "\\Icons\\" + name + ".png");
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
                SaveImage(saveFileDialog.FileName);
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
