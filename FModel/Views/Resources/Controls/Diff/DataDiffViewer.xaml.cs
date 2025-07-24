using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FModel.Extensions;

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
        var linkBrush = Brushes.Cornsilk;
        AvalonLeft.TextArea.TextView.LinkTextForegroundBrush = linkBrush;
        AvalonRight.TextArea.TextView.LinkTextForegroundBrush = linkBrush;
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
        if (_isLoading || _loadedChunkIndex >= Math.Max(_leftChunks.Count, _rightChunks.Count))
            return;

        _isLoading = true;

        int chunksToLoad = Math.Min(ChunksPerLoad, Math.Max(_leftChunks.Count, _rightChunks.Count) - _loadedChunkIndex);

        var (tempAlignment, moved, leftText, rightText) = await Task.Run(() =>
        {
            var tempAlignment = new DiffAlignment([], [], []);
            var moved = new HashSet<string>();

            var leftBuilder = new StringBuilder();
            var rightBuilder = new StringBuilder();

            for (int i = 0; i < chunksToLoad; i++)
            {
                string leftChunk = _loadedChunkIndex + i < _leftChunks.Count ? _leftChunks[_loadedChunkIndex + i] : "";
                string rightChunk = _loadedChunkIndex + i < _rightChunks.Count ? _rightChunks[_loadedChunkIndex + i] : "";

                var builder = new SideBySideDiffBuilder();
                var model = builder.BuildDiffModel(leftChunk, rightChunk);
                var alignment = AlignLinesWithGaps(model);

                tempAlignment.LeftLines.AddRange(alignment.LeftLines);
                tempAlignment.RightLines.AddRange(alignment.RightLines);
                tempAlignment.Meta.AddRange(alignment.Meta);

                foreach (var m in alignment.Meta
                             .Where(m => m.Old != null && m.New != null && m.Old.Text == m.New.Text)
                             .Select(m => m.New.Text))
                {
                    moved.Add(m);
                }

                leftBuilder.AppendJoin("\n", alignment.LeftLines).Append('\n');
                rightBuilder.AppendJoin("\n", alignment.RightLines).Append('\n');
            }

            return (tempAlignment, moved, leftBuilder.ToString(), rightBuilder.ToString());
        });

        AvalonLeft.Document.BeginUpdate();
        AvalonRight.Document.BeginUpdate();

        if (_loadedChunkIndex == 0)
        {
            AvalonLeft.Document.Text = leftText;
            AvalonRight.Document.Text = rightText;
        }
        else
        {
            AvalonLeft.Document.Insert(AvalonLeft.Document.TextLength, leftText);
            AvalonRight.Document.Insert(AvalonRight.Document.TextLength, rightText);
        }

        AvalonLeft.Document.EndUpdate();
        AvalonRight.Document.EndUpdate();

        _globalAlignment.LeftLines.AddRange(tempAlignment.LeftLines);
        _globalAlignment.RightLines.AddRange(tempAlignment.RightLines);
        _globalAlignment.Meta.AddRange(tempAlignment.Meta);
        _globalMovedStrings.UnionWith(moved);

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

        _loadedChunkIndex += chunksToLoad;
        _isLoading = false;
    }

    // Only reference to one scroll is needed because they are aligned
    private void DataDiffViewer_Loaded(object sender, RoutedEventArgs e)
    {
        _scroll = FindScrollViewer(AvalonLeft);

        if (_scroll == null)
            return;

        _scroll.ScrollChanged += Scroll_ScrollChanged;

        DiffNavbar.Attach(_scroll, _globalAlignment.Meta, _globalMovedStrings);
        DiffNavbar.LineClicked += line =>
        {
            _scroll.ScrollToVerticalOffset(line * _scroll.ExtentHeight / DiffNavbar.TotalLines);
        };
    }

    private void Scroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_isLoading || _loadedChunkIndex >= Math.Max(_leftChunks.Count, _rightChunks.Count))
        {
            LoadMorePanel.Visibility = Visibility.Collapsed;
            return;
        }

        if (sender is ScrollViewer sv && IsNearBottom(sv))
        {
            LoadMorePanel.Visibility = Visibility.Visible;
        }
        else
        {
            LoadMorePanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void LoadMoreButton_Click(object sender, RoutedEventArgs e)
    {
        LoadMoreButton.IsEnabled = false;
        LoadingOverlay.Visibility = Visibility.Visible;

        try
        {
            await Task.Delay(1500); // Very slight delay for loading indicator
            await LoadMoreChunksAsync();
            DiffNavbar.UpdateNavbar(_globalAlignment.Meta, _globalMovedStrings, true);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            LoadMoreButton.IsEnabled = true;
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
}
