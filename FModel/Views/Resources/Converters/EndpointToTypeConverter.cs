using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FModel.Settings;
using FModel.ViewModels;

namespace FModel.Views.Resources.Converters;

public class EndpointToTypeConverter : IMultiValueConverter
{
    public static readonly EndpointToTypeConverter Instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not ApplicationViewModel viewModel ||
            values[1] is not EEndpointType type)
            return false;

        var isEnabled = UserSettings.IsEndpointEnabled(viewModel.CUE4Parse.Game, type, out _);
        return targetType switch
        {
            not null when targetType == typeof(Visibility) => isEnabled ? Visibility.Visible : Visibility.Collapsed,
            _ => throw new NotImplementedException()
        };
    }

    public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
