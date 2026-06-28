using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public sealed class ProjectNameToVisibilityConverter : IValueConverter
{
    public static readonly ProjectNameToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.Equals(value as string, parameter as string, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
