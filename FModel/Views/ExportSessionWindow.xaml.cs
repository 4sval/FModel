using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FModel.ViewModels;

namespace FModel.Views;

public partial class ExportSessionWindow
{
    public ExportSessionWindow()
    {
        InitializeComponent();
        DataContext = ExportSessionViewModel.Instance;
    }

    private async void OnExportOrOkClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ExportSessionViewModel viewModel })
            return;

        if (viewModel.IsFinished) Close();
        else if (viewModel.CanExport) await viewModel.ExportAsync();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ExportSessionViewModel viewModel })
            viewModel.CancelExport();
    }

    private void OnClearQueueClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ExportSessionViewModel viewModel })
            viewModel.ClearQueue();
    }

    private void OnOpenInExplorerClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string path }) return;

        try
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
        }
        catch
        {
            //
        }
    }
}
