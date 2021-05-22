using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace FModel.Extensions
{
    public static class StringExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetReadableSize(double size)
        {
            if (size == 0) return "0 B";

            string[] sizes = {"B", "KB", "MB", "GB", "TB"};
            var order = 0;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:# ###.##} {sizes[order]}".TrimStart();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBefore(this string s, char delimiter)
        {
            var index = s.IndexOf(delimiter);
            return index == -1 ? s : s[..index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBefore(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.IndexOf(delimiter, comparisonType);
            return index == -1 ? s : s[..index];
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
            return index == -1 ? s : s[..index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBeforeLast(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.LastIndexOf(delimiter, comparisonType);
            return index == -1 ? s : s[..index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBeforeWithLast(this string s, char delimiter)
        {
            var index = s.LastIndexOf(delimiter);
            return index == -1 ? s : s[..(index + 1)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubstringBeforeWithLast(this string s, string delimiter, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var index = s.LastIndexOf(delimiter, comparisonType);
            return index == -1 ? s : s[..(index + 1)];
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
        public static int GetLineNumber(this string s, string lineToFind)
        {
            if (int.TryParse(lineToFind, out var index))
                return s.GetLineNumber(index);

            lineToFind = $"    \"Name\": \"{lineToFind}\",";
            using var reader = new StringReader(s);
            var lineNum = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;
                if (line.Equals(lineToFind, StringComparison.OrdinalIgnoreCase))
                    return lineNum;
            }

            return 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetLineNumber(this string s, int index)
        {
            using var reader = new StringReader(s);
            var lineNum = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;
                if (line.Equals("  {"))
                    index--;

                if (index == -1)
                    return lineNum + 1;
            }

            return 1;
        }
    }
}