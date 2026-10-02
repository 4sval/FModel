using System;
using System.Globalization;
using System.Windows.Data;
using FModel.Extensions;

namespace FModel.Views.Resources.Converters;

public class SizeToStringConverter : IValueConverter
{
    public static readonly SizeToStringConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return StringExtensions.GetReadableSize(System.Convert.ToDouble(value));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
