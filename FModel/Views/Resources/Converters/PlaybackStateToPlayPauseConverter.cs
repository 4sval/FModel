using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// Maps playback state values to Play/Pause icon geometry resources and tooltip text.
/// The value is compared by string to avoid hard dependency on CSCore enum types.
/// </summary>
public class PlaybackStateToPlayPauseConverter : IValueConverter
{
    public static readonly PlaybackStateToPlayPauseConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isPlaying = string.Equals(value?.ToString(), "Playing", StringComparison.OrdinalIgnoreCase);
        var mode = parameter?.ToString();

        if (string.Equals(mode, "Tooltip", StringComparison.OrdinalIgnoreCase))
            return isPlaying ? "Pause" : "Play";

        // "Icon" parameter or no parameter — return geometry resource
        var resourceKey = isPlaying ? "PauseIcon" : "PlayIcon";
        if (Application.Current is null)
            return null;
        return Application.Current.TryGetResource(resourceKey, null, out var resource)
            ? resource
            : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
