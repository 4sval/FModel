using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;
using CUE4Parse.Utils;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.ViewModels;
using ICSharpCode.AvalonEdit;
using SkiaSharp;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// Logique d'interaction pour AvalonEditor.xaml
/// </summary>
public partial class AvalonEditor
{
    public static TextEditor YesWeEditor;
    private readonly Regex _hexColorRegex = new("\"Hex\": \"(?'target'[0-9A-Fa-f]{3,8})\"$",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly System.Windows.Controls.ToolTip _toolTip = new();
    private readonly Dictionary<string, NavigationList<int>> _savedCarets = new();
    private NavigationList<int> _caretsOffsets
    {
        get => MyAvalonEditor.Document != null
            ? _savedCarets.GetOrAdd(MyAvalonEditor.Document.FileName, () => new NavigationList<int>())
            : new NavigationList<int>();
    }
    private bool _ignoreCaret = true;

    public AvalonEditor()
    {
        InitializeComponent();

        YesWeEditor = MyAvalonEditor;
        MyAvalonEditor.TextArea.TextView.LinkTextBackgroundBrush = null;
        MyAvalonEditor.TextArea.TextView.LinkTextForegroundBrush = Brushes.Cornsilk;
        MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new GamePathElementGenerator());
        MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new HexColorElementGenerator());

        ApplicationService.ApplicationView.CUE4Parse.TabControl.OnTabRemove += OnTabClose;
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

    private void OnMouseHoverStopped(object sender, MouseEventArgs e)
    {
        _toolTip.IsOpen = false;
    }

    private static int PerceivedBrightness(SKColor c)
    {
        return (int) Math.Sqrt(
            c.Red * c.Red * .299 +
            c.Green * c.Green * .587 +
            c.Blue * c.Blue * .114);
    }

    private void OnTextChanged(object sender, EventArgs e)
    {
        if (sender is not TextEditor avalonEditor || DataContext is not TabItem tabItem ||
            avalonEditor.Document == null || string.IsNullOrEmpty(avalonEditor.Document.Text))
            return;
        avalonEditor.Document.FileName = tabItem.Entry.PathWithoutExtension;

        if (!_savedCarets.ContainsKey(avalonEditor.Document.FileName))
            _ignoreCaret = true;

        if (!tabItem.ShouldScroll) return;

        var lineNumber = avalonEditor.Document.Text.GetNameLineNumber(tabItem.ScrollTrigger);
        if (lineNumber == -1) lineNumber = 1;

        var line = avalonEditor.Document.GetLineByNumber(lineNumber);
        avalonEditor.Select(line.Offset, line.Length);
        avalonEditor.ScrollToLine(lineNumber);
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not TabItem tabItem || Keyboard.Modifiers != ModifierKeys.Control)
            return;

        var fontSize = tabItem.FontSize + e.Delta / 50.0;
        tabItem.FontSize = fontSize switch
        {
            < 6 => 6,
            > 200 => 200,
            _ => fontSize
        };
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

    private void OnMouseRelease(object sender, MouseButtonEventArgs e)
    {
        SaveCaretLoc(MyAvalonEditor.CaretOffset);
    }
}
