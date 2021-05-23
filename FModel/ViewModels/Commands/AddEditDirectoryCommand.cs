using AdonisUI.Controls;
using FModel.Framework;
using FModel.Views;

namespace FModel.ViewModels.Commands
{
    public class AddEditDirectoryCommand : ViewModelCommand<CustomDirectoriesViewModel>
    {
        public AddEditDirectoryCommand(CustomDirectoriesViewModel contextViewModel) : base(contextViewModel)
        {
        }

        public override void Execute(CustomDirectoriesViewModel contextViewModel, object parameter)
        {
            if (parameter is not CustomDirectory customDir)
                customDir = new CustomDirectory();

            Helper.OpenWindow<AdonisWindow>("Custom Directory", () =>
            {
                var index = contextViewModel.GetIndex(customDir);
                var input = new CustomDir(customDir);
                var result = input.ShowDialog();
                if (!result.HasValue || !result.Value || string.IsNullOrEmpty(customDir.Header) && string.IsNullOrEmpty(customDir.DirectoryPath))
                    return;

                if (index > 1)
                {
                    contextViewModel.Edit(index, customDir);
                }
                else
                    contextViewModel.Add(customDir);
            });
        }
    }
}