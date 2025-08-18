using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Rendering;

namespace FModel.Views.Resources.Controls;

public class JumpElementGenerator : VisualLineElementGenerator
{
    private readonly Regex _JumpRegex = new(
        @"\b(?:goto\s+Label_(?'target'\d+);)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private Match FindMatch(int startOffset)
    {
        var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
        var relevantText = CurrentContext.Document.GetText(startOffset, endOffset - startOffset);
        return _JumpRegex.Match(relevantText);
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        var m = FindMatch(startOffset);
        return m.Success ? startOffset + m.Index : -1;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        var m = FindMatch(offset);
        if (!m.Success || m.Index != 0 ||
            !m.Groups.TryGetValue("target", out var g))
            return null;

        return new JumpVisualLineText(g.Value, CurrentContext.VisualLine, g.Length + g.Index + 1);
    }
}
