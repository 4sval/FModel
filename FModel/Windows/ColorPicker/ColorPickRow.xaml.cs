using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FModel.Windows.ColorPicker
{
    /// <summary>
    /// Logique d'interaction pour ColorPickRow.xaml
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
            if (ColorPickerWindow.ShowDialog(out Color color, Options))
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
