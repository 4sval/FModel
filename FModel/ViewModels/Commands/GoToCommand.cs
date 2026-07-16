using System.Collections.Generic;
using System.Threading.Tasks;
using FModel.Framework;
using FModel.Services;

namespace FModel.ViewModels.Commands;

public class GoToCommand : ViewModelCommand<CustomDirectoriesViewModel>
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public GoToCommand(CustomDirectoriesViewModel contextViewModel) : base(contextViewModel)
    {
    }

    public override async void Execute(CustomDirectoriesViewModel contextViewModel, object parameter)
    {
        if (parameter is not string s || string.IsNullOrEmpty(s)) return;

        await JumpToAsync(s);
    }

    public async Task<TreeItem> JumpToAsync(string directory)
    {
        _applicationView.SelectedLeftTabIndex = 1; // folders tab
        if (!_applicationView.CUE4Parse.AssetsFolder.TryGetFolder(directory, out var folder))
            return null;

        // An ancestor of the selected folder is already realized. Selecting it directly
        // avoids running the virtualized path walker again (notably for breadcrumbs).
        if (MainWindow.YesWeCats.AssetsFolderName.SelectedItem is TreeItem selectedFolder)
        {
            for (var ancestor = selectedFolder; ancestor != null; ancestor = ancestor.Parent)
            {
                if (!ReferenceEquals(ancestor, folder))
                    continue;

                folder.IsSelected = true;
                return folder;
            }
        }

        var ancestors = new Stack<TreeItem>();
        for (var ancestor = folder; ancestor != null; ancestor = ancestor.Parent)
            ancestors.Push(ancestor);

        var path = new List<TreeItem>(ancestors.Count);
        while (ancestors.TryPop(out var ancestor))
            path.Add(ancestor);

        return await MainWindow.YesWeCats.SelectFolderAsync(path) ? folder : null;
    }
}
