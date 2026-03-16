using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Serilog;
using System.Linq;

namespace FModel.Views.Resources.Controls.ContextMenus;

public partial class FileContextMenuDictionary : ResourceDictionary
{
    public FileContextMenuDictionary()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Resolves the DataContext from the placement target's visual tree.
    /// ContextMenus are hosted in popups (not under a Window), so
    /// $parent[Window].DataContext doesn't reliably resolve.
    /// </summary>
    private void FileContextMenu_OnOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu { PlacementTarget: { } target } menu)
            return;

        // Walk up the visual tree to the Window and grab its DataContext.
        var window = target.GetVisualAncestors().OfType<Window>().FirstOrDefault();
        if (window != null)
        {
            menu.DataContext = window.DataContext;
        }
        else
        {
            Log.Warning("FileContextMenu: could not find a Window ancestor for PlacementTarget {Target}", target.GetType().Name);
        }
    }
}
