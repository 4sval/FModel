using System.Windows.Controls;
using System.Windows.Input;
using FModel.Services;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls.TiledExplorer;

public partial class ResourcesDictionary
{
    public ResourcesDictionary()
    {
        InitializeComponent();
    }

    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item)
            return;

        switch (item.DataContext)
        {
            case GameFileViewModel file:
                ApplicationService.ApplicationView.SelectedLeftTabIndex = 2;
                file.IsSelected = true;
                file.ExtractAsync();
                break;
            case TreeItem folder:
                ApplicationService.ApplicationView.SelectedLeftTabIndex = 1;

                // Expand all parent folders if not expanded
                var parent = folder.Parent;
                while (parent != null)
                {
                    parent.IsExpanded = true;
                    parent = parent.Parent;
                }

                // Auto expand single child folders
                var childFolder = folder;
                while (childFolder.Folders.Count == 1 && childFolder.AssetsList.Assets.Count == 0)
                {
                    childFolder.IsExpanded = true;
                    childFolder = childFolder.Folders[0];
                }

                childFolder.IsExpanded = true;
                childFolder.IsSelected = true;
                break;
        }

    }
}
