using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using FModel;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// Converts <see cref="ELoadingMode"/> to <see cref="SelectionMode"/>.
/// Multiple, All, AllButNew, AllButModified, and AllButPatched use <see cref="SelectionMode.Multiple"/> (multi-select);
/// any future single-file modes would use <see cref="SelectionMode.Single"/>.
/// </summary>
public class LoadingModeToSelectionModeConverter : IValueConverter
{
    public static readonly LoadingModeToSelectionModeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            ELoadingMode.Multiple => SelectionMode.Multiple,
            ELoadingMode.All => SelectionMode.Multiple,
            ELoadingMode.AllButNew => SelectionMode.Multiple,
            ELoadingMode.AllButModified => SelectionMode.Multiple,
            ELoadingMode.AllButPatched => SelectionMode.Multiple,
            _ => SelectionMode.Single
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
