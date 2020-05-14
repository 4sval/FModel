using System;
using System.Linq;
using System.Windows;

namespace FModel.Utils
{
    static class FWindows
    {
        public static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
               ? Application.Current.Windows.OfType<T>().Any()
               : Application.Current.Windows.OfType<T>().Any(w => w.Title.Equals(name));
        }

        public static Window GetOpenedWindow<T>(string name) where T : Window
        {
            return Application.Current.Windows.OfType<T>().FirstOrDefault(w => w.Title.Equals(name));
        }
    }
}
