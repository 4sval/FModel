using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

public class TabSizeConverter : IMultiValueConverter
{
    public static readonly TabSizeConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || values[0] is not TabControl tabControl)
            return 0;

        // Avalonia uses Bounds.Width instead of WPF's ActualWidth.
        var hasDivider = parameter is string;
        double divisor = hasDivider
            ? double.Parse(parameter!.ToString()!, CultureInfo.InvariantCulture)
            : tabControl.ItemCount;
        if (divisor <= 0) return 0;
        var width = tabControl.Bounds.Width / divisor;
        return width <= 1 ? 0 : width - (hasDivider ? 8 : 0);
    }
}
