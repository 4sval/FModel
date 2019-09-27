using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FModel.Methods.Utilities
{
    internal static class MBoxesUtility
    {
        internal static ImageSource ToImageSource(this Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }

        internal static string TryAddKeyboardAccellerator(this string input)
        {
            const string accellerator = "_";

            // If it already contains an accellerator, do nothing
            if (input.Contains(accellerator)) { return input; }

            return accellerator + input;
        }
    }
}
