using System;
using System.Windows;
using System.Windows.Media;
using ColorPickerWPF.Code;
using UserControl = System.Windows.Controls.UserControl;

namespace ColorPickerWPF
{
    /// <summary>
    /// Interaction logic for ColorPickRow.xaml
    /// </summary>
    public partial class ColorPickRow : UserControl
    {
        public event EventHandler OnPick;

        public Color Color { get; set; }

        public ColorPickerDialogOptions Options { get; set; }

        public ColorPickRow()
        {
            InitializeComponent();
        }

        private void PickColorButton_OnClick(object sender, RoutedEventArgs e)
        {
            Color color;
            if (ColorPickerWindow.ShowDialog(out color, Options))
            {
                SetColor(color);
                OnPick?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetColor(Color color)
        {
            Color = color;
            HexLabel.Text = color.ToHexString();
            ColorDisplayGrid.Background = new SolidColorBrush(color);
        }
    }
}
