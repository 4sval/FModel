using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
    }
}
