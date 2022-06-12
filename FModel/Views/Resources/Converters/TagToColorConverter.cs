using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using HelixToolkit.Wpf.SharpDX;

namespace FModel.Views.Resources.Converters;

public class TagToColorConverter : IValueConverter
{
    public static readonly TagToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PBRMaterial material)
            return new SolidColorBrush(Colors.Red);

        return new SolidColorBrush(Color.FromScRgb(
            material.AlbedoColor.Alpha, material.AlbedoColor.Red,
            material.AlbedoColor.Green, material.AlbedoColor.Blue));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}