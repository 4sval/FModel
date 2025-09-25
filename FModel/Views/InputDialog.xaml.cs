using System.Windows;
using AdonisUI.Controls;

namespace FModel.Views
{
    public partial class InputDialog : AdonisWindow
    {
        public string ResponseText => ResponseTextBox.Text;

        public InputDialog()
        {
            InitializeComponent();
            ResponseTextBox.Focus();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
