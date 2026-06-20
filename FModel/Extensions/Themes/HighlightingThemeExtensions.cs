using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using static FModel.Extensions.AvalonExtensions;
using static FModel.Extensions.Themes.JsonHighlightThemes;

namespace FModel.Extensions.Themes;

public static class HighlightingThemeExtensions
{
    public static void ApplyJsonTheme(this IHighlightingDefinition highlighter, EJsonHighlightTheme theme)
    {
        var palette = Get(theme);

        Apply(highlighter, "FieldName", palette.FieldName);
        Apply(highlighter, "String", palette.String);
        Apply(highlighter, "Number", palette.Number);
        Apply(highlighter, "Bool", palette.Bool);
        Apply(highlighter, "Null", palette.Null);
        Apply(highlighter, "Punctuation", palette.Punctuation);
        Apply(highlighter, "Escape", palette.Escape);
    }

    private static void Apply(IHighlightingDefinition highlighter, string name, HighlightColor color)
    {
        var namedColor = highlighter.GetNamedColor(name);
        if (namedColor is null)
            return;

        namedColor.Foreground = new SimpleHighlightingBrush((Color) ColorConverter.ConvertFromString(color.Foreground));
        namedColor.FontWeight = color.Bold ? FontWeights.Bold : null;
        namedColor.FontStyle = color.Italic ? FontStyles.Italic : null;
    }
}
