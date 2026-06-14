using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using FModel.ViewModels;
using Ookii.Dialogs.Wpf;

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

    private void OnBrowseOutputDirectory(object sender, RoutedEventArgs e)
    {
        var folderBrowser = new VistaFolderBrowserDialog { ShowNewFolderButton = false };
        if (folderBrowser.ShowDialog() == true)
            ExportSessionViewModel.Instance.Options.OutputDirectory = folderBrowser.SelectedPath;
    }

    private void OnMakeDefaultOptions(object sender, RoutedEventArgs e)
    {
        ExportSessionViewModel.Instance.Options.SaveAsUserDefaults();
    }

    private void OnResetOptions(object sender, RoutedEventArgs e)
    {
        ExportSessionViewModel.Instance.Options.ResetToUserDefaults();
    }

    private void OnHyperlinkClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is Hyperlink hyperlink)
            Process.Start(new ProcessStartInfo(hyperlink.NavigateUri.AbsoluteUri) { UseShellExecute = true });
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
