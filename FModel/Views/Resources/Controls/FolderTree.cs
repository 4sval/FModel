using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using FModel.Extensions;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls;

public sealed class FolderTree : ListBox
{
    public static readonly DependencyProperty FoldersProperty = DependencyProperty.Register(nameof(Folders), typeof(AssetsFolderViewModel), typeof(FolderTree));

    public AssetsFolderViewModel Folders
    {
        get => (AssetsFolderViewModel) GetValue(FoldersProperty);
        set => SetValue(FoldersProperty, value);
    }

    public event EventHandler OpenAssets;

    public FolderTree()
    {
        IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true)
            {
                RevealSelection();
            }
        };
    }

    public void SelectFolder(TreeItem folder)
    {
        Folders.Reveal(folder);
        SetCurrentValue(SelectedItemProperty, folder);
        this.RevealItem(folder);
    }

    public void FocusSelection()
    {
        if (SelectedItem is TreeItem folder)
        {
            this.RevealItem(folder)?.Focus();
        }
        else
        {
            Focus();
        }
    }

    public void RevealSelection()
    {
        if (SelectedItem is TreeItem folder)
        {
            this.RevealItem(folder, alignToTop: true);
        }
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && source.FindAncestor<ToggleButton>() is { DataContext: TreeItem folder } && ContainerFromElement(this, source) is ListBoxItem)
        {
            e.Handled = true;
            Folders?.Toggle(folder);
            return;
        }

        base.OnPreviewMouseLeftButtonDown(e);
    }

    protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.OriginalSource is DependencyObject source && source.FindAncestor<ToggleButton>() == null && ContainerFromElement(this, source) is ListBoxItem { DataContext: TreeItem folder })
        {
            e.Handled = true;
            if (folder.Folders.Count > 0)
            {
                Folders?.Toggle(folder);
            }
            else if (folder.AssetsList.Assets.Count > 0)
            {
                OpenAssets?.Invoke(this, EventArgs.Empty);
            }
        }

        base.OnPreviewMouseDoubleClick(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (Folders == null || SelectedItem is not TreeItem folder)
        {
            base.OnPreviewKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Up:
                SelectFolder((TreeItem) Items[Math.Max(0, SelectedIndex - 1)]);
                break;
            case Key.Down:
                SelectFolder((TreeItem) Items[Math.Min(Items.Count - 1, SelectedIndex + 1)]);
                break;
            case Key.Home:
                SelectFolder((TreeItem) Items[0]);
                break;
            case Key.End:
                SelectFolder((TreeItem) Items[Items.Count - 1]);
                break;
            case Key.Left:
                if (folder.IsExpanded)
                {
                    Folders?.Toggle(folder);
                }
                else if (folder.Parent != null)
                {
                    SelectFolder(folder.Parent);
                }
                break;
            case Key.Right:
                if (!folder.IsExpanded)
                {
                    Folders?.Toggle(folder);
                }
                else if (folder.Folders.Count > 0)
                {
                    SelectFolder(folder.Folders[0]);
                }
                break;
            case Key.Enter:
                if ((folder.IsExpanded || folder.Folders.Count == 0) && folder.AssetsList.Assets.Count > 0)
                {
                    e.Handled = true;
                    OpenAssets?.Invoke(this, EventArgs.Empty);
                    return;
                }

                while (folder.Folders.Count == 1 && folder.AssetsList.Assets.Count == 0)
                    folder = folder.Folders[0];

                SelectFolder(folder);
                if (!folder.IsExpanded)
                {
                    Folders?.Toggle(folder);
                }
                break;
            default:
                base.OnPreviewKeyDown(e);
                return;
        }

        e.Handled = true;
        FocusSelection();
    }
}
