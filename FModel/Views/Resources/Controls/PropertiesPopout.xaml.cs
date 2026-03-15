using System;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CUE4Parse.Utils;
using FModel.ViewModels;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using SkiaSharp;

namespace FModel.Views.Resources.Controls;

public partial class PropertiesPopout
{
    private readonly Regex _hexColorRegex = new("\"Hex\": \"(?'target'[0-9A-Fa-f]{3,8})\"$",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private JsonFoldingStrategies _manager;

    // Cached tooltip controls — reused across hover events to avoid per-hover allocations.
    private readonly TextBlock _hoverText = new();
    private readonly Border _hoverBorder;

    public PropertiesPopout(FModel.ViewModels.TabItem contextViewModel)
    {
        InitializeComponent();

        MyAvalonEditor.Document = new TextDocument
        {
            Text = contextViewModel.Document.Text,
            FileName = contextViewModel.Entry.PathWithoutExtension
        };
        MyAvalonEditor.FontSize = contextViewModel.FontSize;
        MyAvalonEditor.SyntaxHighlighting = contextViewModel.Highlighter;
        MyAvalonEditor.ScrollToVerticalOffset(contextViewModel.ScrollPosition);
        MyAvalonEditor.TextArea.TextView.LinkTextBackgroundBrush = null;
        MyAvalonEditor.TextArea.TextView.LinkTextForegroundBrush = Brushes.Cornsilk;
        MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new GamePathElementGenerator());
        MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new JumpElementGenerator());
        MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new HexColorElementGenerator());
        _manager = new JsonFoldingStrategies(MyAvalonEditor);
        _manager.UpdateFoldings(MyAvalonEditor.Document);

        // Wire events that cannot be bound in XAML because they live on TextView, not TextEditor.
        MyAvalonEditor.TextArea.TextView.PointerHover += OnMouseHover;
        MyAvalonEditor.TextArea.TextView.PointerHoverStopped += OnMouseHoverStopped;
        MyAvalonEditor.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);

        _hoverBorder = new Border
        {
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(6, 4),
            Child = _hoverText
        };
        ToolTip.SetTip(MyAvalonEditor, _hoverBorder);
    }

    private void OnMouseHover(object? sender, PointerEventArgs e)
    {
        var pos = MyAvalonEditor.GetPositionFromPoint(e.GetPosition(MyAvalonEditor));
        if (pos == null)
            return;

        var line = MyAvalonEditor.Document.GetLineByNumber(pos.Value.Line);
        var m = _hexColorRegex.Match(MyAvalonEditor.Document.GetText(line.Offset, line.Length));
        if (!m.Success || !m.Groups.TryGetValue("target", out var g))
            return;

        var color = SKColor.Parse(g.Value);
        var bg = new SolidColorBrush(Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));
        IBrush fg = PerceivedBrightness(color) > 130 ? Brushes.Black : Brushes.White;
        _hoverBorder.Background = bg;
        _hoverBorder.BorderBrush = fg;
        _hoverText.Text = $"#{g.Value}";
        _hoverText.Foreground = fg;
        ToolTip.SetIsOpen(MyAvalonEditor, true);
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not TextEditor avalonEditor || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        var fontSize = avalonEditor.FontSize + e.Delta.Y * 2.4;

        avalonEditor.FontSize = fontSize switch
        {
            < 6 => 6,
            > 200 => 200,
            _ => fontSize
        };
        e.Handled = true; // prevent scroll-through to the document
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.J when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                _manager.UnfoldAll();
                break;
            case Key.L when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                _manager.FoldToggle(MyAvalonEditor.CaretOffset);
                break;
            case >= Key.D0 and <= Key.D9 when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                _manager.FoldToggleAtLevel(int.Parse(e.Key.ToString()[1].ToString()));
                break;
        }
    }

    private void OnMouseHoverStopped(object? sender, PointerEventArgs e)
    {
        ToolTip.SetIsOpen(MyAvalonEditor, false);
    }

    private int PerceivedBrightness(SKColor c)
    {
        return (int) Math.Sqrt(
            c.Red * c.Red * .299 +
            c.Green * c.Green * .587 +
            c.Blue * c.Blue * .114);
    }
}
