using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

public class IsNullToBoolConverter : IValueConverter
{
  public static readonly IsNullToBoolConverter Instance = new();

  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
      => value == null;

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
      => throw new NotImplementedException();
}
