using System.Windows;
using System.Windows.Input;

namespace FModel.Windows.UserInput
{
    /// <summary>
    /// Logique d'interaction pour GoToUserInput.xaml
    /// </summary>
    public partial class GoToUserInput : Window
    {
        public GoToUserInput()
        {
            InitializeComponent();
        }
        public GoToUserInput(string name, string dir)
        {
            InitializeComponent();
            DName_TxtBox.Text = name;
            DPath_TxtBox.Text = dir;
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }

        public new string Name
        {
            get { return DName_TxtBox.Text; }
        }
        public string Directory
        {
            get { return DPath_TxtBox.Text; }
        }

        private void DName_TxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DPath_TxtBox.Focus();
            }
        }

        private void DPath_TxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.DialogResult = true;
                Close();
            }
        }
    }
}
