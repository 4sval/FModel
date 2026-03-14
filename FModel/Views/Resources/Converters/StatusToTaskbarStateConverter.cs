using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// Stub: Avalonia has no cross-platform TaskbarItemProgressState equivalent.
/// Returns null; actual taskbar integration is tracked by TODO(P2-015).
/// Previously mapped EStatusKind to System.Windows.Shell.TaskbarItemProgressState.
/// </summary>
public class StatusToTaskbarStateConverter : IValueConverter
{
    public static readonly StatusToTaskbarStateConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // TODO(P2-015): Implement via Avalonia taskbar API when available.
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
