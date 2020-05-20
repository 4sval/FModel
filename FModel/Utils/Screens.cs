using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace FModel.Utils
{
    [ValueConversion(typeof(string), typeof(string))]
    public class Screens : MarkupExtension, IValueConverter
    {
        private static Screens _instance;

        public Screens() { }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        { // do not let the culture default to local to prevent variable outcome re decimal syntax
            double size = System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            return size.ToString("G0", CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        { // read only converter...
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new Screens());
        }
    }
}
