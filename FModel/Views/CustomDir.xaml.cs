using Avalonia.Controls;
using Avalonia.Interactivity;
using FModel.Settings;

namespace FModel.Views;

public partial class CustomDir : Window
{
    public CustomDir(CustomDirectory customDir)
    {
        DataContext = customDir;
        InitializeComponent();

        Activate();
        WpfSuckMyDick.Focus();
        WpfSuckMyDick.SelectAll();
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
