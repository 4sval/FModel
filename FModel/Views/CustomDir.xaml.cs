using System.Windows;
using FModel.Settings;

namespace FModel.Views;

public partial class CustomDir
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
        DialogResult = true;
        Close();
    }
}
