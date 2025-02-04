using System;
using System.Globalization;
using System.Windows.Data;
using CUE4Parse.Utils;

namespace FModel.Views.Resources.Converters;

public class FullPathToFileConverter : IValueConverter
{
    public static readonly FullPathToFileConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value.ToString().SubstringAfterLast('/');
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
