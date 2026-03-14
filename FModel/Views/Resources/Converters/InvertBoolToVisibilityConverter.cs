using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// Returns the logical negation of the input bool.
/// Avalonia uses <c>IsVisible</c> (bool); the WPF Visibility.Hidden concept
/// does not exist — both Collapsed and Hidden map to <c>false</c>.
/// </summary>
/// <remarks>File rename from InvertBoolToVisibilityConverter is deferred; class name takes precedence.</remarks>
public class InvertBoolConverter : IValueConverter
{
    public static readonly InvertBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : (object) true;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : (object?) null;
}
