using System.Windows;
using FModel.ViewModels;

namespace FModel.Views;

public partial class BackupManager
{
    private readonly BackupManagerViewModel _viewModel;

    public BackupManager(string gameName)
    {
        DataContext = _viewModel = new BackupManagerViewModel(gameName);
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.Initialize();
    }

    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.Download();
    }

    private async void OnCreateBackupClick(object sender, RoutedEventArgs e)
    {
        _viewModel.IsCreatingBackup = true;
        try
        {
            await _viewModel.CreateBackup();
        }
        finally
        {
            _viewModel.IsCreatingBackup = false;
        }
    }

    private async void OnCreateBackupHeavyClick(object sender, RoutedEventArgs e)
    {
        _viewModel.IsCreatingBackup = true;
        try
        {
            await _viewModel.CreateBackupHeavy();
        }
        finally
        {
            _viewModel.IsCreatingBackup = false;
        }
    }
}
