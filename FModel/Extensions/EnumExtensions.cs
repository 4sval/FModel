using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FModel.Extensions;

public static class EnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetDescription(this Enum value)
    {
        var fi = value.GetType().GetField(value.ToString());
        if (fi == null) return $"{value} ({value:D})";

        var attributes = (DescriptionAttribute[]) fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (attributes.Length > 0) return attributes[0].Description;


        var suffix = $"{value:D}";
        var current = Convert.ToInt32(suffix);
        var target = current & ~0xF;
        if (current != target)
        {
            var values = Enum.GetValues(value.GetType());
            var index = Array.IndexOf(values, value);
            suffix = values.GetValue(index - (current - target))?.ToString();
        }
        return $"{value} ({suffix})";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ToEnum<T>(this string value, T defaultValue) where T : struct => !Enum.TryParse(value, true, out T ret) ? defaultValue : ret;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAnyFlags<T>(this T flags, T contains) where T : Enum, IConvertible => (flags.ToInt32(null) & contains.ToInt32(null)) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(this Enum value)
    {
        var values = Enum.GetValues(value.GetType());
        return Array.IndexOf(values, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Next<T>(this Enum value)
    {
        var values = Enum.GetValues(value.GetType());
        var i = Array.IndexOf(values, value) + 1;
        return i == values.Length ? (T) values.GetValue(0) : (T) values.GetValue(i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Previous<T>(this Enum value)
    {
        var values = Enum.GetValues(value.GetType());
        var i = Array.IndexOf(values, value) - 1;
        return i == -1 ? (T) values.GetValue(values.Length - 1) : (T) values.GetValue(i);
    }
}
