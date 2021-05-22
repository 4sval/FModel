using System.Windows;
using FModel.ViewModels;

namespace FModel.Views
{
    public partial class BackupManager
    {
        private readonly BackupManagerViewModel _viewModel;

        public BackupManager(FGame game)
        {
            DataContext = _viewModel = new BackupManagerViewModel(game);
            InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.Initialize();
        }

        private async void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not BackupManagerViewModel viewModel) return;
            await viewModel.Download();
        }

        private async void OnCreateBackupClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not BackupManagerViewModel viewModel) return;
            await viewModel.CreateBackup();
        }
    }
}