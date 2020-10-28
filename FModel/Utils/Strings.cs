using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace FModel.Utils
{

    public static class Strings
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetReadableSize(double size)
        {
            string[] sizes = { Properties.Resources.B, Properties.Resources.KB, Properties.Resources.MB, Properties.Resources.GB, Properties.Resources.TB };
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return string.Format("{0:# ###.##} {1}", size, sizes[order]).TrimStart();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FixPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length < 5)
                return string.Empty;

            string trigger;
            {
                string tempPath = path[1..];
                trigger = tempPath.Substring(0, tempPath.IndexOf('/'));
            }

            Regex regex = new Regex(trigger);
            if (trigger.Equals("SrirachaRanch"))
                trigger += $"/{trigger}Core";

            string fixedPath = trigger switch
            {
                "Game" => regex.Replace(path, $"{Folders.GetGameName()}/Content", 1),
                "RegionCN" => regex.Replace(path, $"{Folders.GetGameName()}/Plugins/{trigger}/Content", 1),
                _ => regex.Replace(path, $"{Folders.GetGameName()}/Plugins/GameFeatures/{trigger}/Content", 1)
            };

            int sep = fixedPath.LastIndexOf('.');
            return fixedPath.Substring(0, sep > 0 ? sep : fixedPath.Length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBefore(this string s, char delimiter)
        {
            var index = s.IndexOf(delimiter);
            return index == -1 ? s : s.Substring(0, index);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBefore(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.IndexOf(delimiter, comparisonType);
            return index == -1 ? s : s.Substring(0, index);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringAfter(this string s, char delimiter)
        {
            var index = s.IndexOf(delimiter);
            return index == -1 ? s : s.Substring(index + 1, s.Length - index - 1);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringAfter(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.IndexOf(delimiter, comparisonType);
            return index == -1 ? s : s.Substring(index + delimiter.Length, s.Length - index - delimiter.Length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBeforeLast(this string s, char delimiter)
        {
            var index = s.LastIndexOf(delimiter);
            return index == -1 ? s : s.Substring(0, index);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBeforeWithLast(this string s, char delimiter)
        {
            var index = s.LastIndexOf(delimiter);
            return index == -1 ? s : s.Substring(0, index + 1);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBeforeLast(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.LastIndexOf(delimiter, comparisonType);
            return index == -1 ? s : s.Substring(0, index);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringAfterLast(this string s, char delimiter)
        {
            var index = s.LastIndexOf(delimiter);
            return index == -1 ? s : s.Substring(index + 1, s.Length - index - 1);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringAfterLast(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.LastIndexOf(delimiter, comparisonType);
            return index == -1 ? s : s.Substring(index + delimiter.Length, s.Length - index - delimiter.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this string orig, string value, StringComparison comparisonType) =>
            orig.IndexOf(value, comparisonType) >= 0;
    }
}
