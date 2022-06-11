using FModel.Framework;

namespace FModel.ViewModels.Commands;

public class DeleteDirectoryCommand : ViewModelCommand<CustomDirectoriesViewModel>
{
    public DeleteDirectoryCommand(CustomDirectoriesViewModel contextViewModel) : base(contextViewModel)
    {
    }

    public override void Execute(CustomDirectoriesViewModel contextViewModel, object parameter)
    {
        if (parameter is not CustomDirectory customDir) return;

        var index = contextViewModel.GetIndex(customDir);
        if (index < 2) return;

        contextViewModel.Delete(index);
    }
}