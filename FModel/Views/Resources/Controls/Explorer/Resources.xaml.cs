using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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

                var childFolder = folder;
                while (childFolder.Folders.Count == 1 && childFolder.AssetsList.Assets.Count == 0)
                    childFolder = childFolder.Folders[0];

                MainWindow.Instance.SelectFolder(childFolder);
                ApplicationService.ApplicationView.CUE4Parse.AssetsFolder.Expand(childFolder);
                break;
        }
    }

    // Hack to force re-evaluation of context menu options, also prevents menu flicker from happening
    private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item)
            return;
        if (item.DataContext is not GameFileViewModel)
            return;
        var listBox = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
        if (listBox == null)
            return;

        if (!item.IsSelected)
        {
            listBox.UnselectAll();
            item.IsSelected = true;
        }
        item.Focus();

        var contextMenu = listBox.FindResource("FileContextMenu") as ContextMenu;
        if (contextMenu is not null)
        {
            listBox.ContextMenu = null;
            item.Dispatcher.BeginInvoke(new Action(() =>
            {
                contextMenu.DataContext = listBox.DataContext;
                listBox.ContextMenu = contextMenu;
                contextMenu.PlacementTarget = listBox;
                contextMenu.IsOpen = true;
            }), DispatcherPriority.Input);
        }

        e.Handled = true;
    }
}
