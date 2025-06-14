using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FModel.Extensions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FModel.Views.Resources.Controls.Diff;

public partial class DataDiffViewer
{
    private readonly List<string> _leftChunks;
    private readonly List<string> _rightChunks;

    private ScrollViewer _scroll;

    private int _loadedChunkIndex;
    private const int ChunksPerLoad = 1;
    private bool _isLoading;

    private readonly DiffAlignment _globalAlignment = new([], [], []);
    private readonly HashSet<string> _globalMovedStrings = [];

    private DataDiffColorizer _globalLeftColorizer;
    private DataDiffColorizer _globalRightColorizer;
    private GapWidthBackgroundRenderer _leftGapRenderer;
    private GapWidthBackgroundRenderer _rightGapRenderer;

    public DataDiffViewer(List<string> leftChunks, List<string> rightChunks, string extension)
    {
        InitializeComponent();

        var highlighter = AvalonExtensions.HighlighterSelector(extension);
        AvalonLeft.SyntaxHighlighting = highlighter;
        AvalonRight.SyntaxHighlighting = highlighter;

        _leftChunks = leftChunks ?? [];
        _rightChunks = rightChunks ?? [];

        Loaded += DataDiffViewer_Loaded;
    }

    public async Task Initialize()
    {
        await LoadInitialDiff();
    }

    private async Task LoadInitialDiff()
    {
        _loadedChunkIndex = 0;
        AvalonLeft.Text = string.Empty;
        AvalonRight.Text = string.Empty;

        _globalMovedStrings.Clear();

        if (_globalLeftColorizer != null)
            AvalonLeft.TextArea.TextView.LineTransformers.Remove(_globalLeftColorizer);
        if (_globalRightColorizer != null)
            AvalonRight.TextArea.TextView.LineTransformers.Remove(_globalRightColorizer);

        if (_leftGapRenderer != null)
            AvalonLeft.TextArea.TextView.BackgroundRenderers.Remove(_leftGapRenderer);
        if (_rightGapRenderer != null)
            AvalonRight.TextArea.TextView.BackgroundRenderers.Remove(_rightGapRenderer);

        _globalLeftColorizer = null;
        _globalRightColorizer = null;

        await LoadMoreChunksAsync();
    }

    private async Task LoadMoreChunksAsync()
    {
        if (_isLoading)
            return;

        if (_loadedChunkIndex >= Math.Max(_leftChunks.Count, _rightChunks.Count))
            return;

        _isLoading = true;

        int chunksToLoad = Math.Min(ChunksPerLoad, Math.Max(_leftChunks.Count, _rightChunks.Count) - _loadedChunkIndex);

        for (int i = 0; i < chunksToLoad; i++)
        {
            string leftChunk = _loadedChunkIndex + i < _leftChunks.Count ? _leftChunks[_loadedChunkIndex + i] : "";
            string rightChunk = _loadedChunkIndex + i < _rightChunks.Count ? _rightChunks[_loadedChunkIndex + i] : "";

            var builder = new SideBySideDiffBuilder();
            var model = await Task.Run(() => builder.BuildDiffModel(leftChunk, rightChunk));
            var alignment = AlignLinesWithGaps(model);

            _globalAlignment.LeftLines.AddRange(alignment.LeftLines);
            _globalAlignment.RightLines.AddRange(alignment.RightLines);
            _globalAlignment.Meta.AddRange(alignment.Meta);

            foreach (var moved in alignment.Meta
                         .Where(m => m.Old != null && m.New != null && m.Old.Text == m.New.Text)
                         .Select(m => m.New.Text))
            {
                _globalMovedStrings.Add(moved);
            }

            var leftText = string.Join("\n", alignment.LeftLines) + "\n";
            var rightText = string.Join("\n", alignment.RightLines) + "\n";

            AvalonLeft.Document.BeginUpdate();
            AvalonRight.Document.BeginUpdate();

            if (_loadedChunkIndex == 0)
            {
                AvalonLeft.Document.Text = leftText;
                AvalonRight.Document.Text = rightText;
            }
            else
            {
                AvalonLeft.Document.Text += leftText;
                AvalonRight.Document.Text += rightText;
            }

            AvalonLeft.Document.EndUpdate();
            AvalonRight.Document.EndUpdate();

            _loadedChunkIndex++;
        }

        SetupGapRenderers();

        if (_globalLeftColorizer == null)
        {
            _globalLeftColorizer = new DataDiffColorizer(_globalAlignment, _globalMovedStrings, isLeft: true);
            AvalonLeft.TextArea.TextView.LineTransformers.Add(_globalLeftColorizer);
        }


        if (_globalRightColorizer == null)
        {
            _globalRightColorizer = new DataDiffColorizer(_globalAlignment, _globalMovedStrings, isLeft: false);
            AvalonRight.TextArea.TextView.LineTransformers.Add(_globalRightColorizer);
        }

        AvalonLeft.TextArea.TextView.Redraw();
        AvalonRight.TextArea.TextView.Redraw();

        _isLoading = false;
    }

