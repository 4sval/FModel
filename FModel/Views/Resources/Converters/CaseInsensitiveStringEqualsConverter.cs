using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

public class CaseInsensitiveStringEqualsConverter : IValueConverter
{
    public static readonly CaseInsensitiveStringEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString()?.Equals(parameter?.ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
