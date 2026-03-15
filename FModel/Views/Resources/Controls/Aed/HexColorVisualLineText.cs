using System;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Rendering;

namespace FModel.Views.Resources.Controls;

public class HexColorVisualLineText : VisualLineText
{
    private readonly string _hexColor;

    public HexColorVisualLineText(string hexColor, VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
    {
        _hexColor = hexColor;
    }

    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Apply custom colour to everything except the 2-char separator token (":\xa0").
        var runLength = DocumentLength - (startVisualColumn - VisualColumn);
        if (runLength != 2)
            TextRunProperties.SetForegroundBrush(Brushes.PeachPuff);
        else
            TextRunProperties.SetForegroundBrush(null); // restore default for separator token

        return base.CreateTextRun(startVisualColumn, context);
    }

    protected override VisualLineText CreateInstance(int length)
    {
        return new HexColorVisualLineText(_hexColor, ParentVisualLine, length);
    }
}
