using System;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

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
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var relativeOffset = startVisualColumn - VisualColumn;
        var text = context.GetText(context.VisualLine.FirstDocumentLine.Offset + RelativeTextOffset + relativeOffset, DocumentLength - relativeOffset);

        if (text.Count != 2) // ": "
            TextRunProperties.SetForegroundBrush(Brushes.PeachPuff);

        return new TextCharacters(text.Text, text.Offset, text.Count, TextRunProperties);
    }

    protected override VisualLineText CreateInstance(int length)
    {
        return new HexColorVisualLineText(_hexColor, ParentVisualLine, length);
    }
}
