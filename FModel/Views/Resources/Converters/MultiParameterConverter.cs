using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

public class MultiParameterConverter : IMultiValueConverter
{
    public static readonly MultiParameterConverter Instance = new();

    // Avalonia IMultiValueConverter passes IList<object?> instead of object[].
    // Return a snapshot array so callers (CommandParameter multi-bindings) receive
    // the same object[] they previously got from the WPF values.Clone() call.
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.ToArray();
    }
}
