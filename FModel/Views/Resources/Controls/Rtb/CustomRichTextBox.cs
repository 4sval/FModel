using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace FModel.Views.Resources.Controls;

public enum ELog
{
    Information,
    Warning,
    Error,
    Debug,
    None
}

/// <summary>
/// Provides coloured structured logging to the on-screen log panel.
/// <c>Append</c> overloads are thread-safe and marshal work to <see cref="Dispatcher.UIThread"/>.
/// <c>Text</c>, <c>Link</c>, and <c>ClearLogs</c> require the UI thread and must only be
/// called from within an <c>Append</c> lambda.
/// Replaces the WPF FlowDocument / RichTextBox implementation.
/// </summary>
public static class FLogger
{
    public static CustomRichTextBox Logger;

    private const string _at    = "   at ";
    private const char   _dot   = '.';
    private const char   _colon = ':';
    private const string _gray  = "#999999";

    public static void Append(ELog type, Action job)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            switch (type)
            {
                case ELog.Information: Text("[INF] ", Constants.BLUE);   break;
                case ELog.Warning:     Text("[WRN] ", Constants.YELLOW); break;
                case ELog.Error:       Text("[ERR] ", Constants.RED);    break;
                case ELog.Debug:       Text("[DBG] ", Constants.GREEN);  break;
            }
            job();
            Logger?.ScrollToEnd();
        }, DispatcherPriority.Background);
    }

    public static void Append(Exception e)
    {
        Append(ELog.Error, () =>
        {
            if ((e.InnerException ?? e) is { TargetSite.DeclaringType: not null } exception)
            {
                if (exception.TargetSite.ToString() == "CUE4Parse.FileProvider.GameFile get_Item(System.String)")
                {
                    Text(e.Message, Constants.WHITE, true);
                }
                else
                {
                    var t = exception.GetType();
                    Text(t.Namespace + _dot, Constants.GRAY);
                    Text(t.Name, Constants.WHITE);
                    Text(_colon + " ", Constants.GRAY);
                    Text(exception.Message, Constants.RED, true);

                    Text(_at, _gray);
                    Text(exception.TargetSite.DeclaringType.FullName + _dot, Constants.GRAY);
                    Text(exception.TargetSite.Name, Constants.YELLOW);

                    var p = exception.TargetSite.GetParameters();
                    var parameters = new string[p.Length];
                    for (int i = 0; i < parameters.Length; i++)
                        parameters[i] = p[i].ParameterType.Name + " " + p[i].Name;

                    Text("(" + string.Join(", ", parameters) + ")", Constants.GRAY, true);
                }
            }
            else
            {
                Text(e.Message, Constants.WHITE, true);
            }
        });
    }

    public static void Text(string message, string color, bool newLine = false)
    {
        Logger?.AppendText(message, color, newLine);
    }

    public static void Link(string message, string url, bool newLine = false)
    {
        Logger?.AppendLink(message, url, newLine);
    }

    public static void ClearLogs() => Logger?.ClearLog();
}

// ---------------------------------------------------------------------------
// Internal rendering support
// ---------------------------------------------------------------------------

/// <summary>A segment of coloured (or clickable) text in the log document.</summary>
internal sealed record LogSegment(int Offset, int Length, IBrush Brush, bool IsLink = false, string? Url = null);

/// <summary>Applies per-segment foreground colours during line rendering.</summary>
internal sealed class LogColorizer : DocumentColorizingTransformer
{
    private readonly List<LogSegment> _segments;

    public LogColorizer(List<LogSegment> segments) => _segments = segments;

    protected override void ColorizeLine(DocumentLine line)
    {
        var lineStart = line.Offset;
        var lineEnd   = lineStart + line.Length;

        // Segments are always appended in document order (insert at TextLength), so the list is
        // sorted by Offset.  Binary-search for the first segment whose end passes lineStart,
        // skipping all segments that lie entirely above the current line.  This keeps
        // ColorizeLine O(log n + k) instead of O(n) as the log grows.
        int lo = 0, hi = _segments.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (_segments[mid].Offset + _segments[mid].Length <= lineStart)
                lo = mid + 1;
            else
                hi = mid;
        }

