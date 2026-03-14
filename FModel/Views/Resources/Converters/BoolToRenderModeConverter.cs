using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace FModel.Views.Resources.Converters;

public class BoolToRenderModeConverter : IValueConverter
{
    public static readonly BoolToRenderModeConverter Instance = new();

    public BitmapInterpolationMode Convert(bool value)
        => value ? BitmapInterpolationMode.None : BitmapInterpolationMode.HighQuality;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // WPF BitmapScalingMode.NearestNeighbor → Avalonia BitmapInterpolationMode.None (no filtering)
        // WPF BitmapScalingMode.Linear          → Avalonia BitmapInterpolationMode.HighQuality
        return value switch
        {
            true => BitmapInterpolationMode.None,
            _    => BitmapInterpolationMode.HighQuality,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
