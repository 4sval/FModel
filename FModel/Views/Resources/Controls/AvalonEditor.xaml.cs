using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FModel.Extensions;
using FModel.Framework;
using FModel.ViewModels;
using ICSharpCode.AvalonEdit;
using SkiaSharp;

namespace FModel.Views.Resources.Controls
{
    /// <summary>
    /// Logique d'interaction pour AvalonEditor.xaml
    /// </summary>
    public partial class AvalonEditor
    {
        public static TextEditor YesWeEditor;
        public static System.Windows.Controls.TextBox YesWeSearch;
        private readonly Regex _hexColorRegex = new("\"Hex\": \"(?'target'[0-9A-Fa-f]{3,8})\"$",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private readonly System.Windows.Controls.ToolTip _toolTip = new();
        private readonly NavigationList<int> _caretList = new();
        private bool _ignoreCaret = true;

        public AvalonEditor()
        {
            CommandBindings.Add(new CommandBinding(NavigationCommands.Search, (_, e) => FindNext(e.Parameter != null)));
            InitializeComponent();

            YesWeEditor = MyAvalonEditor;
            YesWeSearch = WpfSuckMyDick;
            MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new GamePathElementGenerator());
            MyAvalonEditor.TextArea.TextView.ElementGenerators.Add(new HexColorElementGenerator());
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    ((TabItem) DataContext).HasSearchOpen = false;
                    break;
                case Key.Enter when !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && ((TabItem) DataContext).HasSearchOpen:
                    FindNext();
                    break;
                case Key.Enter when Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && ((TabItem) DataContext).HasSearchOpen:
                    var dc = (TabItem)DataContext;
                    var old = dc.SearchUp;
                    dc.SearchUp = true;
                    FindNext();
                    dc.SearchUp = old;
                    break;
                case Key.System: // Alt
                    if (Keyboard.IsKeyDown(Key.Left))
                    {
                        if (_caretList.Count == 0)
                            return;
                        var movep = _caretList.MovePrevious;
                        MyAvalonEditor.CaretOffset = movep;
                        MyAvalonEditor.TextArea.Caret.BringCaretToView();
                    } else if ((Keyboard.IsKeyDown(Key.Right)))
                    {
                        if (_caretList.Count == 0)
                            return;
                        var move = _caretList.MoveNext;
                        MyAvalonEditor.CaretOffset = move;
                        MyAvalonEditor.TextArea.Caret.BringCaretToView();
                    }
                    break;
            }
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

        private int PerceivedBrightness(SKColor c)
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
            _caretList.Clear();
            _ignoreCaret = true;
            avalonEditor.Document.FileName = tabItem.Directory + '/' + tabItem.Header.SubstringBeforeLast('.');
            if (!tabItem.ShouldScroll) return;

            var lineNumber = avalonEditor.Document.Text.GetLineNumber(tabItem.ScrollTrigger);
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

        private void OnDeleteSearchClick(object sender, RoutedEventArgs e)
        {
            ((TabItem) DataContext).TextToFind = string.Empty;
        }

        private void FindNext(bool invertLeftRight = false)
        {
            var viewModel = (TabItem) DataContext;
            if (viewModel.Document == null || string.IsNullOrEmpty(viewModel.TextToFind))
                return;

            Regex r;
            if (invertLeftRight)
            {
                viewModel.SearchUp = !viewModel.SearchUp;
                r = GetRegEx();
                viewModel.SearchUp = !viewModel.SearchUp;
            }
            else r = GetRegEx();

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
                    if (!m.Success) continue;
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
            var viewModel = (TabItem) DataContext;

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

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            ((TabItem) DataContext).HasSearchOpen = false;
        }

        private void SaveCaretLoc(int offset)
        {
            if (_ignoreCaret) { _ignoreCaret = false; return;} // first always point to the end of the file for some reason
            if (_caretList.Count >= 10)
                _caretList.RemoveAt(0);
            if (!_caretList.Contains(offset))
            {
                _caretList.Add(offset);
                _caretList.CurrentIndex = _caretList.Count-1;
            }
        }

        private void OnMouseRelease(object sender, MouseButtonEventArgs e)
        {
            SaveCaretLoc(MyAvalonEditor.CaretOffset);
        }
    }
}
