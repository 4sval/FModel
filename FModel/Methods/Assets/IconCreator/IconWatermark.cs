using FModel.Methods.Utilities;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets.IconCreator
{
    static class IconWatermark
    {
        public static void DrawIconWatermark()
        {
            if (FProp.Default.FUseWatermark)
            {
                using (StreamReader image = new StreamReader(FProp.Default.FWatermarkFilePath))
                {
                    if (image != null)
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = image.BaseStream;
                        bmp.EndInit();

                        IconCreator.ICDrawingContext.DrawImage(ImagesUtility.CreateTransparency(bmp, FProp.Default.FWatermarkOpacity), new Rect(FProp.Default.FWatermarkXPos, FProp.Default.FWatermarkYPos, FProp.Default.FWatermarkScale, FProp.Default.FWatermarkScale));
                    }
                }
            }
        }
    }
}
