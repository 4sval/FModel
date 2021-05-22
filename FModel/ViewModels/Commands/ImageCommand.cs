using System.Windows;
using AdonisUI.Controls;
using FModel.Framework;
using FModel.Views.Resources.Controls;

namespace FModel.ViewModels.Commands
{
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
                    Helper.OpenWindow<AdonisWindow>(contextViewModel.Header + " (Image)", () =>
                    {
                        new ImagePopout
                        {
                            Title = contextViewModel.Header + " (Image)",
                            Width = contextViewModel.Image.Width,
                            Height = contextViewModel.Image.Height,
                            WindowState = contextViewModel.Image.Height > 1000 ? WindowState.Maximized : WindowState.Normal,
                            ImageCtrl = {Source = contextViewModel.Image}
                        }.Show();
                    });
                    break;
                }
                case "Copy":
                    Clipboard.SetImage(contextViewModel.Image);
                    break;
                case "Save":
                    contextViewModel.SaveImage(false);
                    break;
            }
        }
    }
}