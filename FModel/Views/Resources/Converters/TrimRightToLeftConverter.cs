using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters
{
    public class TrimRightToLeftConverter : IValueConverter
    {
        public static readonly TrimRightToLeftConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            var path = value.ToString();
            var maxWidth = System.Convert.ToDouble(parameter, culture);

            var did = false;
            while (path != null && path.Length > maxWidth / 7)
            {
                did = true;
                path = path[2..];
            }

            return did ? $"...{path}" : path;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}