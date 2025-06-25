using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class BooleanAndConverter : IMultiValueConverter
{
    public static readonly BooleanAndConverter Instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        => values.All(v => v is true);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
