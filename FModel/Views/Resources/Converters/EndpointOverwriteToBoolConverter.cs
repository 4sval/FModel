using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FModel.Framework;
using FModel.Settings;
using FModel.ViewModels;

namespace FModel.Views.Resources.Converters;

public class EndpointOverwriteToBoolConverter : IMultiValueConverter
{
    public static readonly EndpointOverwriteToBoolConverter Instance = new();

    private ApplicationViewModel _vm;
    private EEndpointType _type;
    private FEndpoint _endpoint;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        _vm = values[0] as ApplicationViewModel;
        _type = values[1] as EEndpointType? ?? EEndpointType.Mapping;
        if (_vm == null || !UserSettings.TryGetGameCustomEndpoint(_vm.CUE4Parse.Game, _type, out _endpoint))
            return default;

        return targetType switch
        {
            not null when targetType == typeof(bool?) => _endpoint.Overwrite, // IsChecked
            not null when targetType == typeof(string) => _endpoint.Path,
            not null when targetType == typeof(Visibility) => _endpoint.Overwrite ? Visibility.Visible : Visibility.Collapsed,
            _ => throw new NotImplementedException()
        };
    }

    public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
    {
        var t = value.GetType();
        switch (t)
        {
            case not null when t == typeof(bool):
                _endpoint.Overwrite = (bool)value;
                break;
            case not null when t == typeof(string):
                _endpoint.Path = (string)value;
                break;
            default:
                throw new NotImplementedException();
        }

        return new object[] { _vm, _type };
    }
}
