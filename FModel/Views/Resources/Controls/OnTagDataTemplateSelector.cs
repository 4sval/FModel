using System.Windows;
using System.Windows.Controls;

namespace FModel.Views.Resources.Controls
{
    public class OnTagDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not string s || container is not FrameworkElement f) return null;
            return f.FindResource(s) as DataTemplate;
        }
    }
}