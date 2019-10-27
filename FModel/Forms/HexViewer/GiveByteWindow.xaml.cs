using System.Windows;

namespace WpfHexaEditor.Dialog
{
    /// <summary>
    /// This Window is used to give a hex value for fill the selection with.
    /// </summary>
    internal partial class GiveByteWindow
    {
        public GiveByteWindow() => InitializeComponent();

        private void OKButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}