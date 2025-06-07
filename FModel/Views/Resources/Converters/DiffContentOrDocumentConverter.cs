using System;
using System.Globalization;
using System.Windows.Data;
using FModel.ViewModels;

namespace FModel.Views.Resources.Converters;

public class DiffContentOrDocumentConverter : IValueConverter
{
    public static readonly DiffContentOrDocumentConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tabItem = value as TabItem;
        if (tabItem == null)
            return null;
        if (tabItem.DiffContent != null)
            return tabItem.DiffContent;
        return tabItem.Document != null ? tabItem : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
