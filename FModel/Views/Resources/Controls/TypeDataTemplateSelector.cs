using System.Windows;
using System.Windows.Controls;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls;

public class TypeDataTemplateSelector : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            TreeItem when container is FrameworkElement f => f.FindResource("TiledFolderDataTemplate") as DataTemplate,
            GameFileViewModel when container is FrameworkElement f => f.FindResource("TiledFileDataTemplate") as DataTemplate,
            _ => base.SelectTemplate(item, container)
        };
    }
}
