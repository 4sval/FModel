using System.Windows;
using AdonisUI.Controls;

namespace FModel.Views.Resources.Controls;

public partial class InputDialog : AdonisWindow
{
    public string InputText { get; set; }
    public string DescriptionText { get; set; }

    public InputDialog(string title, string inputFolderName = "", string descriptionText = "")
    {
        InitializeComponent();
        DataContext = this;
        Title = title;
        InputText = inputFolderName;
        DescriptionText = descriptionText;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
