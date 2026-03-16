using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using FModel.Services;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls;

public static class ListBoxItemBehavior
{
    public static readonly AttachedProperty<bool> IsBroughtIntoViewWhenSelectedProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItemBehavior, ListBoxItem, bool>("IsBroughtIntoViewWhenSelected");

    public static readonly AttachedProperty<bool> OpenOnDoubleTapProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItemBehavior, ListBoxItem, bool>("OpenOnDoubleTap");

    public static readonly AttachedProperty<bool> SelectFileOnRightClickProperty =
        AvaloniaProperty.RegisterAttached<ListBoxItemBehavior, ListBoxItem, bool>("SelectFileOnRightClick");

    public static bool GetIsBroughtIntoViewWhenSelected(ListBoxItem listBoxItem)
        => listBoxItem.GetValue(IsBroughtIntoViewWhenSelectedProperty);

    public static void SetIsBroughtIntoViewWhenSelected(ListBoxItem listBoxItem, bool value)
        => listBoxItem.SetValue(IsBroughtIntoViewWhenSelectedProperty, value);

    public static bool GetOpenOnDoubleTap(ListBoxItem listBoxItem)
        => listBoxItem.GetValue(OpenOnDoubleTapProperty);

    public static void SetOpenOnDoubleTap(ListBoxItem listBoxItem, bool value)
        => listBoxItem.SetValue(OpenOnDoubleTapProperty, value);

    public static bool GetSelectFileOnRightClick(ListBoxItem listBoxItem)
        => listBoxItem.GetValue(SelectFileOnRightClickProperty);

    public static void SetSelectFileOnRightClick(ListBoxItem listBoxItem, bool value)
        => listBoxItem.SetValue(SelectFileOnRightClickProperty, value);

    static ListBoxItemBehavior()
    {
        IsBroughtIntoViewWhenSelectedProperty.Changed.AddClassHandler<ListBoxItem>(OnIsBroughtIntoViewWhenSelectedChanged);
        OpenOnDoubleTapProperty.Changed.AddClassHandler<ListBoxItem>(OnOpenOnDoubleTapChanged);
        SelectFileOnRightClickProperty.Changed.AddClassHandler<ListBoxItem>(OnSelectFileOnRightClickChanged);
    }

    private static void OnIsBroughtIntoViewWhenSelectedChanged(ListBoxItem item, AvaloniaPropertyChangedEventArgs e)
    {
        item.PropertyChanged -= OnListBoxItemPropertyChanged;

        if (e.GetNewValue<bool>())
            item.PropertyChanged += OnListBoxItemPropertyChanged;
    }

    private static void OnOpenOnDoubleTapChanged(ListBoxItem item, AvaloniaPropertyChangedEventArgs e)
    {
        item.DoubleTapped -= OnListBoxItemDoubleTapped;

        if (e.GetNewValue<bool>())
            item.DoubleTapped += OnListBoxItemDoubleTapped;
    }

    private static void OnSelectFileOnRightClickChanged(ListBoxItem item, AvaloniaPropertyChangedEventArgs e)
    {
        item.PointerPressed -= OnListBoxItemPointerPressed;

        if (e.GetNewValue<bool>())
            item.PointerPressed += OnListBoxItemPointerPressed;
    }

    private static void OnListBoxItemPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is ListBoxItem item &&
            e.Property.Name == nameof(ListBoxItem.IsSelected) &&
            item.IsSelected)
        {
            item.BringIntoView();
        }
    }

    private static void OnListBoxItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBoxItem item)
            return;

        switch (item.DataContext)
        {
            case GameFileViewModel file:
                ApplicationService.ApplicationView.SelectedLeftTabIndex = 2;
                file.IsSelected = true;
                _ = file.ExtractAsync();
                break;
            case TreeItem folder:
                ApplicationService.ApplicationView.SelectedLeftTabIndex = 1;

                var parent = folder.Parent;
                while (parent != null)
                {
                    parent.IsExpanded = true;
                    parent = parent.Parent;
                }

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

    private static void OnListBoxItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ListBoxItem item)
            return;
        if (!e.GetCurrentPoint(item).Properties.IsRightButtonPressed)
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

        if (listBox.TryFindResource("FileContextMenu", out var resource) && resource is ContextMenu contextMenu)
        {
            listBox.ContextMenu = null;
            Dispatcher.UIThread.Post(() =>
            {
                contextMenu.DataContext = listBox.DataContext;
                listBox.ContextMenu = contextMenu;
                contextMenu.PlacementTarget = listBox;
                contextMenu.Open(listBox);
            });

            e.Handled = true;
        }
    }
}

