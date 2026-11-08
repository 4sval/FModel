using AdonisUI.Controls;
using FModel.Extensions;
using FModel.Framework;
using FModel.Views.Resources.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using FModel.Views.Resources.Converters;

namespace FModel.ViewModels.Commands;

public class ImageCommand(TabItem contextViewModel) : ViewModelCommand<TabItem>(contextViewModel)
{
    public override void Execute(TabItem tabViewModel, object parameter)
    {
        if (parameter == null || tabViewModel?.SelectedImage is not { } image)
            return;

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
                    ImageAnimation.SetFrames(popout.ImageCtrl, tabViewModel.SelectedImage.AnimationFrames);
                    popout.Show();
                });
                break;
            }
            case "InspectTexture" when image.InspectorTexture is { } texture:
                SnooperViewModel.Instance.InspectTexture(texture, image.Name, image.RenderNearestNeighbor);
                break;
            case "PreviousImage":
                tabViewModel.GoPreviousImage();
                break;
            case "NextImage":
                tabViewModel.GoNextImage();
                break;
            case "Copy":
            case BitmapSource:
                ClipboardExtensions.SetImage(parameter as BitmapSource ?? tabViewModel.SelectedImage.Image, Path.ChangeExtension(tabViewModel.SelectedImage.ExportName, "png"));
                break;
            case "Save":
                tabViewModel.SaveImage();
                break;
        }
    }
}
