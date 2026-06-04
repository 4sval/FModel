using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class TextToRefreshConverter : IValueConverter
{
    public static readonly TextToRefreshConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt && dt != DateTime.MaxValue)
            return $"Next Refresh: {dt:MMM d, yyyy}";

        return "Next Refresh: Never";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
