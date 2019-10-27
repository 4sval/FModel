using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfHexaEditor.Core.MethodExtention
{
    public static class StringExtension
    {
        /// <summary>
        /// The screen size of a string
        /// </summary>
        /// <remarks>
        /// Code from :
        /// https://stackoverflow.com/questions/11447019/is-there-any-way-to-find-the-width-of-a-character-in-a-fixed-width-font-given-t
        /// 
        /// Modified/adapted by Derek Tremblay
        /// </remarks>
        public static Size GetScreenSize(this string text, FontFamily fontFamily, double fontSize, FontStyle fontStyle,
            FontWeight fontWeight, FontStretch fontStretch, Brush foreGround)
        {
            fontFamily = new TextBlock().FontFamily;
            fontSize = fontSize > 0 ? fontSize : new TextBlock().FontSize;

            var typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);

            var ft = new FormattedText(text ?? string.Empty, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                typeface, fontSize, foreGround, VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);


            return new Size(ft.Width, ft.Height);
        }
    }
}