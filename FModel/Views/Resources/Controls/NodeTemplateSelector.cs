using System.Windows;
using System.Windows.Controls;
using FModel.ViewModels.Nodify;

namespace FModel.Views.Resources.Controls;

public class NodeTemplateSelector : DataTemplateSelector
{
    public DataTemplate DefaultTemplate { get; set; }
    public DataTemplate FlowNodeTemplate { get; set; }
    public DataTemplate IndexedTemplate { get; set; }
    public DataTemplate PackagedTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            PackagedNodeViewModel => PackagedTemplate,
            BaseIndexedNodeViewModel => IndexedTemplate,
            FlowNodeViewModel => FlowNodeTemplate,
            _ => DefaultTemplate
        };
    }
}
