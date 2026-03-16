using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FModel.Extensions;
using FModel.Framework;
using FModel.Views.Resources.Controls;
using FModel.Views.Resources.Converters;
using ImGuiController = FModel.Framework.ImGuiController;

namespace FModel.ViewModels.Commands;

public class ImageCommand : ViewModelCommand<TabItem>
{
    public ImageCommand(TabItem contextViewModel) : base(contextViewModel)
    {
    }

    public override void Execute(TabItem tabViewModel, object parameter)
    {
        if (parameter == null || !tabViewModel.HasImage)
            return;

        switch (parameter)
        {
            case "Open":
                {
                    Helper.OpenWindow<Window>(tabViewModel.SelectedImage.ExportName + " (Image)", () =>
                    {
                        var pixelWidth = tabViewModel.SelectedImage.Image.PixelSize.Width;
                        var pixelHeight = tabViewModel.SelectedImage.Image.PixelSize.Height;
                        var dpiScale = ImGuiController.GetDpiScale();
                        var popout = new ImagePopout
                        {
                            Title = tabViewModel.SelectedImage.ExportName + " (Image)",
                            Width = pixelWidth / dpiScale,
                            Height = pixelHeight / dpiScale,
                            WindowState = pixelHeight > 1000 ? WindowState.Maximized : WindowState.Normal,
                        };
                        popout.ImageCtrl.Source = tabViewModel.SelectedImage.Image;
                        RenderOptions.SetBitmapInterpolationMode(popout.ImageCtrl, BoolToRenderModeConverter.Instance.Convert(tabViewModel.SelectedImage.RenderNearestNeighbor));
                        popout.Show();
                    });
                    break;
                }
            case "Copy":
                ClipboardExtensions.SetImage(tabViewModel.SelectedImage.ImageBuffer);
                break;
            case "Save":
                tabViewModel.SaveImage();
                break;
        }
    }
}
