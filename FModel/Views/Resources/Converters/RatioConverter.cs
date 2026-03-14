using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

public class RatioConverter : IValueConverter
{
    public static readonly RatioConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var size = System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter, culture);
        return size.ToString("G0", culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}