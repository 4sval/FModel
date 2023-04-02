using System;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;
using FModel.Extensions;
using FModel.ViewModels;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using SkiaSharp;

namespace FModel.Views.Resources.Controls;

public partial class PropertiesPopout
{
    private readonly Regex _hexColorRegex = new("\"Hex\": \"(?'target'[0-9A-Fa-f]{3,8})\"$",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly System.Windows.Controls.ToolTip _toolTip = new();
    private JsonFoldingStrategies _manager;

    public PropertiesPopout(TabItem contextViewModel)
    {
        InitializeComponent();

        MyAvalonEditor.Document = new TextDocument
        {
            Text = contextViewModel.Document.Text,
            FileName = contextViewModel.Directory + '/' + contextViewModel.Header.SubstringBeforeLast('.')
        };
        MyAvalonEditor.FontSize = contextViewModel.FontSize;
        MyAvalonEditor.SyntaxHighlighting = contextViewModel.Highlighter;
        MyAvalonEditor.ScrollToVerticalOffset(contextViewModel.ScrollPosition);
        MyAvalonEditor.TextArea.TextView.LinkTextBackgroundBrush = null;
        MyAvalonEditor.TextArea.TextView.LinkTextForegroundBrush = Brushes.Cornsilk;
        MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new GamePathElementGenerator());
        MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new HexColorElementGenerator());
        _manager = new JsonFoldingStrategies(MyAvalonEditor);
        _manager.UpdateFoldings(MyAvalonEditor.Document);
    }

    private void OnMouseHover(object sender, MouseEventArgs e)
    {
        var pos = MyAvalonEditor.GetPositionFromPoint(e.GetPosition(MyAvalonEditor));
        if (pos == null) return;

        var line = MyAvalonEditor.Document.GetLineByNumber(pos.Value.Line);
        var m = _hexColorRegex.Match(MyAvalonEditor.Document.GetText(line.Offset, line.Length));
        if (!m.Success || !m.Groups.TryGetValue("target", out var g)) return;

        var color = SKColor.Parse(g.Value);
        _toolTip.PlacementTarget = this; // required for property inheritance
        _toolTip.Background = new SolidColorBrush(Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));
        _toolTip.Foreground = _toolTip.BorderBrush = PerceivedBrightness(color) > 130 ? Brushes.Black : Brushes.White;
        _toolTip.Content = $"#{g.Value}";
        _toolTip.IsOpen = true;
        e.Handled = true;
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not TextEditor avalonEditor || Keyboard.Modifiers != ModifierKeys.Control)
            return;

        var fontSize = avalonEditor.FontSize + e.Delta / 50.0;

        avalonEditor.FontSize = fontSize switch
        {
            < 6 => 6,
            > 200 => 200,
            _ => fontSize
        };
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.J when Keyboard.IsKeyDown(Key.K) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                _manager.UnfoldAll();
                break;
            case Key.L when Keyboard.IsKeyDown(Key.K) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                _manager.FoldToggle(MyAvalonEditor.CaretOffset);
                break;
            case >= Key.D0 and <= Key.D9 when Keyboard.IsKeyDown(Key.K) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                _manager.FoldToggleAtLevel(int.Parse(e.Key.ToString()[1].ToString()));
                break;
        }
    }

    private void OnMouseHoverStopped(object sender, MouseEventArgs e)
    {
        _toolTip.IsOpen = false;
    }

    private int PerceivedBrightness(SKColor c)
    {
        return (int) Math.Sqrt(
            c.Red * c.Red * .299 +
            c.Green * c.Green * .587 +
            c.Blue * c.Blue * .114);
    }
}
