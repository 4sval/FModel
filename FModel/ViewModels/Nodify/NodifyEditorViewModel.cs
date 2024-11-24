using System.Collections.ObjectModel;
using System.Windows;
using CUE4Parse.UE4.Assets;
using FModel.Framework;

namespace FModel.ViewModels.Nodify;

public class NodifyEditorViewModel : ViewModel
{
    public NodifyEditorViewModel()
    {
        Nodes
            .WhenAdded(n => n.Graph = this)
            .WhenCleared(_ => Connections.Clear());
    }

    public NodifyEditorViewModel(IPackage package) : this()
    {
        var root = new PackagedNodeViewModel(package);
        Nodes.Add(root);
    }

    // private void AddToCollection(NodeViewModel parent, NodeViewModel node)
    // {
    //     parent.Children.Add(Nodes.Count);
    //     Nodes.Add(node);
    // }
    //
    // private void AddPropertyToNode(NodeViewModel parent, IPropertyHolder holder) => AddPropertyToNode(parent, holder.Properties);
    // private void AddPropertyToNode(NodeViewModel parent, List<FPropertyTag> properties)
    // {
    //     var node = new NodeViewModel { Title = "Properties", Depth = parent.Depth + 1 };
    //     properties.ForEach(p => AddPropertyToNode(parent, node, p.Name.ToString(), p.Tag, p.TagData));
    //     AddToCollection(parent, node);
    // }
    //
    // private void AddPropertyToNode(NodeViewModel parent, List<FPropertyTagType> properties, FPropertyTagData type)
    // {
    //     var node = new ArrayNodeViewModel { Title = "Properties", Depth = parent.Depth + 1, ArraySize = properties.Count };
    //     AddPropertyToNode(parent, node, "In", properties[0], type);
    //     AddToCollection(parent, node);
    // }
    //
    // private void AddPropertyToNode(NodeViewModel parent, NodeViewModel node, string name, FPropertyTagType tag, FPropertyTagData type)
    // {
    //     switch (tag)
    //     {
    //         case FPropertyTagType<FScriptStruct> { Value: not null } s:
    //             switch (s.Value.StructType)
    //             {
    //                 case FStructFallback fallback:
    //                     if (node.Input.Count == 0)
    //                     {
    //                         // node.Output.RemoveAt(node.Output.Count - 1);
    //                         fallback.Properties.ForEach(p => AddPropertyToNode(parent, node, p.Name.ToString(), p.Tag, p.TagData));
    //                     }
    //                     else
    //                     {
    //                         AddPropertyToNode(node, fallback);
    //                     }
    //                     break;
    //                 default:
    //                     var fields = s.Value.StructType.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
    //                     AddPropertyToNode(node, fields.Select(field => new FPropertyTag(field, s.Value.StructType)).ToList());
    //                     break;
    //             }
    //             break;
    //         case FPropertyTagType<UScriptArray> { Value.Properties.Count: > 0 } a:
    //             AddPropertyToNode(node, a.Value.Properties, a.Value.InnerTagData);
    //             break;
    //         // case FPropertyTagType<FPackageIndex> { Value: not null } i:
    //         //     // special node that if clicked will open the asset
    //         //     break;
    //         // case FPropertyTagType<FSoftObjectPath> s:
    //         //     // special node that if clicked will open the asset
    //         //     break;
    //         case { } t:
    //             AddPropertyToNode(node, name, t.GenericValue);
    //             break;
    //         default:
    //             AddPropertyToNode(node, name, tag);
    //             break;
    //     }
    //
    //     if (parent.Output.Count > 0 && node.Input.Count > 0)
    //     {
    //         Connections.Add(new ConnectionViewModel(parent.Output[^1], node.Input[^1]));
    //     }
    // }
    //
    // private void AddPropertyToNode(NodeViewModel node, string name, object? property)
    // {
    //     node.AddProperty(name, property?.ToString() ?? "null");
    // }
    //
    // private const int MarginX = 64;
    // private const int MarginY = 16;
    //
    // public void OrganizeNodes() => OrganizeNodes(Nodes[^1]);
    // private void OrganizeNodes(NodeViewModel root)
    // {
    //     FirstPass(root);
    //     AssignFinalPositions(root);
    // }
    //
    // private double currentLeafY = 0;
    //
    // private void FirstPass(NodeViewModel node, double x = 0)
    // {
    //     node.X = x;
    //
    //     if (node.Children.Count == 0)
    //     {
    //         node.Y = currentLeafY;
    //         currentLeafY += node.ActualSize.Height + MarginY;
    //     }
    //     else
    //     {
    //         foreach (var child in node.Children)
    //         {
    //             FirstPass(Nodes[child], node.X + node.ActualSize.Width + MarginX);
    //         }
    //
    //         var leftmost = Nodes[node.Children[0]];
    //         var rightmost = Nodes[node.Children[^1]];
    //         node.Y = (leftmost.Y + rightmost.Y) / 2;
    //     }
    // }
    //
    // private void AssignFinalPositions(NodeViewModel node)
    // {
    //     node.Location = new Point((int)node.X, (int)node.Y);
    //     foreach (var child in node.Children)
    //     {
    //         AssignFinalPositions(Nodes[child]);
    //     }
    // }

    private NodifyObservableCollection<NodeViewModel> _nodes = new();
    public NodifyObservableCollection<NodeViewModel> Nodes
    {
        get => _nodes;
        set => SetProperty(ref _nodes, value);
    }

    private ObservableCollection<NodeViewModel> _selectedNodes = new();
    public ObservableCollection<NodeViewModel> SelectedNodes
    {
        get => _selectedNodes;
        set => SetProperty(ref _selectedNodes, value);
    }

    private NodifyObservableCollection<ConnectionViewModel> _connections = new();
    public NodifyObservableCollection<ConnectionViewModel> Connections
    {
        get => _connections;
        set => SetProperty(ref _connections, value);
    }

    private ObservableCollection<ConnectionViewModel> _selectedConnections = new();
    public ObservableCollection<ConnectionViewModel> SelectedConnections
    {
        get => _selectedConnections;
        set => SetProperty(ref _selectedConnections, value);
    }

    private ConnectionViewModel? _selectedConnection;
    public ConnectionViewModel? SelectedConnection
    {
        get => _selectedConnection;
        set => SetProperty(ref _selectedConnection, value);
    }

    private NodeViewModel? _selectedNode;
    public NodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set => SetProperty(ref _selectedNode, value);
    }

    private Size _viewportSize;
    public Size ViewportSize
    {
        get => _viewportSize;
        set => SetProperty(ref _viewportSize, value);
    }

    private Point _viewportLocation;
    public Point ViewportLocation
    {
        get => _viewportLocation;
        set => SetProperty(ref _viewportLocation, value);
    }

    private double _viewportZoom;
    public double ViewportZoom
    {
        get => _viewportZoom;
        set => SetProperty(ref _viewportZoom, value);
    }
}
