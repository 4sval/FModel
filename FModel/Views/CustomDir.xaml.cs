using System.Windows;
using FModel.ViewModels;

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