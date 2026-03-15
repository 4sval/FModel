using System;
using System.Text.RegularExpressions;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using CUE4Parse.Utils;
using FModel.Extensions;
using FModel.Services;
using FModel.ViewModels;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace FModel.Views.Resources.Controls;

public class GamePathVisualLineText : VisualLineText
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public delegate void GamePathOnClick(string gamePath, string parentExportType);

    public event GamePathOnClick OnGamePathClicked;
    private readonly string _gamePath;
    private readonly string _parentExportType;

    public GamePathVisualLineText(string gamePath, string parentExportType, VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
    {
        _gamePath = gamePath;
        _parentExportType = parentExportType;
    }

    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var runLength = DocumentLength - (startVisualColumn - VisualColumn);
        if (runLength != 2) // skip separator tokens
            TextRunProperties.SetForegroundBrush(Brushes.Plum);
        else
            TextRunProperties.SetForegroundBrush(null); // restore default for separator token

        return base.CreateTextRun(startVisualColumn, context);
    }

    private bool GamePathIsClickable(KeyModifiers modifiers) =>
        !string.IsNullOrEmpty(_gamePath) && modifiers == KeyModifiers.None;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed || !GamePathIsClickable(e.KeyModifiers))
            return;
        if (e.Handled || OnGamePathClicked == null)
            return;

        OnGamePathClicked(_gamePath, _parentExportType);
        e.Handled = true;
    }

    protected override VisualLineText CreateInstance(int length)
    {
        var a = new GamePathVisualLineText(_gamePath, _parentExportType, ParentVisualLine, length);
        a.OnGamePathClicked += async (gamePath, parentExportType) =>
        {
            var obj = gamePath.SubstringAfterLast('.');
            var package = gamePath.SubstringBeforeLast('.');
            var fullPath = _applicationView.CUE4Parse.Provider.FixPath(package);

            var firstLine = a.ParentVisualLine.Document.GetLineByNumber(2);
            if (a.ParentVisualLine.Document.FileName.Equals(fullPath.SubstringBeforeLast('.'), StringComparison.OrdinalIgnoreCase) &&
                !a.ParentVisualLine.Document.GetText(firstLine.Offset, firstLine.Length).Equals("  \"Summary\": {")) // Show Metadata case
            {
                var lineNumber = a.ParentVisualLine.Document.Text.GetNameLineNumber(obj);
                if (lineNumber > -1)
                {
                    var line = a.ParentVisualLine.Document.GetLineByNumber(lineNumber);
                    AvalonEditor.YesWeEditor.Select(line.Offset, line.Length);
                    AvalonEditor.YesWeEditor.ScrollToLine(lineNumber);
                    return;
                }
            }

            await _threadWorkerView.Begin(cancellationToken =>
                _applicationView.CUE4Parse.ExtractAndScroll(cancellationToken, fullPath, obj, parentExportType));
        };
        return a;
    }
}
