using System.Windows;
using FModel.ViewModels;

namespace FModel.Views
{
    public partial class CustomDir
    {
        public CustomDir(CustomDirectory customDir)
        {
            DataContext = customDir;
            InitializeComponent();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}