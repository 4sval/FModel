using System.ComponentModel;
using System.Windows;
using FModel.Services;
using FModel.ViewModels;

namespace FModel.Views
{
    public partial class AesManager
    {
        private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

        public AesManager()
        {
            DataContext = _applicationView;
            InitializeComponent();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void OnRefreshAes(object sender, RoutedEventArgs e)
        {
            await _applicationView.CUE4Parse.RefreshAes();
            await _applicationView.AesManager.InitAes();
            _applicationView.AesManager.HasChange = true; // yes even if nothing actually changed
        }

        private async void OnClosing(object sender, CancelEventArgs e)
        {
            await _applicationView.AesManager.UpdateProvider(false);
        }
    }
}