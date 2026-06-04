using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public sealed class EnumFlagToBoolConverter : IValueConverter
{
    public static readonly EnumFlagToBoolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null) return false;

        var enumType = value.GetType();
        if (!enumType.IsEnum) return false;

        var flag = parameter is string s
            ? Enum.Parse(enumType, s, ignoreCase: true)
            : parameter;

        var current = System.Convert.ToInt64(value);
        var wanted  = System.Convert.ToInt64(flag);

        return (current & wanted) == wanted;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
