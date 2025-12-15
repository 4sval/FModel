using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls.ContextMenus;

public partial class FolderContextMenuDictionary
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public FolderContextMenuDictionary()
    {
        InitializeComponent();
    }

    private void FolderContextMenu_OnOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu { PlacementTarget: FrameworkElement fe } menu)
            return;

        var listBox = FindAncestor<ListBox>(fe);
        if (listBox != null)
        {
            menu.DataContext = listBox.DataContext;
            menu.Tag = listBox.SelectedItems;
            return;
        }

        var treeView = FindAncestor<TreeView>(fe);
        if (treeView != null)
        {
            menu.DataContext = treeView.DataContext;
            menu.Tag = new[] { treeView.SelectedItem }.ToList();
        }
    }

    private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T t)
                return t;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void OnFavoriteDirectoryClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: IEnumerable<object> list } || list.FirstOrDefault() is not TreeItem folder)
            return;

        _applicationView.CustomDirectories.Add(new CustomDirectory(folder.Header, folder.PathAtThisPoint));
        FLogger.Append(ELog.Information, () =>
            FLogger.Text($"Successfully saved '{folder.PathAtThisPoint}' as a new favorite directory", Constants.WHITE, true));
    }

    private void OnCopyDirectoryPathClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: IEnumerable<object> list } || list.FirstOrDefault() is not TreeItem folder)
            return;

        Clipboard.SetText(folder.PathAtThisPoint);
    }
}
