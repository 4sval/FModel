using System.Collections.Generic;
using System.Windows.Controls;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Framework;

namespace FModel.ViewModels.Nodify;

public class FlowNodeViewModel : NodeViewModel
{
    private string? _title;
    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public NodifyObservableCollection<ConnectorViewModel> Input { get; } = new();
    public NodifyObservableCollection<ConnectorViewModel> Output { get; } = new();

    public FlowNodeViewModel()
    {
        void Clear(IList<ConnectorViewModel> connectors)
        {
            foreach (var connector in connectors)
            {
                connector.Connections.Clear();
            }
        }

        Input.WhenAdded(c =>
        {
            c.Flow = ConnectorFlow.Input;
            c.Node = this;
        }).WhenCleared(Clear);
        Output.WhenAdded(c =>
        {
            c.Flow = ConnectorFlow.Output;
            c.Node = this;
        }).WhenCleared(Clear);

        Orientation = Orientation.Horizontal;
    }

    public FlowNodeViewModel(string title) : this()
    {
        Title = title;
    }

    protected void ConnectOutput(FPropertyTagType? tag)
    {
        var bSameNode = this is IndexedNodeViewModel { Output.Count: 1 };
        FlowNodeViewModel node = null;

        switch (tag)
        {
            case FPropertyTagType<FScriptStruct> { Value: not null } s:
                switch (s.Value.StructType)
                {
                    case FStructFallback fallback:
                        node = bSameNode ? this : new FlowNodeViewModel("Properties");
                        foreach (var property in fallback.Properties)
                        {
                            node.Input.Add(new ConnectorViewModel(property.Name.Text));
                            node.Output.Add(new ConnectorViewModel(property.TagData));
                            node.ConnectOutput(property.Tag);
                        }
                        break;
                }
                break;
            case FPropertyTagType<UScriptArray> { Value.Properties.Count: > 0 } a:
                node = new IndexedNodeViewModel(a.Value.Properties, this) { Title = "Properties" };
                node.Input.Add(new ConnectorViewModel("In"));
                node.Output.Add(new ConnectorViewModel(a.Value.InnerTagData));
                break;
            case FPropertyTagType<FPackageIndex> { Value.IsNull: false } p:
                node = bSameNode ? this : new FlowNodeViewModel("Properties");
                node.Input.Add(new ConnectorViewModel("In"));
                node.Output.Add(new ConnectorViewModel(p.Value.Index.ToString()));
                break;
            case FPropertyTagType<FSoftObjectPath> s:
                node = bSameNode ? this : new FlowNodeViewModel("Properties");
                node.Input.Add(new ConnectorViewModel("In"));
                node.Output.Add(new ConnectorViewModel(s.Value.ToString()));
                break;
            case { } t:
                if (Output.Count == 0) Output.Add(new ConnectorViewModel());
                Output[^1].Title = t.GenericValue?.ToString();
                break;
        }

        node?.PostConnectOutput(this);
    }

    protected virtual void PostConnectOutput(FlowNodeViewModel parent)
    {
        parent.Children.Add(this);
        if (Input.Count > 0 && parent.Output.Count > 0)
        {
            parent.Output[^1].Connections.Add(new ConnectionViewModel(Input[^1]));
        }
    }
}
