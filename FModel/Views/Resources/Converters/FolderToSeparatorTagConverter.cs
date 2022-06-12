using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class FolderToSeparatorTagConverter : IValueConverter
{
    public static readonly FolderToSeparatorTagConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? $"{value.ToString()?.ToUpper()} PACKAGES" : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}