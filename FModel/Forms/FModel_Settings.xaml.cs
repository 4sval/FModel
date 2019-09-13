using FModel.Methods.MessageBox;
using System.Windows;
using FProp = FModel.Properties.Settings;

namespace FModel.Forms
{
    /// <summary>
    /// Logique d'interaction pour FModel_Settings.xaml
    /// </summary>
    public partial class FModel_Settings : Window
    {
        public FModel_Settings()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetUserSettings();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetUserSettings();
            Close();
        }

        private void GetUserSettings()
        {
            InputTextBox.Text = FProp.Default.FPak_Path;
            OutputTextBox.Text = FProp.Default.FOutput_Path;
        }

        private void SetUserSettings()
        {
            if (!string.Equals(FProp.Default.FPak_Path, InputTextBox.Text))
            {
                FProp.Default.FPak_Path = InputTextBox.Text;
                DarkMessageBox.Show("Please, restart FModel to apply your new input path", "FModel Input Path Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (!string.Equals(FProp.Default.FOutput_Path, OutputTextBox.Text))
            {
                FProp.Default.FOutput_Path = OutputTextBox.Text;
                DarkMessageBox.Show("Please, restart FModel to apply your new output path", "FModel Output Path Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            FProp.Default.Save();
        }
    }
}
