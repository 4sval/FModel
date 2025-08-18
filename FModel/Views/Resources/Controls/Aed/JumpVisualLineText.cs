using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using FModel.Extensions;
using FModel.Services;
using FModel.ViewModels;
using ICSharpCode.AvalonEdit.Rendering;

namespace FModel.Views.Resources.Controls;

public class JumpVisualLineText : VisualLineText
{
    public delegate void JumpOnClick(string Jump);

    public event JumpOnClick OnJumpClicked;
    private readonly string _jump;

    public JumpVisualLineText(string jump, VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
    {
        _jump = jump;
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

    private bool JumpIsClickable() => !string.IsNullOrEmpty(_jump) && Keyboard.Modifiers == ModifierKeys.None;

    protected override void OnQueryCursor(QueryCursorEventArgs e)
    {
        if (!JumpIsClickable())
            return;
        e.Handled = true;
        e.Cursor = Cursors.Hand;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || !JumpIsClickable())
            return;
        if (e.Handled || OnJumpClicked == null)
            return;

        OnJumpClicked(_jump);
        e.Handled = true;
    }

    protected override VisualLineText CreateInstance(int length)
    {
        var a = new JumpVisualLineText(_jump, ParentVisualLine, length);
        a.OnJumpClicked += jump =>
        {
            var lineNumber = a.ParentVisualLine.Document.Text.GetNameLineNumberText($"        Label_{jump}:"); // impossible for different indentation
            if (lineNumber > -1)
            {
                var line = a.ParentVisualLine.Document.GetLineByNumber(lineNumber);
                AvalonEditor.YesWeEditor.Select(line.Offset, line.Length);
                AvalonEditor.YesWeEditor.ScrollToLine(lineNumber);
            }
        };
        return a;
    }

}
