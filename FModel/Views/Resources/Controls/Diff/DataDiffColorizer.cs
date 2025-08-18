using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using DiffPlex;
using DiffPlex.DiffBuilder.Model;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FModel.Views.Resources.Controls.Diff;

public partial class DataDiffViewer
{
    public static class DiffColors
    {
        public static readonly Brush Insert = new SolidColorBrush(Color.FromRgb(50, 90, 30));
        public static readonly Brush Delete = new SolidColorBrush(Color.FromRgb(140, 50, 50));
        public static readonly Brush Modify = new SolidColorBrush(Color.FromRgb(110, 100, 70));
        public static readonly Brush Move = new SolidColorBrush(Color.FromRgb(70, 100, 155));
        public static readonly Brush CharDiff = new SolidColorBrush(Color.FromRgb(150, 90, 30));
        public static readonly Brush Transparent = Brushes.Transparent;
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

    private class DataDiffColorizer(
        DiffAlignment alignment,
        HashSet<string> movedStrings,
        bool isLeft,
        int lineOffset = 0)
        : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            int row = line.LineNumber - 1 - lineOffset;
            if (row < 0 || row >= alignment.Meta.Count)
                return;

            var meta = alignment.Meta[row];
            var piece = isLeft ? meta.Old : meta.New;

            if (piece == null || piece.Type == ChangeType.Unchanged)
                return;

            Brush baseBrush = piece.Type switch
            {
                ChangeType.Inserted => DiffColors.Insert,
                ChangeType.Deleted => DiffColors.Delete,
                ChangeType.Modified => DiffColors.Modify,
                _ => DiffColors.Transparent
            };

            if (piece.Type == ChangeType.Inserted && movedStrings.Contains(piece.Text))
            {
                baseBrush = DiffColors.Move;
            }

            ChangeLinePart(line.Offset, line.EndOffset, e =>
                e.TextRunProperties.SetBackgroundBrush(baseBrush));

            if (piece.Type == ChangeType.Modified && meta.Old != null && meta.New != null)
            {
                char[] separators = [' ', '\t', '\r', '\n', ',', '.', ':', ';', '"', '\'', '[', ']', '{', '}', '(', ')', '=', '!'];
                var differ = new Differ();
                var diff = differ.CreateWordDiffs(meta.Old.Text ?? "", meta.New.Text ?? "", ignoreWhitespace: false, separators);

                string lineText = CurrentContext.Document.GetText(line);
                int lineStart = line.Offset;

                if (isLeft)
                {
                    var oldWords = diff.PiecesOld;
                    int charIndex = 0;

                    foreach (var block in diff.DiffBlocks)
                    {
                        for (int i = block.DeleteStartA; i < block.DeleteStartA + block.DeleteCountA; i++)
                        {
                            var word = oldWords[i];
                            int index = lineText.IndexOf(word, charIndex, StringComparison.Ordinal);

                            if (index < 0) continue;

                            ChangeLinePart(
                                lineStart + index,
                                lineStart + index + word.Length,
                                e => e.TextRunProperties.SetBackgroundBrush(DiffColors.CharDiff));
                            charIndex = index + word.Length;
                        }
                    }
                }
                else
                {
                    var newWords = diff.PiecesNew;
                    int charIndex = 0;

                    foreach (var block in diff.DiffBlocks)
                    {
                        for (int i = block.InsertStartB; i < block.InsertStartB + block.InsertCountB; i++)
                        {
                            var word = newWords[i];
                            int index = lineText.IndexOf(word, charIndex, StringComparison.Ordinal);

                            if (index < 0) continue;

                            ChangeLinePart(
                                lineStart + index,
                                lineStart + index + word.Length,
                                e => e.TextRunProperties.SetBackgroundBrush(DiffColors.CharDiff));
                            charIndex = index + word.Length;
                        }
                    }
                }
            }
        }

        public static DrawingBrush CreateGapBrush()
        {
            var background = new SolidColorBrush(Color.FromArgb(120, 60, 60, 60));
            var line = new GeometryDrawing(
                null,
                new Pen(new SolidColorBrush(Color.FromArgb(180, 100, 100, 100)), 1),
                new GeometryGroup
                {
                    Children = { new LineGeometry(new Point(0, 0), new Point(4, 4)) }
                });

            return new DrawingBrush
            {
                Drawing = new DrawingGroup
                {
                    Children =
                    {
                        new GeometryDrawing(background, null, new RectangleGeometry(new Rect(0, 0, 4, 4))),
                        line
                    }
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

    private static readonly DrawingBrush _gapBrush = DataDiffColorizer.CreateGapBrush();
    public class GapWidthBackgroundRenderer(Dictionary<int, double> gapLineToWidth)
        : IBackgroundRenderer
    {
        public KnownLayer Layer => KnownLayer.Background;

        // Maps 0-based line index to the pixel width to fill with the gap brush
        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (gapLineToWidth == null || gapLineToWidth.Count == 0)
                return;

            textView.EnsureVisualLines();

            foreach (var visualLine in textView.VisualLines)
            {
                int lineNumber = visualLine.FirstDocumentLine.LineNumber - 1;

                if (!gapLineToWidth.TryGetValue(lineNumber, out double width) || !(width > 0))
                    continue;

                var textLine = visualLine.GetTextLine(0);
                double x = textLine.WidthIncludingTrailingWhitespace - textView.HorizontalOffset;
                double y = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.TextTop)
                           - textView.VerticalOffset;

                var rect = new Rect(new Point(x, y), new Size(width, textLine.Height));
                drawingContext.DrawRectangle(_gapBrush, null, rect);
            }
        }
    }
}
