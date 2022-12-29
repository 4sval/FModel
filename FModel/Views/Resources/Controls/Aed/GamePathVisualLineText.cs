using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using FModel.Extensions;
using FModel.Services;
using FModel.ViewModels;
using ICSharpCode.AvalonEdit.Rendering;

namespace FModel.Views.Resources.Controls;

public class GamePathVisualLineText : VisualLineText
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public delegate void GamePathOnClick(string gamePath);

    public event GamePathOnClick OnGamePathClicked;
    private readonly string _gamePath;

    public GamePathVisualLineText(string gamePath, VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
    {
        _gamePath = gamePath;
    }

    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var relativeOffset = startVisualColumn - VisualColumn;
        var text = context.GetText(context.VisualLine.FirstDocumentLine.Offset + RelativeTextOffset + relativeOffset, DocumentLength - relativeOffset);

        if (text.Count != 2) // ": "
            TextRunProperties.SetForegroundBrush(Brushes.Plum);

        return new TextCharacters(text.Text, text.Offset, text.Count, TextRunProperties);
    }

    private bool GamePathIsClickable() => !string.IsNullOrEmpty(_gamePath) && Keyboard.Modifiers == ModifierKeys.None;

    protected override void OnQueryCursor(QueryCursorEventArgs e)
    {
        if (!GamePathIsClickable()) return;
        e.Handled = true;
        e.Cursor = Cursors.Hand;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || !GamePathIsClickable())
            return;
        if (e.Handled || OnGamePathClicked == null)
            return;

        OnGamePathClicked(_gamePath);
        e.Handled = true;
    }

    protected override VisualLineText CreateInstance(int length)
    {
        var a = new GamePathVisualLineText(_gamePath, ParentVisualLine, length);
        a.OnGamePathClicked += async gamePath =>
        {
            var obj = gamePath.SubstringAfterLast('.');
            var package = gamePath.SubstringBeforeLast('.');
            var fullPath = _applicationView.CUE4Parse.Provider.FixPath(package, StringComparison.Ordinal);
            if (a.ParentVisualLine.Document.FileName.Equals(fullPath.SubstringBeforeLast('.'), StringComparison.OrdinalIgnoreCase))
            {
                var lineNumber = a.ParentVisualLine.Document.Text.GetLineNumber(obj);
                var line = a.ParentVisualLine.Document.GetLineByNumber(lineNumber);
                AvalonEditor.YesWeEditor.Select(line.Offset, line.Length);
                AvalonEditor.YesWeEditor.ScrollToLine(lineNumber);
            }
            else
            {
                await _threadWorkerView.Begin(cancellationToken =>
                    _applicationView.CUE4Parse.ExtractAndScroll(cancellationToken, fullPath, obj));
            }
        };
        return a;
    }
}
