using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class BorderThicknessToStrokeThicknessConverter : IValueConverter
{
    public static readonly BorderThicknessToStrokeThicknessConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var thickness = (Thickness) value;
        return (thickness.Bottom + thickness.Left + thickness.Right + thickness.Top) / 4;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}