using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FModel.Extensions;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
using FModel.Views;

namespace FModel.Views.Resources.Controls.ContextMenus;

public partial class FolderContextMenuDictionary
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public FolderContextMenuDictionary()
    {
        InitializeComponent();
    }

    private void FolderContextMenu_OnOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu { PlacementTarget: Control control } menu)
            return;

        var listBox = FindAncestor<ListBox>(control);
        if (listBox != null)
        {
            menu.DataContext = listBox.DataContext;
            menu.Tag = listBox.SelectedItems?.Cast<object>().ToList() ?? [];
            return;
        }

        var treeView = FindAncestor<TreeView>(control);
        if (treeView != null)
        {
            menu.DataContext = treeView.DataContext;
            menu.Tag = treeView.SelectedItem is not null ? new[] { treeView.SelectedItem }.ToList() : [];
        }
    }

    private static T? FindAncestor<T>(Control? current) where T : class
    {
        if (current is null)
            return null;

        if (current is T self)
            return self;

        return current.GetVisualAncestors().OfType<T>().FirstOrDefault();
    }

    private void OnFavoriteDirectoryClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: IEnumerable<object> list } || list.FirstOrDefault() is not TreeItem folder)
            return;

        _applicationView.CustomDirectories.Add(new CustomDirectory(folder.Header, folder.PathAtThisPoint));
        FLogger.Append(ELog.Information, () =>
            FLogger.Text($"Successfully saved '{folder.PathAtThisPoint}' as a new favorite directory", Constants.WHITE, true));
    }

    private void OnCopyDirectoryPathClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: IEnumerable<object> list } || list.FirstOrDefault() is not TreeItem folder)
            return;

        ClipboardExtensions.SetText(folder.PathAtThisPoint);
    }
}
