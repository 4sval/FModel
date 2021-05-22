using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters
{
    public class MultiParameterConverter : IMultiValueConverter
    {
        public static readonly MultiParameterConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Clone();
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}