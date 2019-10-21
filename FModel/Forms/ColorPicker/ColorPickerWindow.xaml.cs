using System.Windows;
using System.Windows.Media;
using ColorPickerWPF.Code;

namespace ColorPickerWPF
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        protected readonly int WidthMax = 574;
        protected readonly int WidthMin = 342;
        protected bool SimpleMode { get; set; }

        public ColorPickerWindow()
        {
            InitializeComponent();
        }

        public static bool ShowDialog(out Color color, ColorPickerDialogOptions flags = ColorPickerDialogOptions.None, ColorPickerControl.ColorPickerChangeHandler customPreviewEventHandler = null)
        {
            if ((flags & ColorPickerDialogOptions.LoadCustomPalette) == ColorPickerDialogOptions.LoadCustomPalette)
            {
                ColorPickerSettings.UsingCustomPalette = true;
            }

            var instance = new ColorPickerWindow();
            color = instance.ColorPicker.Color;

            if ((flags & ColorPickerDialogOptions.SimpleView) == ColorPickerDialogOptions.SimpleView)
            {
                instance.ToggleSimpleAdvancedView();
            }

            if (ColorPickerSettings.UsingCustomPalette)
            {
                instance.ColorPicker.LoadDefaultCustomPalette();
            }

            if (customPreviewEventHandler != null)
            {
                instance.ColorPicker.OnPickColor += customPreviewEventHandler;
            }

            var result = instance.ShowDialog();
            if (result.HasValue && result.Value)
            {
                color = instance.ColorPicker.Color;
                return true;
            }

            return false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Hide();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Hide();
        }

        private void MinMaxViewButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SimpleMode)
            {
                SimpleMode = false;
                MinMaxViewButton.Content = "<< Simple";
                Width = WidthMax;
            }
            else
            {
                SimpleMode = true;
                MinMaxViewButton.Content = "Advanced >>";
                Width = WidthMin;
            }
        }

        public void ToggleSimpleAdvancedView()
        {
            if (SimpleMode)
            {
                SimpleMode = false;
                MinMaxViewButton.Content = "<< Simple";
                Width = WidthMax;
            }
            else
            {
                SimpleMode = true;
                MinMaxViewButton.Content = "Advanced >>";
                Width = WidthMin;
            }
        }
    }
}
