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

    public override void Execute(TabItem contextViewModel, object parameter)
    {
        if (parameter == null || !contextViewModel.HasImage) return;

        switch (parameter)
        {
            case "Open":
            {
                Helper.OpenWindow<AdonisWindow>(contextViewModel.SelectedImage.ExportName + " (Image)", () =>
                {
                    var popout = new ImagePopout
                    {
                        Title = contextViewModel.SelectedImage.ExportName + " (Image)",
                        Width = contextViewModel.SelectedImage.Image.Width,
                        Height = contextViewModel.SelectedImage.Image.Height,
                        WindowState = contextViewModel.SelectedImage.Image.Height > 1000 ? WindowState.Maximized : WindowState.Normal,
                        ImageCtrl = { Source = contextViewModel.SelectedImage.Image }
                    };
                    RenderOptions.SetBitmapScalingMode(popout.ImageCtrl, BoolToRenderModeConverter.Instance.Convert(contextViewModel.SelectedImage.RenderNearestNeighbor));
                    popout.Show();
                });
                break;
            }
            case "Copy":
                ClipboardExtensions.SetImage(contextViewModel.SelectedImage.ImageBuffer, $"{contextViewModel.SelectedImage.ExportName}.png");
                break;
            case "Save":
                contextViewModel.SaveImage(false);
                break;
        }
    }
}