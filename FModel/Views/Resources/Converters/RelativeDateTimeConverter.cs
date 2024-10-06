using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class RelativeDateTimeConverter : IValueConverter
{
    public static readonly RelativeDateTimeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime.ToLocalTime();

            int time;
            string unit;
            if (timeSpan.TotalSeconds < 30)
                return "Just now";

            if (timeSpan.TotalMinutes < 1)
            {
                time = timeSpan.Seconds;
                unit = "second";
            }
            else if (timeSpan.TotalHours < 1)
            {
                time = timeSpan.Minutes;
                unit = "minute";
            }
            else switch (timeSpan.TotalDays)
            {
                case < 1:
                    time = timeSpan.Hours;
                    unit = "hour";
                    break;
                case < 7:
                    time = timeSpan.Days;
                    unit = "day";
                    break;
                case < 30:
                    time = timeSpan.Days / 7;
                    unit = "week";
                    break;
                case < 365:
                    time = timeSpan.Days / 30;
                    unit = "month";
                    break;
                default:
                    time = timeSpan.Days / 365;
                    unit = "year";
                    break;
            }

            return $"{time} {unit}{(time > 1 ? "s" : string.Empty)} ago";
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
