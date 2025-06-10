using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FModel.Extensions;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FModel.Views.Resources.Controls.Diff;

public partial class DataDiffViewer
{
    private const string GapPrefix = "⎯ GAP ⎯ ";
    private static string MakeGapString(int width)
    {
        if (width < 0)
            width = 0;
        return GapPrefix + new string(' ', width);
    }

    public DataDiffViewer(string leftText, string rightText, string extension)
    {
        InitializeComponent();

        var highlighter = AvalonExtensions.HighlighterSelector(extension);
        AvalonLeft.SyntaxHighlighting = highlighter;
        AvalonRight.SyntaxHighlighting = highlighter;

        ApplyAlignedDiff(AvalonLeft, AvalonRight, leftText ?? string.Empty, rightText ?? string.Empty);
    }

    private static void ApplyAlignedDiff(
        TextEditor leftEditor,
        TextEditor rightEditor,
        string leftText,
        string rightText)
    {
        var differ = new Differ();
        var sideBuilder = new SideBySideDiffBuilder(differ);
        var model = sideBuilder.BuildDiffModel(leftText, rightText);

        var aligned = AlignLinesWithGaps(model);

        leftEditor.Document = new TextDocument(string.Join("\n", aligned.LeftLines));
        rightEditor.Document = new TextDocument(string.Join("\n", aligned.RightLines));

        leftEditor.TextArea.TextView.LineTransformers.Add(
            new JsonDiffColorizer(aligned, isLeft: true));
        rightEditor.TextArea.TextView.LineTransformers.Add(
            new JsonDiffColorizer(aligned, isLeft: false));
    }

    private static DiffAlignment AlignLinesWithGaps(SideBySideDiffModel model)
    {
        var leftLines = new List<string>();
        var rightLines = new List<string>();
        var meta = new List<LineMeta>();

        int max = Math.Max(model.OldText.Lines.Count, model.NewText.Lines.Count);
        for (int i = 0; i < max; i++)
        {
            var oldLine = i < model.OldText.Lines.Count ? model.OldText.Lines[i] : null;
            var newLine = i < model.NewText.Lines.Count ? model.NewText.Lines[i] : null;

            string leftText, rightText;

            if (oldLine == null || oldLine.Type == ChangeType.Imaginary)
            {
                int width = (newLine != null && newLine.Type != ChangeType.Imaginary) ? newLine.Text.Length : 0;
                leftText = MakeGapString(width);
            }
            else
            {
                leftText = oldLine.Text;
            }

            if (newLine == null || newLine.Type == ChangeType.Imaginary)
            {
                int width = (oldLine != null && oldLine.Type != ChangeType.Imaginary) ? oldLine.Text.Length : 0;
                rightText = MakeGapString(width);
            }
            else
            {
                rightText = newLine.Text;
            }

            leftLines.Add(leftText);
            rightLines.Add(rightText);
            meta.Add(new LineMeta(oldLine, newLine));
        }

        return new DiffAlignment(leftLines, rightLines, meta);
    }

    public class DiffAlignment(List<string> l, List<string> r, List<LineMeta> m)
    {
        public List<string> LeftLines { get; } = l;
        public List<string> RightLines { get; } = r;
        public List<LineMeta> Meta { get; } = m;
    }

    public class LineMeta(DiffPiece old, DiffPiece @new)
    {
        public DiffPiece Old { get; } = old;
        public DiffPiece New { get; } = @new;
    }

    private class JsonDiffColorizer(DiffAlignment alignment, bool isLeft)
        : DocumentColorizingTransformer
    {
        private readonly Brush _gapTextureBrush = CreateGapBrush();

        protected override void ColorizeLine(DocumentLine line)
        {
            int row = line.LineNumber - 1;
            if (row < 0 || row >= alignment.Meta.Count)
                return;

            var meta = alignment.Meta[row];
            var piece = isLeft ? meta.Old : meta.New;
            var text = (isLeft ? alignment.LeftLines : alignment.RightLines)[row];

            // Detect gap line by prefix
            bool isGap = text.StartsWith(GapPrefix);

            if (isGap)
            {
                ChangeLinePart(line.Offset, line.Offset + GapPrefix.Length, e =>
                {
                    e.TextRunProperties.SetBackgroundBrush(_gapTextureBrush);
                    e.TextRunProperties.SetForegroundBrush(Brushes.Transparent); // Hide prefix
                });
                ChangeLinePart(line.Offset + GapPrefix.Length, line.EndOffset, e =>
                    e.TextRunProperties.SetBackgroundBrush(_gapTextureBrush));
                return;
            }

            if (piece == null || piece.Type == ChangeType.Unchanged)
                return;

            Color baseColor = piece.Type switch
            {
                ChangeType.Inserted => Color.FromRgb(50, 90, 30),
                ChangeType.Deleted => Color.FromArgb(140, 140, 50, 50),
                ChangeType.Modified => Color.FromRgb(110, 100, 70),
                _ => Colors.Transparent
            };

            bool isMoved = piece.Type == ChangeType.Inserted &&
                           alignment.Meta.Any(m => m.Old != null && m.Old.Text == piece.Text);
            if (isMoved)
                baseColor = Color.FromRgb(70, 100, 155);

            ChangeLinePart(line.Offset, line.EndOffset, e =>
                e.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(baseColor)));
        }

        private static DrawingBrush CreateGapBrush()
        {
            var background = new SolidColorBrush(Color.FromArgb(120, 60, 60, 60));
            var line = new GeometryDrawing(
                null,
                new Pen(new SolidColorBrush(Color.FromArgb(180, 100, 100, 100)), 1),
                new GeometryGroup { Children = [new LineGeometry(new Point(0, 0), new Point(4, 4))] });

            return new DrawingBrush
            {
                Drawing = new DrawingGroup
                {
                    Children =
                    [
                        new GeometryDrawing(background, null, new RectangleGeometry(new Rect(0, 0, 4, 4))),
                        line
                    ]
                },
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 4, 4),
                ViewportUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, 4, 4),
                ViewboxUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None
            };
        }
    }
}
