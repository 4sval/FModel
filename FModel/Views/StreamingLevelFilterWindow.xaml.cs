using System.Windows;
using System.Windows.Controls;
using FModel.ViewModels;

namespace FModel.Views;

public partial class StreamingLevelFilterWindow
{
    public StreamingLevelFilterWindow(StreamingLevelFilterViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnOkClick(object sender, RoutedEventArgs e) => Close();

    private void OnSkipClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: StreamingLevelFilterViewModel viewModel })
        {
            viewModel.SkipAll();
            Close();
        }
    }
}
