using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class RatioToGridLengthConverter : IMultiValueConverter
{
    public static readonly RatioToGridLengthConverter Instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return new GridLength(1, GridUnitType.Star);

        var count1 = values[0] is int c1 ? c1 : 0;
        var count2 = values[1] is int c2 ? c2 : 0;

        var total = count1 + count2;
        if (total == 0) return new GridLength(1, GridUnitType.Star);

        var ratio = (double)count1 / total;
        return new GridLength(ratio, GridUnitType.Star);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

