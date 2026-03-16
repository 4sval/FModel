using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

public class ItemsSourceEmptyToBoolConverter : IMultiValueConverter
{
  public static readonly ItemsSourceEmptyToBoolConverter Instance = new();

  public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
  {
    if (values.Count < 2)
      return false;

    var itemsSource = values[0];
    if (itemsSource == null || ReferenceEquals(itemsSource, AvaloniaProperty.UnsetValue))
      return false;

    var rawCount = values[1];
    if (rawCount == null || ReferenceEquals(rawCount, AvaloniaProperty.UnsetValue))
      return false;

    var count = rawCount switch
    {
      int intCount => intCount,
      long longCount => (int) longCount,
      _ => -1
    };

    return count == 0;
  }
}
