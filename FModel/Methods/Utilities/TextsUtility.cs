using System;
using System.Windows.Media;

namespace FModel.Methods.Utilities
{
    static class TextsUtility
    {
        private const string BASE = "pack://application:,,,/";
        public static readonly FontFamily FBurbank = new FontFamily(new Uri(BASE), "./Resources/#Burbank Big Cd Bd");
        public static readonly FontFamily Burbank = new FontFamily(new Uri(BASE), "./Resources/#Burbank Big Cd Bk");
    }
}
