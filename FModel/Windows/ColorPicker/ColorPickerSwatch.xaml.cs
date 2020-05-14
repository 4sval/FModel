using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FModel.Windows.ColorPicker
{
    /// <summary>
    /// Logique d'interaction pour ColorPickerSwatch.xaml
    /// </summary>
    public partial class ColorPickerSwatch : UserControl
    {
        public delegate void ColorSwatchPickHandler(Color color);

        public static ColorPickerControl ColorPickerControl { get; set; }

        public event ColorSwatchPickHandler OnPickColor;

        public bool Editable { get; set; }
        public Color CurrentColor = Colors.White;

        public ColorPickerSwatch()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var border = (sender as Border);
            if (border == null)
                return;

            if (Editable && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                border.Background = new SolidColorBrush(CurrentColor);

                if (border.DataContext is ColorSwatchItem data)
                {
                    data.Color = CurrentColor;
                    data.HexString = CurrentColor.ToHexString();
                }

                if (ColorPickerControl != null)
                {
                    ColorPickerControl.CustomColorsChanged();
                }
            }
            else
            {
                var color = border.Background as SolidColorBrush;
                OnPickColor?.Invoke(color.Color);
            }


        }


        internal List<ColorSwatchItem> GetColors()
        {
            var results = new List<ColorSwatchItem>();
            if (SwatchListBox.ItemsSource is List<ColorSwatchItem> colors)
            {
                return colors;
            }

            return results;
        }
    }
}
