using AdonisUI.Controls;
using FModel.Extensions;
using FModel.Framework;
using FModel.Views.Resources.Controls;
using System.Windows;
using System.Windows.Media;
using FModel.Views.Resources.Converters;

namespace FModel.ViewModels.Commands;

public class ImageCommand : ViewModelCommand<TabItem>
{
    public ImageCommand(TabItem contextViewModel) : base(contextViewModel)
    {
    }

    public override void Execute(TabItem tabViewModel, object parameter)
    {
        if (parameter == null || !tabViewModel.HasImage) return;

        switch (parameter)
        {
            case "Open":
            {
                Helper.OpenWindow<AdonisWindow>(tabViewModel.SelectedImage.ExportName + " (Image)", () =>
                {
                    var popout = new ImagePopout
                    {
                        Title = tabViewModel.SelectedImage.ExportName + " (Image)",
                        Width = tabViewModel.SelectedImage.Image.Width,
                        Height = tabViewModel.SelectedImage.Image.Height,
                        WindowState = tabViewModel.SelectedImage.Image.Height > 1000 ? WindowState.Maximized : WindowState.Normal,
                        ImageCtrl = { Source = tabViewModel.SelectedImage.Image }
                    };
                    RenderOptions.SetBitmapScalingMode(popout.ImageCtrl, BoolToRenderModeConverter.Instance.Convert(tabViewModel.SelectedImage.RenderNearestNeighbor));
                    popout.Show();
                });
                break;
            }
            case "Copy":
                ClipboardExtensions.SetImage(tabViewModel.SelectedImage.ImageBuffer, $"{tabViewModel.SelectedImage.ExportName}.png");
                break;
            case "Save":
                tabViewModel.SaveImage();
                break;
        }
    }
}
