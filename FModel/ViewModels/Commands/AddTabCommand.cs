using FModel.Framework;

namespace FModel.ViewModels.Commands;

public class AddTabCommand : ViewModelCommand<TabControlViewModel>
{
    public AddTabCommand(TabControlViewModel contextViewModel) : base(contextViewModel)
    {
    }

    public override void Execute(TabControlViewModel contextViewModel, object parameter)
    {
        contextViewModel.AddTab();
    }
}