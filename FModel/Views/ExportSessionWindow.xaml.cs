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
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ExportSessionViewModel { CanExport: true } viewModel })
            await viewModel.ExportAsync();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Close();
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

    private void OnRemoveFromQueueClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ObjectGroupViewModel item } && DataContext is ExportSessionViewModel viewModel)
            viewModel.RemoveFromQueue(item);
    }

    private void OnMakeDefaultOptions(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ExportSessionViewModel viewModel })
            viewModel.Options.SaveAsUserDefaults();
    }

    private void OnResetOptions(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ExportSessionViewModel viewModel })
            viewModel.Options.ResetToUserDefaults();
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
