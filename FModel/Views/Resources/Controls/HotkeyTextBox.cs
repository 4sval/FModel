using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FModel.Framework;

namespace FModel.Views.Resources.Controls
{
    /// <summary>
    /// https://tyrrrz.me/blog/hotkey-editor-control-in-wpf
    /// </summary>
    public class HotkeyTextBox : TextBox
    {
        public Hotkey HotKey
        {
            get => (Hotkey) GetValue(HotKeyProperty);
            set => SetValue(HotKeyProperty, value);
        }
        public static readonly DependencyProperty HotKeyProperty = DependencyProperty.Register("HotKey", typeof(Hotkey),
            typeof(HotkeyTextBox), new FrameworkPropertyMetadata(new Hotkey(Key.None), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, HotKeyChanged));

        private static void HotKeyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not HotkeyTextBox control) return;
            control.Text = control.HotKey.ToString();
        }

        public HotkeyTextBox()
        {
            IsReadOnly = true;
            IsReadOnlyCaretVisible = false;
            IsUndoEnabled = false;

            if (ContextMenu != null)
                ContextMenu.Visibility = Visibility.Collapsed;

            Text = HotKey.ToString();
        }

        private static bool HasKeyChar(Key key) =>
            // A - Z
            key is >= Key.A and <= Key.Z or
            // 0 - 9
            Key.D0 and <= Key.D9 or
            // Numpad 0 - 9
            Key.NumPad0 and <= Key.NumPad9 or
            // The rest
            Key.OemQuestion or Key.OemQuotes or Key.OemPlus or Key.OemOpenBrackets or Key.OemCloseBrackets or Key.OemMinus or Key.DeadCharProcessed or Key.Oem1 or Key.Oem5 or Key.Oem7 or Key.OemPeriod or Key.OemComma or Key.Add or Key.Divide or Key.Multiply or Key.Subtract or Key.Oem102 or Key.Decimal;

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            e.Handled = true;

            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            switch (key)
            {
                // If nothing was pressed - return
                case Key.None:
                    return;
                // If Alt is used as modifier - the key needs to be extracted from SystemKey
                case Key.System:
                    key = e.SystemKey;
                    break;
                // If Delete/Backspace/Escape is pressed without modifiers - clear current value and return
                case Key.Delete or Key.Back or Key.Escape when modifiers == ModifierKeys.None:
                    HotKey = new Hotkey(Key.None);
                    return;
                // If the only key pressed is one of the modifier keys - return
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LWin:
                case Key.RWin:
                case Key.Clear:
                case Key.OemClear:
                case Key.Apps:
                // If Enter/Space/Tab is pressed without modifiers - return
                case Key.Enter or Key.Space or Key.Tab when modifiers == ModifierKeys.None:
                    return;
                default:
                    // Set value
                    HotKey = new Hotkey(key, modifiers);
                    break;
            }
        }
    }
}