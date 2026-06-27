using System.Windows;
using System.Windows.Controls;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls;

public sealed class TypeDataTemplateSelector : DataTemplateSelector
{
    public object FolderTemplateKey { get; set; }
    public object FileTemplateKey { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (container is not FrameworkElement element)
            return base.SelectTemplate(item, container);

        var key = item switch
        {
            TreeItem => FolderTemplateKey,
            GameFileViewModel => FileTemplateKey,
            _ => null
        };

        return key is not null
            ? element.TryFindResource(key) as DataTemplate
            : base.SelectTemplate(item, container);
    }
}
