using Avalonia.Controls;

namespace FModel.Views.Resources.Controls;

public partial class ImagePopout : Window
{
    public ImagePopout()
    {
        InitializeComponent();
        MagnifierManager.SetMagnifier(RootPanel, new Magnifier { Radius = 150, ZoomFactor = 0.7 });
    }
}
