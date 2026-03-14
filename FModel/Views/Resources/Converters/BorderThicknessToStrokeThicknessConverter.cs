using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

public class BorderThicknessToStrokeThicknessConverter : IValueConverter
{
    public static readonly BorderThicknessToStrokeThicknessConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var thickness = value is Thickness t ? t : default;
        return (thickness.Bottom + thickness.Left + thickness.Right + thickness.Top) / 4;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}