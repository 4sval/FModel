using FModel.Extensions;
using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public static readonly EnumToStringConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return null;
                case Enum e:
                    return e.GetDescription();
                default:
                    Type t;
                    t = value.GetType();
                    return t.IsValueType ? ((Enum) Activator.CreateInstance(t)).GetDescription() : value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}