        for (int i = lo; i < _segments.Count; i++)
        {
            var seg = _segments[i];
            if (seg.Offset >= lineEnd)
                break;

            var start = Math.Max(seg.Offset, lineStart);
            var end   = Math.Min(seg.Offset + seg.Length, lineEnd);
            ChangeLinePart(start, end, el => el.TextRunProperties.SetForegroundBrush(seg.Brush));
        }
    }
}

// ---------------------------------------------------------------------------
// Public control
// ---------------------------------------------------------------------------

/// <summary>
/// Log panel control backed by <see cref="AvaloniaEdit.TextEditor"/>.
/// Coloured text is appended via <see cref="AppendText"/> / <see cref="AppendLink"/> and
/// highlighted at render-time by <see cref="LogColorizer"/>.
/// </summary>
public class CustomRichTextBox : TextEditor
{
    private readonly List<LogSegment> _segments = [];

    // Off-white (Cornsilk) distinguishes link text from regular log text.
    // WPF Hyperlink system-blue styling is unavailable in AvaloniaEdit.
    private static readonly IBrush _linkBrush = new SolidColorBrush(Colors.Cornsilk);
    private static readonly Dictionary<string, IBrush> _brushCache = [];

    public CustomRichTextBox()
    {
        IsReadOnly = true;
        WordWrap   = false;
        ShowLineNumbers = false;
        Options.EnableHyperlinks      = false;
        Options.EnableEmailHyperlinks = false;
        Document.UndoStack.SizeLimit  = 0;
        TextArea.TextView.LineTransformers.Add(new LogColorizer(_segments));
        TextArea.PointerPressed += OnPointerPressed;
    }

    /// <summary>Appends a coloured text run.</summary>
    public void AppendText(string message, string color, bool newLine)
    {
        Dispatcher.UIThread.VerifyAccess();
        var offset = Document.TextLength;
        Document.Insert(offset, newLine ? message + "\n" : message);
        _segments.Add(new LogSegment(offset, message.Length, GetBrush(color)));
    }

    /// <summary>
    /// Appends a clickable link run.
    /// OS file-manager invocation is deferred to TODO(P4-001).
    /// </summary>
    public void AppendLink(string message, string url, bool newLine)
    {
        Dispatcher.UIThread.VerifyAccess();
        var offset = Document.TextLength;
        Document.Insert(offset, newLine ? message + "\n" : message);
        _segments.Add(new LogSegment(offset, message.Length, _linkBrush, IsLink: true, Url: url));
    }

    /// <summary>Scrolls to the last line.</summary>
    public new void ScrollToEnd()
    {
        if (Document.TextLength > 0)
            ScrollTo(Document.LineCount, 0);
    }

    /// <summary>Clears all log text and colour segments.</summary>
    public void ClearLog()
    {
        Dispatcher.UIThread.VerifyAccess();
        _segments.Clear();
        Document.Text = string.Empty;
    }

    private static IBrush GetBrush(string color)
    {
        if (_brushCache.TryGetValue(color, out var cached))
            return cached;

        IBrush brush;
        try   { brush = new SolidColorBrush(Color.Parse(color)); }
        catch { brush = Brushes.White; }

        return _brushCache[color] = brush;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pos    = e.GetPosition(TextArea.TextView);
        var docPos = TextArea.TextView.GetPositionFloor(pos);
        if (docPos == null) return;

        var offset = Document.GetOffset(docPos.Value.Location);
        var link   = _segments.Find(s => s.IsLink && offset >= s.Offset && offset < s.Offset + s.Length);
        if (link == null) return;

        // TODO(P4-001): open link.Url in the OS file manager.
        // Note: e.Handled is intentionally NOT set until the link-open behaviour is implemented;
        // marking events handled while performing no action breaks normal text selection.
    }
}
