using AdonisUI.Controls;
using FModel.Framework;
using FModel.Settings;
using FModel.Views;

namespace FModel.ViewModels.Commands
{
    public class DeleteBookmarkCommand : ViewModelCommand<BookmarksViewModel>
    {
        public DeleteBookmarkCommand(BookmarksViewModel contextViewModel) : base(contextViewModel) { }

        public override void Execute(BookmarksViewModel contextViewModel, object parameter)
        {
            var bmList = new BookmarkList(UserSettings.Default.Bookmarks);
            Helper.OpenWindow<AdonisWindow>("Delete Bookmark", () =>
            {
                var input = new BookmarkDelete(bmList);
                var result = input.ShowDialog();
                if (!result.HasValue || !result.Value || string.IsNullOrEmpty(bmList.SelectedBM))
                    return;

                contextViewModel.Delete(bmList.SelectedBM);
            });
        }
    }
}
