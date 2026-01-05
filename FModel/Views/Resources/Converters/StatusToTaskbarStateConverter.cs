using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Shell;

namespace FModel.Views.Resources.Converters;

public class StatusToTaskbarStateConverter : MarkupExtension, IMultiValueConverter
{
    private static readonly StatusToTaskbarStateConverter _instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[1] is true || values[0] is not EStatusKind kind)
            return TaskbarItemProgressState.None;

        return kind switch
        {
            EStatusKind.Loading => TaskbarItemProgressState.Normal,
            EStatusKind.Stopping => TaskbarItemProgressState.Paused,
            EStatusKind.Failed => TaskbarItemProgressState.Error,
            _ => TaskbarItemProgressState.None,
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => _instance;
}
