using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using FModel.ViewModels;
using Ookii.Dialogs.Wpf;

namespace FModel.Views.Resources.Controls;

public partial class ExportOptionsControl
{
    public ExportOptionsControl()
    {
        InitializeComponent();
        PngCompressionSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnPngCompressionDrag), true);
        PngCompressionSlider.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnPngCompressionDrag), true);
    }

    private void OnPngCompressionDrag(object sender, RoutedEventArgs e)
    {
        if (PngCompressionSlider.Value == 3 && e.OriginalSource is Thumb { ToolTip: ToolTip tooltip })
        {
            tooltip.Content = "3 (Recommended)";
        }
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
