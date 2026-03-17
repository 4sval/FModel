using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// Returns 1 when HasImage is true (editor uses its own column),
/// 3 when HasImage is false (editor spans all columns including the hidden image column).
/// </summary>
public class HasImageToColumnSpanConverter : IValueConverter
{
    public static readonly HasImageToColumnSpanConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 1 : 3;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
