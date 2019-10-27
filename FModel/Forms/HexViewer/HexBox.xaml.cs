//////////////////////////////////////////////
// Apache 2.0  - 2017-2019
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Bytes;

namespace WpfHexaEditor
{
    /// <summary>
    /// Control for enter hex value and deal with.
    /// </summary>
    public partial class HexBox
    {
        public HexBox() => InitializeComponent();

        #region Events
        /// <summary>
        /// Occurs when value are changed.
        /// </summary>
        public event EventHandler ValueChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Get hexadecimal value of LongValue
        /// </summary>
        public string HexValue => ByteConverters.LongToHex(LongValue);

        /// <summary>
        /// Set maximum value
        /// </summary>
        public long MaximumValue
        {
            get => (long)GetValue(MaximumValueProperty);
            set => SetValue(MaximumValueProperty, value);
        }

        // Using a DependencyProperty as the backing store for MaximumValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumValueProperty =
            DependencyProperty.Register(nameof(MaximumValue), typeof(long), typeof(HexBox),
                new FrameworkPropertyMetadata(long.MaxValue, MaximumValue_Changed));

        private static void MaximumValue_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexBox ctrl)
                if (e.NewValue != e.OldValue)
                    if (ctrl.LongValue > (long)e.NewValue)
                        ctrl.UpdateValueFrom((long)e.NewValue);
        }

        /// <summary>
        /// Get or set the hex value show in control
        /// </summary>
        public long LongValue
        {
            get => (long)GetValue(LongValueProperty);
            set => SetValue(LongValueProperty, value);
        }

        // Using a DependencyProperty as the backing store for LongValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LongValueProperty =
            DependencyProperty.Register(nameof(LongValue), typeof(long), typeof(HexBox),
                new FrameworkPropertyMetadata(0L,
                    LongValue_Changed,
                    LongValue_CoerceValue));

        private static object LongValue_CoerceValue(DependencyObject d, object baseValue)
        {
            var ctrl = d as HexBox;

            var newValue = (long)baseValue;

            if (newValue > ctrl.MaximumValue) newValue = ctrl.MaximumValue;
            if (newValue < 0) newValue = 0;

            return newValue;
        }

        private static void LongValue_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexBox ctrl)
                if (e.NewValue != e.OldValue)
                {
                    var val = ByteConverters.LongToHex((long)e.NewValue);

                    if (val == "00000000")
                        val = "0";
                    else if (val.Length >= 3)
                        val = val.TrimStart('0');

                    ctrl.HexTextBox.Text = val.ToUpper();
                    ctrl.HexTextBox.CaretIndex = ctrl.HexTextBox.Text.Length;
                    ctrl.ToolTip = e.NewValue;

                    ctrl.ValueChanged?.Invoke(ctrl, new EventArgs());
                }
        }

        #endregion Properties       

        #region Methods

        /// <summary>
        /// Substract one to the LongValue
        /// </summary>
        private void SubstractOne() => LongValue--;

        /// <summary>
        /// Add one to the LongValue
        /// </summary>
        private void AddOne() => LongValue++;

        /// <summary>
        /// Update value from decimal long
        /// </summary>
        private void UpdateValueFrom(long value) => LongValue = value;

        /// <summary>
        /// Update value from hex string
        /// </summary>
        private void UpdateValueFrom(string value)
        {
            var (success, val) = ByteConverters.HexLiteralToLong(value);

            LongValue = success ? val : 0;
        }

        #endregion Methods

        #region Controls events

        private void HexTextBox_PreviewKeyDown(object sender, KeyEventArgs e) =>
            e.Handled = !KeyValidator.IsHexKey(e.Key) &&
                !KeyValidator.IsBackspaceKey(e.Key) &&
                !KeyValidator.IsDeleteKey(e.Key) &&
                !KeyValidator.IsArrowKey(e.Key) &&
                !KeyValidator.IsTabKey(e.Key) &&
                !KeyValidator.IsEnterKey(e.Key);

        private void HexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                AddOne();

            if (e.Key == Key.Down)
                SubstractOne();

            HexTextBox.Focus();
        }

        private void UpButton_Click(object sender, RoutedEventArgs e) => AddOne();

        private void DownButton_Click(object sender, RoutedEventArgs e) => SubstractOne();

        private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e) =>
            UpdateValueFrom(HexTextBox.Text);

        private void CopyHexaMenuItem_Click(object sender, RoutedEventArgs e) =>
            Clipboard.SetText($"0x{HexTextBox.Text}");

        private void CopyLongMenuItem_Click(object sender, RoutedEventArgs e) =>
            Clipboard.SetText(LongValue.ToString());

        #endregion Controls events
    }
}