using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters
{
    public class FileNameWithoutExtensionConverter : IValueConverter
    {
        public static readonly FileNameWithoutExtensionConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string s ? Path.GetFileNameWithoutExtension(s) : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
