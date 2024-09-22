using System;
using System.Globalization;
using System.Windows.Data;

namespace FModel.Views.Resources.Converters;

public class CommitMessageConverter : IValueConverter
{
    public static readonly CommitMessageConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string commitMessage)
        {
            var parts = commitMessage.Split("\n\n");
            return parameter?.ToString() == "Title" ? parts[0] : parts.Length > 1 ? parts[1] : string.Empty;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
