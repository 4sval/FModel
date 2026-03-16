using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

public class IntGreaterThanZeroConverter : IValueConverter
{
  public static readonly IntGreaterThanZeroConverter Instance = new();

  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
      => value is int n and > 0;

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
      => throw new NotImplementedException();
}
