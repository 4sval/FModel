using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Rendering;

namespace FModel.Views.Resources.Controls
{
    public class HexColorElementGenerator : VisualLineElementGenerator
    {
        private readonly Regex _hexColorRegex =
            new Regex("\"Hex\": \"(?'target'[0-9A-Fa-f]{3,8})\"$",
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public HexColorElementGenerator()
        {
        }

        private Match FindMatch(int startOffset)
        {
            var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
            var relevantText = CurrentContext.Document.GetText(startOffset, endOffset - startOffset);
            return _hexColorRegex.Match(relevantText);
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

            return m.Groups.TryGetValue("target", out var g) ?
                new HexColorVisualLineText(g.Value, CurrentContext.VisualLine, g.Length + g.Index + 1) :
                null;
        }
    }
}