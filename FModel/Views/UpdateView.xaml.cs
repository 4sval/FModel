using Avalonia.Controls;
using Avalonia.Interactivity;
using FModel.ViewModels;
using FModel.Views.Resources.Controls;

namespace FModel.Views;

public partial class UpdateView : Window
{
    public UpdateView()
    {
        DataContext = new UpdateViewModel();
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not UpdateViewModel viewModel)
            return;
        _ = viewModel.LoadAsync();
    }

    private void OnDownloadLatest(object sender, RoutedEventArgs e)
    {
        if (DataContext is not UpdateViewModel viewModel)
            return;
        viewModel.DownloadLatest();
    }
}
