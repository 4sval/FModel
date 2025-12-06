using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class InvertBoolToVisibilityConverter : IValueConverter
{
    public static readonly InvertBoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return true;
    }
}
