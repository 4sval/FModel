using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;

namespace FModel.Extensions;

public static partial class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetReadableSize(double size)
    {
        if (size == 0) return "0 B";

        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:# ###.##} {sizes[order]}".TrimStart();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNameLineNumber(this string s, string lineToFind)
    {
        if (KismetRegex().IsMatch(lineToFind))
            return s.GetKismetLineNumber(lineToFind);
        if (int.TryParse(lineToFind, out var index))
            return s.GetLineNumber(index);

        lineToFind = $"    \"Name\": \"{lineToFind}\",";
        using var reader = new StringReader(s);
        var lineNum = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNum++;
            if (line.Equals(lineToFind, StringComparison.OrdinalIgnoreCase))
                return lineNum;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetParentExportType(this TextDocument doc, int startOffset)
    {
        var line = doc.GetLineByOffset(startOffset);
        var lineNumber = line.LineNumber - 1;

        while (doc.GetText(line.Offset, line.Length) is { } content)
        {
            if (content.StartsWith("    \"Type\": \"", StringComparison.OrdinalIgnoreCase))
                return content.Split("\"")[3];

            lineNumber--;
            if (lineNumber < 1) break;
            line = doc.GetLineByNumber(lineNumber);
        }

        return string.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetKismetLineNumber(this string s, string input)
    {
        var match = KismetRegex().Match(input);
        var name = match.Groups[1].Value;
        int index = int.Parse(match.Groups[2].Value);
        var lineToFind = $"    \"Name\": \"{name}\",";
        var offset = $"\"StatementIndex\": {index}";
        using var reader = new StringReader(s);
        var lineNum = 0;

        while (reader.ReadLine() is { } line)
        {
            lineNum++;
            if (line.Equals(lineToFind, StringComparison.OrdinalIgnoreCase))
            {
                var objectLine = lineNum;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNum++;
                    if (line.Contains(offset, StringComparison.OrdinalIgnoreCase))
                        return lineNum;
                }
                return objectLine;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetLineNumber(this string s, int index)
    {
        using var reader = new StringReader(s);
        var lineNum = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNum++;
            if (line.Equals("  {"))
                index--;

            if (index == -1)
                return lineNum + 1;
        }

        return -1;
    }

    [GeneratedRegex(@"^(.+)\[(\d+)\]$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex KismetRegex();
}
