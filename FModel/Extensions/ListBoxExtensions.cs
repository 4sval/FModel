using System.Windows.Controls;

namespace FModel.Extensions;

public static class ListBoxExtensions
{
    public static ListBoxItem RevealItem(this ListBox list, object item, bool alignToTop = false)
    {
        var index = list.Items.IndexOf(item);
        if (index < 0)
            return null;

        list.ApplyTemplate();
        list.UpdateLayout();
        if (alignToTop && list.FindVisualChild<ScrollViewer>() is { } scroll)
        {
            scroll.ScrollToVerticalOffset(index);
            list.UpdateLayout();
        }

        if (list.FindVisualChild<VirtualizingPanel>() is { } panel)
        {
            panel.BringIndexIntoViewPublic(index);
        }
        else
        {
            list.ScrollIntoView(item);
        }

        list.UpdateLayout();
        var container = list.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
        container?.BringIntoView();
        list.UpdateLayout();
        return container;
    }
}
