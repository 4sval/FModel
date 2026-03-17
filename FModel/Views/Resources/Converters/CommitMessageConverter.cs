using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

public class CommitMessageConverter : IValueConverter
{
    public static readonly CommitMessageConverter Instance = new();

    // Message may be cleaned by UpdateViewModel.LoadCoAuthors() which strips
    // "Co-authored-by:" lines and trims; the split on "\n\n" still yields the
    // correct title/description parts after that cleanup.
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string commitMessage)
        {
            var parts = commitMessage.Split("\n\n");
            var param = parameter?.ToString();
            if (param == "Title")
                return parts[0];
            if (param == "HasDescription")
                return parts.Length > 1 && !string.IsNullOrEmpty(parts[1]);
            return parts.Length > 1 ? parts[1] : string.Empty;
        }
        return parameter?.ToString() == "HasDescription" ? false : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
