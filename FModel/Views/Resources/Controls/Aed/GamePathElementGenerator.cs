using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Rendering;

namespace FModel.Views.Resources.Controls;

public class GamePathElementGenerator : VisualLineElementGenerator
{
    private readonly Regex _gamePathRegex =
        new("\"(?:ObjectPath|AssetPathName|AssetName|ParameterName|CollisionProfileName|TableId)\": \"(?'target'(?!/?Script/)(.*/.*))\",?$",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public GamePathElementGenerator()
    {
    }

    private Match FindMatch(int startOffset)
    {
        var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
        var relevantText = CurrentContext.Document.GetText(startOffset, endOffset - startOffset);
        return _gamePathRegex.Match(relevantText);
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        var m = FindMatch(startOffset);
        return m.Success ? startOffset + m.Index : -1;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        var m = FindMatch(offset);
        if (!m.Success || m.Index != 0) return null;

        return m.Groups.TryGetValue("target", out var g) ? new GamePathVisualLineText(g.Value, CurrentContext.VisualLine, g.Length + g.Index + 1) : null;
    }
}