using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class IntGreaterThanZeroToVisibilityConverter : IValueConverter
{
    public static readonly IntGreaterThanZeroToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int n && n > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
