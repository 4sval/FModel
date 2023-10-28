using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FModel.Settings;

namespace FModel.Views.Resources.Converters;

public class EndpointToTypeConverter : IValueConverter
{
    public static readonly EndpointToTypeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not EEndpointType type) throw new NotImplementedException();

        var isValid = UserSettings.IsEndpointValid(type, out _);
        return targetType switch
        {
            not null when targetType == typeof(Visibility) => isValid ? Visibility.Visible : Visibility.Collapsed,
            _ => throw new NotImplementedException()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
