using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters
{
    public class TabSizeConverter : IMultiValueConverter
    {
        public static readonly TabSizeConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not TabControl tabControl)
                return 0;

            var hasDivider = parameter is string;
            var width = tabControl.ActualWidth / (hasDivider ? double.Parse(parameter.ToString() ?? "6") : tabControl.Items.Count);
            return width <= 1 ? 0 : width - (hasDivider ? 8 : 0);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}