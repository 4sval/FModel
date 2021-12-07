using System;
using System.ComponentModel;
using System.Resources;
using System.Runtime.CompilerServices;

using FModel.Properties;

namespace FModel.Extensions
{
    public static class EnumExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDescription(this Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            if (fi == null) return $"{value} ({value:D})";
            var attributes = (DescriptionAttribute[]) fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : $"{value} ({value:D})";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetLocalizedDescription(this Enum value) => value.GetLocalizedDescription(Resources.ResourceManager);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetLocalizedDescription(this Enum value, ResourceManager resourceManager)
        {
            var resourceName = value.GetType().Name + "_" + value;
            var description = resourceManager.GetString(resourceName);

            if (string.IsNullOrEmpty(description))
            {
                description = value.GetDescription();
            }

            return description;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetLocalizedCategory(this Enum value) => value.GetLocalizedCategory(Resources.ResourceManager);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetLocalizedCategory(this Enum value, ResourceManager resourceManager)
        {
            var resourceName = value.GetType().Name + "_" + value + "_Category";
            var description = resourceManager.GetString(resourceName);

            return description;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToEnum<T>(this string value, T defaultValue) where T : struct
        {
            if (!Enum.TryParse(value, true, out T ret))
                return defaultValue;

            return ret;
        }

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
}
