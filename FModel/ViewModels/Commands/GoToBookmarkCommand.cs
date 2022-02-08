using FModel.Framework;
using FModel.Services;

namespace FModel.ViewModels.Commands
{
    public class GoToBookmarkCommand : ViewModelCommand<BookmarksViewModel>
    {
        private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

        public GoToBookmarkCommand(BookmarksViewModel contextViewModel) : base(contextViewModel)
        {
        }

        public override void Execute(BookmarksViewModel contextViewModel, object parameter)
        {
            if (parameter is not string s || string.IsNullOrEmpty(s)) return;

            contextViewModel.GoTo(s, _applicationView);
        }
    }
}
