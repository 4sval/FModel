using System;
using FModel.Framework;
using FModel.Services;

namespace FModel.ViewModels.Commands
{
    public class GoToCommand : ViewModelCommand<CustomDirectoriesViewModel>
    {
        private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

        public GoToCommand(CustomDirectoriesViewModel contextViewModel) : base(contextViewModel)
        {
        }

        public override void Execute(CustomDirectoriesViewModel contextViewModel, object parameter)
        {
            if (parameter is not string s || string.IsNullOrEmpty(s)) return;

            JumpTo(s);
        }

        public TreeItem JumpTo(string directory)
        {
            MainWindow.YesWeCats.LeftTabControl.SelectedIndex = 1; // folders tab
            var root = _applicationView.CUE4Parse.AssetsFolder.Folders;
            if (root is not {Count: > 0}) return null;

            var i = 0;
            var done = false;
            var folders = directory.Split('/');
            while (!done)
            {
                foreach (var folder in root)
                {
                    if (!folder.Header.Equals(folders[i], i == 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                        continue;

                    folder.IsExpanded = true; // folder found = expand

                    // is this the last folder aka the one we want to jump in
                    if (i >= folders.Length - 1)
                    {
                        folder.IsSelected = true; // select it
                        return folder;
                    }

                    root = folder.Folders; // grab his subfolders
                    break;
                }

                i++;
                done = i == folders.Length || root.Count == 0;
            }

            return null;
        }
    }
}