using System.IO;
using AdonisUI.Controls;
using FModel.Framework;
using FModel.Settings;
using FModel.Views;

namespace FModel.ViewModels.Commands
{
    public class AddBookmarkCommand : ViewModelCommand<BookmarksViewModel>
    {
        public AddBookmarkCommand(BookmarksViewModel contextViewModel) : base(contextViewModel) { }

        public override void Execute(BookmarksViewModel contextViewModel, object parameter)
        {
            if (parameter is not FGame gameEnum)
                gameEnum = FGame.Unknown;
            var key = UserSettings.Default.AesKeys[gameEnum].MainKey;
            var bookmark = new Bookmark(string.Empty, UserSettings.Default.GameDirectory, key, gameEnum);

            Helper.OpenWindow<AdonisWindow>("Add Bookmark", () =>
            {
                var input = new BookmarkAdd(bookmark);
                var result = input.ShowDialog();
                if (!result.HasValue || !result.Value || string.IsNullOrEmpty(bookmark.Header.Trim()) || string.IsNullOrEmpty(bookmark.DirectoryPath) || !Directory.Exists(bookmark.DirectoryPath))
                {
                    MessageBox.Show("Bookmark name cannot be empty, directory has to be valid.\n No changes were made.", "Invalid data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                contextViewModel.Add(bookmark);
            });
        }
    }
}
