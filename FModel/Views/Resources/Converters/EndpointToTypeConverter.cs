using System;
using System.Globalization;
using Avalonia.Data.Converters;
using FModel.Settings;

namespace FModel.Views.Resources.Converters;

public class EndpointToTypeConverter : IValueConverter
{
    public static readonly EndpointToTypeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not EEndpointType type)
            throw new ArgumentException($"Expected EEndpointType parameter, got: {parameter?.GetType().Name ?? "null"}");
        return UserSettings.IsEndpointValid(type, out _);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
