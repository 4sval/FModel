using System.Windows;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls.Diff;

public partial class ImageDiffViewer
{
    public ImageDiffViewer()
    {
        InitializeComponent();
    }

    public void SetImages(TabImage left, TabImage right)
    {
        if (left?.Image != null)
        {
            LeftImage.Source = left.Image;
            LeftImageNotFound.Visibility = Visibility.Collapsed;
        }
        else
        {
            LeftImage.Source = null;
            LeftImageNotFound.Visibility = Visibility.Visible;
        }

        if (right?.Image != null)
        {
            RightImage.Source = right.Image;
            RightImageNotFound.Visibility = Visibility.Collapsed;
        }
        else
        {
            RightImage.Source = null;
            RightImageNotFound.Visibility = Visibility.Visible;
        }
    }
}
