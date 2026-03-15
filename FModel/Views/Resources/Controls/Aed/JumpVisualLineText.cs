using System;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using FModel.Extensions;
using FModel.Services;
using FModel.ViewModels;
using AvaloniaEdit.Rendering;

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
        ArgumentNullException.ThrowIfNull(context);

        var runLength = DocumentLength - (startVisualColumn - VisualColumn);
        if (runLength != 2) // skip separator tokens
            TextRunProperties.SetForegroundBrush(Brushes.Plum);
        else
            TextRunProperties.SetForegroundBrush(null); // restore default for separator token

        return base.CreateTextRun(startVisualColumn, context);
    }

    private bool JumpIsClickable(KeyModifiers modifiers) =>
        !string.IsNullOrEmpty(_jump) && modifiers == KeyModifiers.None;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed || !JumpIsClickable(e.KeyModifiers))
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
