using System.Windows;
using AdonisUI.Controls;

namespace FModel.Views.Resources.Controls;

public partial class InputDialog : AdonisWindow
{
    public string InputText { get; set; }

    public InputDialog(string title, string defaultText = "")
    {
        InitializeComponent();
        DataContext = this;
        Title = title;
        InputText = defaultText;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
