using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using FModel.Services;
using FModel.ViewModels;

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

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}

