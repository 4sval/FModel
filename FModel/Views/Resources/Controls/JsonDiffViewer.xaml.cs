using System.Collections.Generic;
using System.Windows.Media;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FModel.Extensions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace FModel.Views.Resources.Controls;

public partial class JsonDiffViewer
{
    public JsonDiffViewer(string leftText, string rightText)
    {
        InitializeComponent();

        // Always use Document assignment for AvalonEdit
        AvalonLeft.Document = new TextDocument(leftText ?? "");
        AvalonRight.Document = new TextDocument(rightText ?? "");

        var highlighter = AvalonExtensions.HighlighterSelector("");

        AvalonLeft.SyntaxHighlighting = highlighter;
        AvalonRight.SyntaxHighlighting = highlighter;

        HighlightDifferences(AvalonLeft, AvalonRight, leftText, rightText);
    }

    private static void HighlightDifferences(
        ICSharpCode.AvalonEdit.TextEditor left,
        ICSharpCode.AvalonEdit.TextEditor right,
        string leftText, string rightText)
    {
        var diffBuilder = new InlineDiffBuilder(new DiffPlex.Differ());
        var diffResult = diffBuilder.BuildDiffModel(leftText ?? "", rightText ?? "");

        var leftChanged = new HashSet<int>();
        var rightChanged = new HashSet<int>();
        var leftInserted = new HashSet<int>();
        var rightInserted = new HashSet<int>();
        var leftDeleted = new HashSet<int>();
        var rightDeleted = new HashSet<int>();

        int leftLine = 0, rightLine = 0;
        foreach (var line in diffResult.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Unchanged:
                    leftLine++;
                    rightLine++;
                    break;
                case ChangeType.Inserted:
                    rightInserted.Add(rightLine);
                    rightLine++;
                    break;
                case ChangeType.Deleted:
                    leftDeleted.Add(leftLine);
                    leftLine++;
                    break;
                case ChangeType.Modified:
                    leftChanged.Add(leftLine);
                    rightChanged.Add(rightLine);
                    leftLine++;
                    rightLine++;
                    break;
            }
        }

        left.TextArea.TextView.LineTransformers.Add(new DiffColorizer(leftChanged, Color.FromRgb(70, 60, 10)));
        left.TextArea.TextView.LineTransformers.Add(new DiffColorizer(leftDeleted, Color.FromRgb(70, 30, 50)));
        right.TextArea.TextView.LineTransformers.Add(new DiffColorizer(rightChanged, Color.FromRgb(70, 60, 10)));
        right.TextArea.TextView.LineTransformers.Add(new DiffColorizer(rightInserted, Color.FromRgb(30, 70, 30)));
    }

    public class DiffColorizer(HashSet<int> lines, Color color) : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            if (lines.Contains(line.LineNumber - 1))
            {
                ChangeLinePart(line.Offset, line.EndOffset, element =>
                {
                    element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(color));
                });
            }
        }
    }
}
