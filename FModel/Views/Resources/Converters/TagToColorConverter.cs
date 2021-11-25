using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FModel.ViewModels;

namespace FModel.Views.Resources.Converters
{
    public class TagToColorConverter : IValueConverter
    {
        public static readonly TagToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not CustomPBRMaterial material)
                return new SolidColorBrush(Colors.Red);

            return new SolidColorBrush(Color.FromScRgb(
                material.MaterialColor.Alpha, material.MaterialColor.Red,
                material.MaterialColor.Green, material.MaterialColor.Blue));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
