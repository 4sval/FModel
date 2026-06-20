using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using FModel.ViewModels;
using Ookii.Dialogs.Wpf;

namespace FModel.Views.Resources.Controls;

public partial class ExportOptionsControl
{
    public ExportOptionsControl()
    {
        InitializeComponent();
    }

    private void OnBrowseOutputDirectory(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ExportOptionsViewModel viewModel })
        {
            var folderBrowser = new VistaFolderBrowserDialog { ShowNewFolderButton = false };
            if (folderBrowser.ShowDialog() == true)
                viewModel.OutputDirectory = folderBrowser.SelectedPath;
        }
    }

    private void OnHyperlinkClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is Hyperlink hyperlink)
            Process.Start(new ProcessStartInfo(hyperlink.NavigateUri.AbsoluteUri) { UseShellExecute = true });
    }
}
