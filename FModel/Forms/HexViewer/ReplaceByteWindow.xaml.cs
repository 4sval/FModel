using System.Windows;

namespace WpfHexaEditor.Dialog
{
    /// <summary>
    /// This Window is used to give tow hex value for deal with.
    /// </summary>
    internal partial class ReplaceByteWindow
    {
        public ReplaceByteWindow() => InitializeComponent();

        private void OKButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}