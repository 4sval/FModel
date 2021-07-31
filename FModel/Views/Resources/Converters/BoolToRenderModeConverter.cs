using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FModel.Views.Resources.Converters
{
    public class BoolToRenderModeConverter : IValueConverter
    {
        public static readonly BoolToRenderModeConverter Instance = new();

        public BitmapScalingMode Convert(bool value) => (BitmapScalingMode) Convert(value, null, null, null);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                true => BitmapScalingMode.NearestNeighbor,
                _ => BitmapScalingMode.Linear
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}