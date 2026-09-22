using System;
using FModel.Framework;
using FModel.Services;

namespace FModel.ViewModels.Commands;

public class GoToCommand(CustomDirectoriesViewModel contextViewModel) : ViewModelCommand<CustomDirectoriesViewModel>(contextViewModel)
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public override void Execute(CustomDirectoriesViewModel contextViewModel, object parameter)
    {
        if (parameter is not string s || string.IsNullOrEmpty(s)) return;

        var folder = JumpTo(s);
        if (folder != null)
        {
            MainWindow.Instance.SelectFolder(folder);
        }
    }

    public TreeItem JumpTo(string directory)
    {
        _applicationView.SelectedLeftTabIndex = 1;

        var current = _applicationView.CUE4Parse.AssetsFolder.Folders;
        if (current.Count == 0)
            return null;

        var folders = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);

        TreeItem result = null;
        for (var i = 0; i < folders.Length; i++)
        {
            result = null;

            foreach (var folder in current)
            {
                if (!folder.Header.Equals(folders[i], i == 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    continue;

                result = folder;
                break;
            }

            if (result == null)
                return null;

            current = result.Folders;
        }

        if (result != null)
        {
            _applicationView.CUE4Parse.AssetsFolder.Reveal(result);
        }

        return result;
    }
}
