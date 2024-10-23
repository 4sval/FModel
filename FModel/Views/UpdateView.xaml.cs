using System.Windows;
using FModel.ViewModels;
using FModel.Views.Resources.Controls;

namespace FModel.Views;

public partial class UpdateView
{
    public UpdateView()
    {
        DataContext = new UpdateViewModel();
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not UpdateViewModel viewModel) return;
        await viewModel.Load();
    }

    private void OnDownloadLatest(object sender, RoutedEventArgs e)
    {
        if (DataContext is not UpdateViewModel viewModel) return;
        viewModel.DownloadLatest();
    }
}

