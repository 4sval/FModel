using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CUE4Parse.Utils;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.ViewModels;
using AvaloniaEdit;
using SkiaSharp;
using VmTabItem = FModel.ViewModels.TabItem;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// Logique d'interaction pour AvalonEditor.xaml
/// </summary>
public partial class AvalonEditor
{
    public static TextEditor YesWeEditor;
    public static TextBox YesWeSearch;
    private readonly Regex _hexColorRegex = new("\"Hex\": \"(?'target'[0-9A-Fa-f]{3,8})\"$",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly Dictionary<string, NavigationList<int>> _savedCarets = new();
    // Cached tooltip controls — reused across hover events to avoid per-hover allocations.
    private readonly TextBlock _hoverText = new();
    private readonly Border _hoverBorder;
    private NavigationList<int> _caretsOffsets
    {
        get => MyAvalonEditor.Document != null && MyAvalonEditor.Document.FileName != null
            ? _savedCarets.GetOrAdd(MyAvalonEditor.Document.FileName, () => new NavigationList<int>())
            : new NavigationList<int>();
    }
    private bool _ignoreCaret = true;

    public AvalonEditor()
    {
        InitializeComponent();

        YesWeEditor = MyAvalonEditor;
        YesWeSearch = WpfSuckMyDick;
        MyAvalonEditor.TextArea.TextView.LinkTextBackgroundBrush = null;
        MyAvalonEditor.TextArea.TextView.LinkTextForegroundBrush = Brushes.Cornsilk;
        MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new GamePathElementGenerator());
        MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new JumpElementGenerator());
        MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new HexColorElementGenerator());

        // Events that were XAML-bound in WPF are wired here to use Avalonia's event model.
        MyAvalonEditor.TextChanged += OnTextChanged;
        MyAvalonEditor.TextArea.TextView.PointerHover += OnMouseHover;
        MyAvalonEditor.TextArea.TextView.PointerHoverStopped += OnMouseHoverStopped;
        MyAvalonEditor.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        MyAvalonEditor.PointerReleased += OnPointerReleased;

        _hoverBorder = new Border
        {
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(6, 4),
            Child = _hoverText
        };
        ToolTip.SetTip(MyAvalonEditor, _hoverBorder);

        ApplicationService.ApplicationView.CUE4Parse.TabControl.OnTabRemove += OnTabClose;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                ((VmTabItem) DataContext).HasSearchOpen = false;
                break;
            case Key.Enter when !e.KeyModifiers.HasFlag(KeyModifiers.Shift) && ((VmTabItem) DataContext).HasSearchOpen:
                FindNext();
                break;
            case Key.Enter when e.KeyModifiers.HasFlag(KeyModifiers.Shift) && ((VmTabItem) DataContext).HasSearchOpen:
                var dc = (VmTabItem) DataContext;
                var old = dc.SearchUp;
                dc.SearchUp = true;
                FindNext();
                dc.SearchUp = old;
                break;
            // Alt+Left / Alt+Right — navigate backward/forward through saved caret positions.
            case Key.Left when e.KeyModifiers.HasFlag(KeyModifiers.Alt):
                if (_caretsOffsets.Count == 0)
                    return;
                MyAvalonEditor.CaretOffset = _caretsOffsets.MovePrevious;
                MyAvalonEditor.TextArea.Caret.BringCaretToView();
                break;
            case Key.Right when e.KeyModifiers.HasFlag(KeyModifiers.Alt):
                if (_caretsOffsets.Count == 0)
                    return;
                MyAvalonEditor.CaretOffset = _caretsOffsets.MoveNext;
                MyAvalonEditor.TextArea.Caret.BringCaretToView();
                break;
        }
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

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (sender is not TextEditor avalonEditor || DataContext is not VmTabItem tabItem ||
            avalonEditor.Document == null || string.IsNullOrEmpty(avalonEditor.Document.Text))
            return;
        avalonEditor.Document.FileName = tabItem.Entry.PathWithoutExtension;

        if (!_savedCarets.ContainsKey(avalonEditor.Document.FileName))
            _ignoreCaret = true;

        if (!tabItem.ShouldScroll)
            return;

        var lineNumber = avalonEditor.Document.Text.GetNameLineNumber(tabItem.ScrollTrigger);
        if (lineNumber == -1)
            lineNumber = 1;

        var line = avalonEditor.Document.GetLineByNumber(lineNumber);
        avalonEditor.Select(line.Offset, line.Length);
        avalonEditor.ScrollToLine(lineNumber);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not VmTabItem tabItem || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        var fontSize = tabItem.FontSize + e.Delta.Y * 2.4;
        tabItem.FontSize = fontSize switch
        {
            < 6 => 6,
            > 200 => 200,
            _ => fontSize
        };
        e.Handled = true; // prevent scroll-through to the document
    }

    private void OnDeleteSearchClick(object? sender, RoutedEventArgs e)
    {
        ((VmTabItem) DataContext).TextToFind = string.Empty;
    }

    private void FindNext(bool invertLeftRight = false)
    {
        var viewModel = (VmTabItem) DataContext;
        if (viewModel.Document == null || string.IsNullOrEmpty(viewModel.TextToFind))
            return;

        Regex r;
        if (invertLeftRight)
        {
            viewModel.SearchUp = !viewModel.SearchUp;
            r = GetRegEx();
            viewModel.SearchUp = !viewModel.SearchUp;
        }
        else
            r = GetRegEx();

        var rightToLeft = r.Options.HasFlag(RegexOptions.RightToLeft);
        var m = r.Match(MyAvalonEditor.Text, rightToLeft ? MyAvalonEditor.SelectionStart : MyAvalonEditor.SelectionStart + MyAvalonEditor.SelectionLength);
        if (m.Success)
        {
            MyAvalonEditor.Select(m.Index, m.Length);
            MyAvalonEditor.TextArea.Caret.BringCaretToView();
        }
        else
        {
            // we have reached the end of the document
            // start again from the beginning/end,
            var oldEditor = MyAvalonEditor;
            do
            {
                m = rightToLeft ? r.Match(MyAvalonEditor.Text, MyAvalonEditor.Text.Length - 1) : r.Match(MyAvalonEditor.Text, 0);
                if (!m.Success)
                    continue;
                MyAvalonEditor.Select(m.Index, m.Length);
                MyAvalonEditor.TextArea.Caret.BringCaretToView();
                break;
            } while (MyAvalonEditor != oldEditor);
        }
    }

    private Regex GetRegEx(bool forceLeftToRight = false)
    {
        Regex r;
        var o = RegexOptions.None;
        var viewModel = (VmTabItem) DataContext;

        if (viewModel.SearchUp && !forceLeftToRight)
            o |= RegexOptions.RightToLeft;
        if (!viewModel.CaseSensitive)
            o |= RegexOptions.IgnoreCase;

        if (viewModel.UseRegEx)
        {
            r = new Regex(viewModel.TextToFind, o);
        }
        else
        {
            var s = Regex.Escape(viewModel.TextToFind);
            if (viewModel.WholeWord)
                s = "\\W" + s + "\\W";

            r = new Regex(s, o);
        }

        return r;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        ((VmTabItem) DataContext).HasSearchOpen = false;
    }

    private void OnTabClose(object sender, EventArgs eventArgs)
    {
        if (eventArgs is not TabControlViewModel.TabEventArgs e || e.TabToRemove.Document?.FileName is not { } fileName)
            return;

        if (_savedCarets.ContainsKey(fileName))
            _savedCarets.Remove(fileName);
    }

    private void SaveCaretLoc(int offset)
    {
        if (_ignoreCaret)
        {
            _ignoreCaret = false;
            return;
        } // first always point to the end of the file for some reason

        if (_caretsOffsets.Count >= 10)
            _caretsOffsets.RemoveAt(0);
        if (!_caretsOffsets.Contains(offset))
        {
            _caretsOffsets.Add(offset);
            _caretsOffsets.CurrentIndex = _caretsOffsets.Count - 1;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SaveCaretLoc(MyAvalonEditor.CaretOffset);
    }
}
