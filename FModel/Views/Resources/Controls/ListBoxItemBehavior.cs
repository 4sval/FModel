using System.Windows;
using System.Windows.Controls;

namespace FModel.Views.Resources.Controls;

public sealed class ListBoxItemBehavior
{
    public static bool GetIsBroughtIntoViewWhenSelected(ListBoxItem listBoxItem)
    {
        return (bool) listBoxItem.GetValue(IsBroughtIntoViewWhenSelectedProperty);
    }

    public static void SetIsBroughtIntoViewWhenSelected(ListBoxItem listBoxItem, bool value)
    {
        listBoxItem.SetValue(IsBroughtIntoViewWhenSelectedProperty, value);
    }

    public static readonly DependencyProperty IsBroughtIntoViewWhenSelectedProperty =
        DependencyProperty.RegisterAttached("IsBroughtIntoViewWhenSelected", typeof(bool), typeof(ListBoxItemBehavior),
            new UIPropertyMetadata(false, OnIsBroughtIntoViewWhenSelectedChanged));

    private static void OnIsBroughtIntoViewWhenSelectedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
    {
        if (depObj is not ListBoxItem item)
            return;

        if (e.NewValue is not bool value)
            return;

        if (value)
            item.Selected += OnListBoxItemSelected;
        else
            item.Selected -= OnListBoxItemSelected;
    }

    private static void OnListBoxItemSelected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is ListBoxItem item)
            item.BringIntoView();
    }
}

