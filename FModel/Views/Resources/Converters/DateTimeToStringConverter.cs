using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        public static readonly DateTimeToStringConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not DateTime dateTime ? value : $"{dateTime.ToLongDateString()}, {dateTime.ToShortTimeString()}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}