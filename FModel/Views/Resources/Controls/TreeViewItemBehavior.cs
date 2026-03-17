using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace FModel.Views.Resources.Controls;

public sealed class TreeViewItemBehavior
{
    public static bool GetIsBroughtIntoViewWhenSelected(TreeViewItem treeViewItem)
    {
        return treeViewItem.GetValue(IsBroughtIntoViewWhenSelectedProperty);
    }

    public static void SetIsBroughtIntoViewWhenSelected(TreeViewItem treeViewItem, bool value)
    {
        treeViewItem.SetValue(IsBroughtIntoViewWhenSelectedProperty, value);
    }

    public static readonly AttachedProperty<bool> IsBroughtIntoViewWhenSelectedProperty =
        AvaloniaProperty.RegisterAttached<TreeViewItemBehavior, TreeViewItem, bool>("IsBroughtIntoViewWhenSelected");

    static TreeViewItemBehavior()
    {
        IsBroughtIntoViewWhenSelectedProperty.Changed.AddClassHandler<TreeViewItem>(OnIsBroughtIntoViewWhenSelectedChanged);
    }

    private static void OnIsBroughtIntoViewWhenSelectedChanged(TreeViewItem item, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool value)
            return;

        DisposeSubscription(item);

        if (value)
        {
            var sub = item.GetObservable(TreeViewItem.IsSelectedProperty)
                .Subscribe(isSelected =>
                {
                    if (isSelected)
                        item.BringIntoView();
                });
            item.SetValue(SubscriptionProperty, sub);

            // Clean up when the item is removed from the visual tree (virtualization)
            item.DetachedFromVisualTree -= OnDetachedFromVisualTree;
            item.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }
        else
        {
            item.DetachedFromVisualTree -= OnDetachedFromVisualTree;
        }
    }

    private static void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TreeViewItem item)
            DisposeSubscription(item);
    }

    private static void DisposeSubscription(TreeViewItem item)
    {
        if (item.GetValue(SubscriptionProperty) is IDisposable oldSub)
        {
            oldSub.Dispose();
            item.SetValue(SubscriptionProperty, null);
        }
    }

    private static readonly AttachedProperty<IDisposable?> SubscriptionProperty =
        AvaloniaProperty.RegisterAttached<TreeViewItemBehavior, TreeViewItem, IDisposable?>("Subscription");
}
