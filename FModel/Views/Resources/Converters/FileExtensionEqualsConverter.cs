using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class FileExtensionEqualsConverter : IValueConverter
{
    public static readonly FileExtensionEqualsConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value.ToString().EndsWith(parameter.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}