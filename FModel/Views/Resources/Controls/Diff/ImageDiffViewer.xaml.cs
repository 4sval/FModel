using System.Windows.Controls;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls.Diff;

public partial class ImageDiffViewer : UserControl
{
    public ImageDiffViewer()
    {
        InitializeComponent();
    }

    public void SetImages(TabImage left, TabImage right)
    {
        LeftImage.Source = left?.Image;
        RightImage.Source = right?.Image;
    }
}
