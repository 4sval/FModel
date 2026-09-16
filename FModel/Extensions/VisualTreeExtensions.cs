using System.Windows;
using System.Windows.Media;

namespace FModel.Extensions;

public static class VisualTreeExtensions
{
    public static T FindAncestor<T>(this DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T t)
                return t;
            current = current is FrameworkContentElement contentElement
                ? contentElement.Parent
                : VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
