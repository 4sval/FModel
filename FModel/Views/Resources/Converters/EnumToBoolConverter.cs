using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public sealed class EnumToBoolConverter : IValueConverter
{
    public static readonly EnumToBoolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        var enumType = value.GetType();
        if (!enumType.IsEnum)
            return false;

        var target = parameter is string text
            ? Enum.Parse(enumType, text, ignoreCase: true)
            : parameter;

        return value.Equals(target);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true && parameter is not null
            ? parameter
            : Binding.DoNothing;
    }
}
