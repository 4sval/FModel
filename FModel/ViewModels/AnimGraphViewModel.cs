using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.EdGraph;
using CUE4Parse.UE4.Objects.Engine.EdGraph;

namespace FModel.ViewModels;

public class AnimGraphNode
{
    public string Name { get; set; } = string.Empty;
    public string ExportType { get; set; } = string.Empty;
    public string NodeComment { get; set; } = string.Empty;
    public int NodePosX { get; set; }
    public int NodePosY { get; set; }
    public List<AnimGraphPin> Pins { get; set; } = [];

    public override string ToString() => $"{ExportType} ({Name})";
}

public class AnimGraphPin
{
    public string PinName { get; set; } = string.Empty;
    public EEdGraphPinDirection Direction { get; set; }
    public string PinType { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public AnimGraphNode OwnerNode { get; set; } = null!;
}

public class AnimGraphConnection
{
    public AnimGraphNode SourceNode { get; set; } = null!;
    public string SourcePinName { get; set; } = string.Empty;
    public AnimGraphNode TargetNode { get; set; } = null!;
    public string TargetPinName { get; set; } = string.Empty;
}

public class AnimGraphViewModel
{
    public string PackageName { get; set; } = string.Empty;
    public List<AnimGraphNode> Nodes { get; } = [];
    public List<AnimGraphConnection> Connections { get; } = [];

    public static AnimGraphViewModel ExtractFromPackage(IPackage package)
    {
        var vm = new AnimGraphViewModel { PackageName = package.Name };

        var allExports = package.GetExports().ToList();

        // Map from UObject to AnimGraphNode for connection resolution
        var nodeMap = new Dictionary<UObject, AnimGraphNode>();

        // First pass: extract all graph nodes
        foreach (var export in allExports)
        {
            if (export is not UEdGraphNode graphNode) continue;

            var node = new AnimGraphNode
            {
                Name = graphNode.Name,
                ExportType = graphNode.ExportType,
                NodePosX = graphNode.GetOrDefault("NodePosX", 0),
                NodePosY = graphNode.GetOrDefault("NodePosY", 0),
                NodeComment = graphNode.GetOrDefault<string>("NodeComment", string.Empty)
            };

            // Extract pins
            foreach (var pinRef in graphNode.Pins)
            {
                if (pinRef is not UEdGraphPin pin) continue;

                var graphPin = new AnimGraphPin
                {
                    PinName = pin.PinName.Text ?? string.Empty,
                    Direction = pin.Direction,
                    PinType = pin.PinType?.PinCategory.Text ?? string.Empty,
                    DefaultValue = pin.DefaultValue ?? string.Empty,
                    OwnerNode = node
                };
                node.Pins.Add(graphPin);
            }

            nodeMap[graphNode] = node;
            vm.Nodes.Add(node);
        }

        // Second pass: resolve connections via LinkedTo references
        foreach (var export in allExports)
        {
            if (export is not UEdGraphNode graphNode) continue;
            if (!nodeMap.TryGetValue(graphNode, out var sourceNode)) continue;

            foreach (var pinRef in graphNode.Pins)
            {
                if (pinRef is not UEdGraphPin pin) continue;

                foreach (var linkedRef in pin.LinkedTo)
                {
                    if (linkedRef == null) continue;

                    // Resolve the owning node of the linked pin
                    var resolvedObject = linkedRef.OwningNode.ResolvedObject;
                    var linkedNodeObj = resolvedObject?.Object?.Value;
                    if (linkedNodeObj == null || !nodeMap.TryGetValue(linkedNodeObj, out var targetNode)) continue;

                    // Only add connection from output to input to avoid duplicates
                    if (pin.Direction != EEdGraphPinDirection.EGPD_Output) continue;

                    // Try to find the target pin name
                    var targetPinName = string.Empty;
                    if (linkedNodeObj is UEdGraphNode linkedGraphNode)
                    {
                        foreach (var tp in linkedGraphNode.Pins)
                        {
                            if (tp is UEdGraphPin targetPin && targetPin.PinId == linkedRef.PinId)
                            {
                                targetPinName = targetPin.PinName.Text ?? string.Empty;
                                break;
                            }
                        }
                    }

                    vm.Connections.Add(new AnimGraphConnection
                    {
                        SourceNode = sourceNode,
                        SourcePinName = pin.PinName.Text ?? string.Empty,
                        TargetNode = targetNode,
                        TargetPinName = targetPinName
                    });
                }
            }
        }

        return vm;
    }
}
