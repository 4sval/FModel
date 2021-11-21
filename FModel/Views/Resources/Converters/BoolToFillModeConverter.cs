using System;
using System.Globalization;
using System.Windows.Data;
using SharpDX.Direct3D11;

namespace FModel.Views.Resources.Converters
{
    public class BoolToFillModeConverter : IValueConverter
    {
        public static readonly BoolToFillModeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                FillMode.Solid => true,
                _ => false
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                true => FillMode.Solid,
                _ => FillMode.Wireframe
            };
        }
    }
}