    // Only reference to one scroll is needed because they are aligned
    private void DataDiffViewer_Loaded(object sender, RoutedEventArgs e)
    {
        _scroll = FindScrollViewer(AvalonLeft);
        if (_scroll != null)
            _scroll.ScrollChanged += Scroll_ScrollChanged;
    }

    private void Scroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_isLoading)
            return;

        if (sender is ScrollViewer sv && IsNearBottom(sv))
        {
            _ = LoadMoreChunksAsync();
        }
    }

    private static bool IsNearBottom(ScrollViewer sv)
    {
        return sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 50;
    }

    private static ScrollViewer FindScrollViewer(DependencyObject d)
    {
        switch (d)
        {
            case null:
                return null;
            case ScrollViewer sv:
                return sv;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
        {
            var child = VisualTreeHelper.GetChild(d, i);
            var result = FindScrollViewer(child);
            if (result != null)
                return result;
        }
        return null;
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

            string leftText = (oldLine == null || oldLine.Type == ChangeType.Imaginary) ? "" : oldLine.Text;
            string rightText = (newLine == null || newLine.Type == ChangeType.Imaginary) ? "" : newLine.Text;

            leftLines.Add(leftText);
            rightLines.Add(rightText);
            meta.Add(new LineMeta(oldLine, newLine));
        }

        return new DiffAlignment(leftLines, rightLines, meta);
    }

    private void SetupGapRenderers()
    {
        if (_leftGapRenderer != null)
            AvalonLeft.TextArea.TextView.BackgroundRenderers.Remove(_leftGapRenderer);
        if (_rightGapRenderer != null)
            AvalonRight.TextArea.TextView.BackgroundRenderers.Remove(_rightGapRenderer);

        var leftGapMap = new Dictionary<int, double>();
        var rightGapMap = new Dictionary<int, double>();
        var typeface = new Typeface(AvalonLeft.FontFamily, AvalonLeft.FontStyle, AvalonLeft.FontWeight, AvalonLeft.FontStretch);

        for (int i = 0; i < _globalAlignment.Meta.Count; i++)
        {
            var meta = _globalAlignment.Meta[i];
            if (meta.Old == null || meta.Old.Type == ChangeType.Imaginary)
            {
                string reference = _globalAlignment.RightLines[i];
                leftGapMap[i] = MeasureStringWidth(reference, typeface, AvalonLeft.FontSize);
            }
            if (meta.New == null || meta.New.Type == ChangeType.Imaginary)
            {
                string reference = _globalAlignment.LeftLines[i];
                rightGapMap[i] = MeasureStringWidth(reference, typeface, AvalonRight.FontSize);
            }
        }

        _leftGapRenderer = new GapWidthBackgroundRenderer(leftGapMap);
        _rightGapRenderer = new GapWidthBackgroundRenderer(rightGapMap);

        AvalonLeft.TextArea.TextView.BackgroundRenderers.Add(_leftGapRenderer);
        AvalonRight.TextArea.TextView.BackgroundRenderers.Add(_rightGapRenderer);
    }

    private static double MeasureStringWidth(string text, Typeface typeface, double fontSize)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.Transparent,
            VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

        return formatted.WidthIncludingTrailingWhitespace;
    }

#region UI
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
        private static readonly Brush _insertBrush = new SolidColorBrush(Color.FromRgb(50, 90, 30));
        private static readonly Brush _deleteBrush = new SolidColorBrush(Color.FromArgb(140, 140, 50, 50));
        private static readonly Brush _modifyBrush = new SolidColorBrush(Color.FromRgb(110, 100, 70));
        private static readonly Brush _moveBrush = new SolidColorBrush(Color.FromRgb(70, 100, 155));
        private static readonly Brush _transparentBrush = Brushes.Transparent;

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
                ChangeType.Inserted => _insertBrush,
                ChangeType.Deleted => _deleteBrush,
                ChangeType.Modified => _modifyBrush,
                _ => _transparentBrush
            };

            if (piece.Type == ChangeType.Inserted && movedStrings.Contains(piece.Text))
            {
                baseBrush = _moveBrush;
            }

            ChangeLinePart(line.Offset, line.EndOffset, e =>
                e.TextRunProperties.SetBackgroundBrush(baseBrush));
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

                var rect = new Rect(
                    new Point(0, visualLine.VisualTop - textView.VerticalOffset),
                    new Size(width, visualLine.Height)
                );
                drawingContext.DrawRectangle(_gapBrush, null, rect);
            }
        }

        public KnownLayer Layer => KnownLayer.Background;
    }
#endregion
}
