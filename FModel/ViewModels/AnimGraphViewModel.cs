using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.UObject;

namespace FModel.ViewModels;

public class AnimGraphNode
{
    public string Name { get; set; } = string.Empty;
    public string ExportType { get; set; } = string.Empty;
    public string NodeComment { get; set; } = string.Empty;
    public int NodePosX { get; set; }
    public int NodePosY { get; set; }
    public List<AnimGraphPin> Pins { get; set; } = [];
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();

    public override string ToString() => $"{ExportType} ({Name})";
}

public class AnimGraphPin
{
    public string PinName { get; set; } = string.Empty;
    public bool IsOutput { get; set; }
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
    private const int GridColumns = 4;
    private const int NodeHorizontalSpacing = 300;
    private const int NodeVerticalSpacing = 200;
    private const int MaxPropertyValueDisplayLength = 100;

    public string PackageName { get; set; } = string.Empty;
    public List<AnimGraphNode> Nodes { get; } = [];
    public List<AnimGraphConnection> Connections { get; } = [];

    /// <summary>
    /// Extracts animation graph node information from a UAnimBlueprintGeneratedClass.
    /// In cooked assets, graph nodes (UEdGraphNode) are stripped as editor-only data.
    /// The actual animation node data is stored in:
    /// - ChildProperties (FField[]) on the class: describes the struct property types (e.g., FAnimNode_StateMachine)
    /// - ClassDefaultObject properties: contains the actual struct values (FStructFallback) with node data
    /// </summary>
    public static AnimGraphViewModel ExtractFromClass(UClass animBlueprintClass)
    {
        var vm = new AnimGraphViewModel { PackageName = animBlueprintClass.Owner?.Name ?? animBlueprintClass.Name };

        // Load the ClassDefaultObject which contains the actual property values
        var cdo = animBlueprintClass.ClassDefaultObject.Load();

        // Extract animation node properties from ChildProperties metadata
        // and their corresponding values from the CDO
        var childProps = animBlueprintClass.ChildProperties;
        if (childProps == null || childProps.Length == 0)
            return vm;

        // Collect all anim node struct properties from the class definition
        var animNodeProps = new List<(string name, string structType)>();
        foreach (var field in childProps)
        {
            if (field is not FStructProperty structProp) continue;

            var structName = structProp.Struct.ResolvedObject?.Name.Text ?? string.Empty;
            // Animation node structs typically start with "FAnimNode_" or "AnimNode_"
            if (!IsAnimNodeStruct(structName) && !IsAnimNodeStruct(field.Name.Text))
                continue;

            animNodeProps.Add((field.Name.Text, structName));
        }

        // Build nodes from the collected properties
        var nodeByName = new Dictionary<string, AnimGraphNode>();
        var nodeIndex = 0;
        foreach (var (propName, structType) in animNodeProps)
        {
            var node = new AnimGraphNode
            {
                Name = propName,
                ExportType = structType,
                NodePosX = nodeIndex % GridColumns * NodeHorizontalSpacing,
                NodePosY = nodeIndex / GridColumns * NodeVerticalSpacing
            };

            // Try to extract property values from the CDO
            if (cdo != null)
            {
                ExtractNodeProperties(cdo, propName, node);
            }

            // Add a default output pin for each node
            node.Pins.Add(new AnimGraphPin
            {
                PinName = "Output",
                IsOutput = true,
                PinType = "pose",
                OwnerNode = node
            });

            nodeByName[propName] = node;
            vm.Nodes.Add(node);
            nodeIndex++;
        }

        // Resolve connections between nodes using CDO property values
        if (cdo != null)
        {
            ResolveConnections(cdo, animNodeProps, nodeByName, vm);
        }

        return vm;
    }

    private static bool IsAnimNodeStruct(string name)
    {
        return name.StartsWith("FAnimNode_", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("AnimNode_", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("AnimGraphNode_", StringComparison.OrdinalIgnoreCase);
    }

    private static void ExtractNodeProperties(UObject cdo, string propName, AnimGraphNode node)
    {
        // Try to get the struct fallback value for this node property
        if (!cdo.TryGetValue(out FStructFallback structValue, propName))
            return;

        // Extract useful properties from the struct
        foreach (var prop in structValue.Properties)
        {
            var name = prop.Name.Text;
            var value = prop.Tag?.ToString() ?? string.Empty;

            switch (name)
            {
                case "NodeComment":
                    node.NodeComment = value;
                    break;
                default:
                    // Store additional properties for display in tooltip
                    if (value.Length <= MaxPropertyValueDisplayLength)
                        node.AdditionalProperties[name] = value;
                    break;
            }
        }

        // Add input pins based on struct properties that reference other poses/nodes
        foreach (var prop in structValue.Properties)
        {
            var name = prop.Name.Text;

            // Properties referencing other animation poses are connections
            if (IsPoseProperty(name) || IsLinkedNodeProperty(name))
            {
                node.Pins.Add(new AnimGraphPin
                {
                    PinName = name,
                    IsOutput = false,
                    PinType = "pose",
                    OwnerNode = node
                });
            }
        }
    }

    private static bool IsPoseProperty(string name)
    {
        return name.Contains("Pose", StringComparison.OrdinalIgnoreCase) &&
               !name.Contains("PoseSnapshot", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLinkedNodeProperty(string name)
    {
        return name.Equals("BasePose", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("InputPose", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("SourcePose", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("ComponentPose", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("LinkedAnimGraph", StringComparison.OrdinalIgnoreCase);
    }

    private static void ResolveConnections(UObject cdo, List<(string name, string structType)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName, AnimGraphViewModel vm)
    {
        // Animation node connections in cooked assets are encoded via
        // FPoseLink / FComponentSpacePoseLink struct properties within each node.
        // These contain a "LinkID" integer that maps to the index of the target node
        // in the class's animation node property list.

        foreach (var (propName, _) in animNodeProps)
        {
            if (!cdo.TryGetValue(out FStructFallback structValue, propName))
                continue;

            if (!nodeByName.TryGetValue(propName, out var sourceNode))
                continue;

            foreach (var prop in structValue.Properties)
            {
                var tag = prop.Tag;
                if (tag == null) continue;

                // Check if this property is a pose link (FPoseLink or FComponentSpacePoseLink)
                TryResolvePoseLink(tag, prop.Name.Text, sourceNode, animNodeProps, nodeByName, vm);
            }
        }
    }

    private static void TryResolvePoseLink(FPropertyTagType tag, string pinName,
        AnimGraphNode sourceNode, List<(string name, string structType)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName, AnimGraphViewModel vm)
    {
        // A PoseLink/ComponentSpacePoseLink is a struct with a LinkID property
        if (tag.GetValue(typeof(FStructFallback)) is not FStructFallback linkStruct)
            return;

        if (!linkStruct.TryGetValue(out int linkId, "LinkID"))
            return;

        // LinkID of -1 means not connected
        if (linkId < 0 || linkId >= animNodeProps.Count)
            return;

        var targetPropName = animNodeProps[linkId].name;

        // Avoid self-connections
        if (targetPropName == sourceNode.Name) return;

        if (!nodeByName.TryGetValue(targetPropName, out var targetNode))
            return;

        vm.Connections.Add(new AnimGraphConnection
        {
            SourceNode = targetNode,
            SourcePinName = "Output",
            TargetNode = sourceNode,
            TargetPinName = pinName
        });
    }
}
