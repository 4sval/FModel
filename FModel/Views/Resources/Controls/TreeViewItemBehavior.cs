using System.Windows;
using System.Windows.Controls;

namespace FModel.Views.Resources.Controls
{
    public sealed class TreeViewItemBehavior
    {
        public static bool GetIsBroughtIntoViewWhenSelected(TreeViewItem treeViewItem)
        {
            return (bool) treeViewItem.GetValue(IsBroughtIntoViewWhenSelectedProperty);
        }

        public static void SetIsBroughtIntoViewWhenSelected(TreeViewItem treeViewItem, bool value)
        {
            treeViewItem.SetValue(IsBroughtIntoViewWhenSelectedProperty, value);
        }

        public static readonly DependencyProperty IsBroughtIntoViewWhenSelectedProperty =
            DependencyProperty.RegisterAttached("IsBroughtIntoViewWhenSelected", typeof(bool), typeof(TreeViewItemBehavior),
                new UIPropertyMetadata(false, OnIsBroughtIntoViewWhenSelectedChanged));

        private static void OnIsBroughtIntoViewWhenSelectedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            if (depObj is not TreeViewItem item)
                return;

            if (e.NewValue is not bool value)
                return;

            if (value)
                item.Selected += OnTreeViewItemSelected;
            else
                item.Selected -= OnTreeViewItemSelected;
        }

        private static void OnTreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem item)
                item.BringIntoView();
        }
    }
}