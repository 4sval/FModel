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
    public bool IsStateMachineState { get; set; }
    public bool IsEntryNode { get; set; }
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

/// <summary>
/// Represents a layer/sub-graph within the animation blueprint,
/// similar to how UE's Animation Blueprint editor organizes nodes
/// into separate tabs (AnimGraph, StateMachine sub-graphs, etc.).
/// </summary>
public class AnimGraphLayer
{
    public string Name { get; set; } = string.Empty;
    public List<AnimGraphNode> Nodes { get; } = [];
    public List<AnimGraphConnection> Connections { get; } = [];
}

/// <summary>
/// Holds metadata extracted from BakedStateMachines for building
/// state machine overview layers (Entry + State nodes + Transition connections).
/// </summary>
internal class StateMachineMetadata
{
    public string MachineName { get; init; } = string.Empty;
    public List<string> StateNames { get; } = [];
    public List<(int PreviousState, int NextState)> Transitions { get; } = [];
}

public class AnimGraphViewModel
{
    private const int GridColumns = 4;
    private const int NodeHorizontalSpacing = 300;
    private const int NodeVerticalSpacing = 200;
    private const int StateNodeHorizontalSpacing = 250;
    private const int StateNodeVerticalSpacing = 150;
    private const int MaxPropertyValueDisplayLength = 100;
    internal const string SubGraphPathSeparator = " > ";

    public string PackageName { get; set; } = string.Empty;
    public List<AnimGraphNode> Nodes { get; } = [];
    public List<AnimGraphConnection> Connections { get; } = [];
    public List<AnimGraphLayer> Layers { get; } = [];

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
        foreach (var (propName, structType) in animNodeProps)
        {
            var node = new AnimGraphNode
            {
                Name = propName,
                ExportType = structType
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
        }

        // Resolve connections between nodes using CDO property values
        if (cdo != null)
        {
            ResolveConnections(cdo, animNodeProps, nodeByName, vm);
        }

        // Associate state machine nodes with their baked machine names
        // and collect state machine metadata for overview layers
        var smMetadata = new List<StateMachineMetadata>();
        AssociateStateMachineNames(animBlueprintClass, cdo, animNodeProps, nodeByName, smMetadata);

        // Group nodes into layers (connected subgraphs)
        BuildLayers(vm);

        // Prefix state machine internal layers with their parent path to avoid name collisions
        PrefixStateMachineLayerNames(vm);

        // Build state machine overview layers (Entry + State nodes + Transitions)
        BuildStateMachineOverviewLayers(vm, smMetadata);

        return vm;
    }

    /// <summary>
    /// Groups nodes into layers by finding connected components in the graph.
    /// Each connected component becomes a separate layer/tab, named after
    /// its most prominent node (Root, StateMachine, etc.).
    /// </summary>
    private static void BuildLayers(AnimGraphViewModel vm)
    {
        if (vm.Nodes.Count == 0) return;

        // Build adjacency sets (undirected) for connected component detection
        var adjacency = new Dictionary<AnimGraphNode, HashSet<AnimGraphNode>>();
        foreach (var node in vm.Nodes)
            adjacency[node] = [];

        foreach (var conn in vm.Connections)
        {
            adjacency[conn.SourceNode].Add(conn.TargetNode);
            adjacency[conn.TargetNode].Add(conn.SourceNode);
        }

        // Find connected components via BFS
        var visited = new HashSet<AnimGraphNode>();
        var components = new List<List<AnimGraphNode>>();

        foreach (var node in vm.Nodes)
        {
            if (visited.Contains(node)) continue;

            var component = new List<AnimGraphNode>();
            var queue = new Queue<AnimGraphNode>();
            queue.Enqueue(node);
            visited.Add(node);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                component.Add(current);

                foreach (var neighbor in adjacency[current])
                {
                    if (visited.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }

            components.Add(component);
        }

        // Create a layer for each connected component
        var layerIndex = 0;
        foreach (var component in components)
        {
            var componentSet = new HashSet<AnimGraphNode>(component);
            var layerName = GetLayerName(component, layerIndex);

            var layer = new AnimGraphLayer { Name = layerName };
            layer.Nodes.AddRange(component);

            // Add only the connections that belong to this component
            foreach (var conn in vm.Connections)
            {
                if (componentSet.Contains(conn.SourceNode) && componentSet.Contains(conn.TargetNode))
                    layer.Connections.Add(conn);
            }

            // Layout nodes within this layer in a grid
            LayoutLayerNodes(layer);

            vm.Layers.Add(layer);
            layerIndex++;
        }
    }

    /// <summary>
    /// Renames state machine internal layers with a parent path prefix
    /// (e.g., "AnimGraph > Locomotion") to avoid name collisions with
    /// linked anim layer sub-graphs that may share the same base name.
    /// </summary>
    private static void PrefixStateMachineLayerNames(AnimGraphViewModel vm)
    {
        // Map: machineName → parent layer name (where the StateMachine node lives)
        var smParentLayer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in vm.Layers)
        {
            foreach (var node in layer.Nodes)
            {
                if (node.AdditionalProperties.TryGetValue("StateMachineName", out var machineName))
                    smParentLayer.TryAdd(machineName, layer.Name);
            }
        }

        // Rename layers whose nodes belong to a state machine
        foreach (var layer in vm.Layers)
        {
            var smName = string.Empty;
            foreach (var node in layer.Nodes)
            {
                if (node.AdditionalProperties.TryGetValue("BelongsToStateMachine", out var val) &&
                    !string.IsNullOrEmpty(val))
                {
                    smName = val;
                    break;
                }
            }

            if (string.IsNullOrEmpty(smName))
                continue;

            if (smParentLayer.TryGetValue(smName, out var parentName))
                layer.Name = $"{parentName}{SubGraphPathSeparator}{smName}";
        }
    }

    /// <summary>
    /// Creates state machine overview layers with synthetic Entry + State nodes
    /// and transition connections between states, providing a UE-like state machine
    /// editor view. The overview layer is named with the path prefix to match
    /// double-click navigation from StateMachine nodes.
    /// </summary>
    private static void BuildStateMachineOverviewLayers(AnimGraphViewModel vm, List<StateMachineMetadata> smMetadata)
    {
        // Map: machineName → parent layer name (where the StateMachine node lives)
        var smParentLayer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in vm.Layers)
        {
            foreach (var node in layer.Nodes)
            {
                if (node.AdditionalProperties.TryGetValue("StateMachineName", out var machineName))
                    smParentLayer.TryAdd(machineName, layer.Name);
            }
        }

        foreach (var sm in smMetadata)
        {
            if (sm.StateNames.Count == 0) continue;

            // Determine the path-prefixed layer name
            var parentName = smParentLayer.GetValueOrDefault(sm.MachineName, "AnimGraph");
            var overviewLayerName = $"{parentName}{SubGraphPathSeparator}{sm.MachineName}";

            // Remove existing internal layers with this name (they'll be replaced by the overview)
            vm.Layers.RemoveAll(l => l.Name.Equals(overviewLayerName, StringComparison.OrdinalIgnoreCase));

            var overviewLayer = new AnimGraphLayer { Name = overviewLayerName };
            var stateNodes = new List<AnimGraphNode>();

            // Create Entry node
            var entryNode = new AnimGraphNode
            {
                Name = "Entry",
                ExportType = "Entry",
                IsEntryNode = true
            };
            entryNode.Pins.Add(new AnimGraphPin
            {
                PinName = "Output",
                IsOutput = true,
                PinType = "transition",
                OwnerNode = entryNode
            });
            overviewLayer.Nodes.Add(entryNode);

            // Create State nodes
            for (var i = 0; i < sm.StateNames.Count; i++)
            {
                var stateNode = new AnimGraphNode
                {
                    Name = sm.StateNames[i],
                    ExportType = "State",
                    IsStateMachineState = true
                };
                stateNode.Pins.Add(new AnimGraphPin
                {
                    PinName = "In",
                    IsOutput = false,
                    PinType = "transition",
                    OwnerNode = stateNode
                });
                stateNode.Pins.Add(new AnimGraphPin
                {
                    PinName = "Out",
                    IsOutput = true,
                    PinType = "transition",
                    OwnerNode = stateNode
                });
                stateNodes.Add(stateNode);
                overviewLayer.Nodes.Add(stateNode);
            }

            // Entry connects to first state (state index 0)
            if (stateNodes.Count > 0)
            {
                overviewLayer.Connections.Add(new AnimGraphConnection
                {
                    SourceNode = entryNode,
                    SourcePinName = "Output",
                    TargetNode = stateNodes[0],
                    TargetPinName = "In"
                });
            }

            // Transition connections between states
            foreach (var (prevIdx, nextIdx) in sm.Transitions)
            {
                if (prevIdx < stateNodes.Count && nextIdx < stateNodes.Count)
                {
                    overviewLayer.Connections.Add(new AnimGraphConnection
                    {
                        SourceNode = stateNodes[prevIdx],
                        SourcePinName = "Out",
                        TargetNode = stateNodes[nextIdx],
                        TargetPinName = "In"
                    });
                }
            }

            // Layout state nodes in a grid arrangement
            LayoutStateMachineOverview(overviewLayer, entryNode, stateNodes);

            vm.Layers.Add(overviewLayer);
        }
    }

    /// <summary>
    /// Arranges state machine overview nodes: Entry on the left, state nodes in a grid.
    /// </summary>
    private static void LayoutStateMachineOverview(AnimGraphLayer layer, AnimGraphNode entryNode, List<AnimGraphNode> stateNodes)
    {
        // Place Entry on the far left
        entryNode.NodePosX = 0;
        entryNode.NodePosY = 0;

        if (stateNodes.Count == 0) return;

        // Arrange state nodes in a grid to the right of Entry
        var cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(stateNodes.Count)));
        for (var i = 0; i < stateNodes.Count; i++)
        {
            var col = i % cols;
            var row = i / cols;
            stateNodes[i].NodePosX = StateNodeHorizontalSpacing + col * StateNodeHorizontalSpacing;
            stateNodes[i].NodePosY = row * StateNodeVerticalSpacing;
        }

        // Center Entry vertically relative to state nodes
        var maxRow = (stateNodes.Count - 1) / cols;
        entryNode.NodePosY = maxRow * StateNodeVerticalSpacing / 2;
    }

    /// <summary>
    /// Determines a display name for a layer based on the types of nodes it contains.
    /// A Root node's "Name" property defines the layer/sub-graph name
    /// (e.g., "AnimGraph" for the main output pose, or a specific name for LinkedAnimLayer sub-graphs).
    /// </summary>
    private static string GetLayerName(List<AnimGraphNode> nodes, int index)
    {
        var rootNode = nodes.FirstOrDefault(n =>
            n.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase) &&
            n.AdditionalProperties.TryGetValue("Name", out _));
        if (rootNode != null &&
            rootNode.AdditionalProperties.TryGetValue("Name", out var rootName) &&
            !string.IsNullOrEmpty(rootName))
            return rootName;

        // Check if any node belongs to a baked state machine
        var smNode = nodes.FirstOrDefault(n =>
            n.AdditionalProperties.TryGetValue("BelongsToStateMachine", out _));
        if (smNode != null &&
            smNode.AdditionalProperties.TryGetValue("BelongsToStateMachine", out var smName) &&
            !string.IsNullOrEmpty(smName))
            return smName;

        var stateMachine = nodes.FirstOrDefault(n =>
            n.ExportType.Contains("StateMachine", StringComparison.OrdinalIgnoreCase));
        if (stateMachine != null)
            return $"StateMachine ({stateMachine.Name})";

        var blend = nodes.FirstOrDefault(n =>
            n.ExportType.Contains("Blend", StringComparison.OrdinalIgnoreCase));
        if (blend != null)
            return $"Blend ({blend.Name})";

        if (nodes.Count == 1)
            return GetShortTypeName(nodes[0].ExportType);

        return $"Layer {index}";
    }

    private static string GetShortTypeName(string exportType)
    {
        if (exportType.StartsWith("FAnimNode_"))
            return exportType["FAnimNode_".Length..];
        if (exportType.StartsWith("AnimNode_"))
            return exportType["AnimNode_".Length..];
        return exportType;
    }

    /// <summary>
    /// Reads BakedStateMachines from the animation blueprint class to associate
    /// FAnimNode_StateMachine nodes with their machine names, mark internal
    /// state root nodes, and collect state/transition metadata for overview layers.
    /// </summary>
    private static void AssociateStateMachineNames(UClass animBlueprintClass, UObject? cdo,
        List<(string name, string structType)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName,
        List<StateMachineMetadata> smMetadata)
    {
        // BakedStateMachines is a UPROPERTY on UAnimBlueprintGeneratedClass
        // Try reading from both the class and CDO
        UScriptArray? bakedMachines = null;
        if (animBlueprintClass.TryGetValue(out UScriptArray classBaked, "BakedStateMachines"))
            bakedMachines = classBaked;
        else if (cdo != null && cdo.TryGetValue(out UScriptArray cdoBaked, "BakedStateMachines"))
            bakedMachines = cdoBaked;

        if (bakedMachines == null || bakedMachines.Properties.Count == 0)
            return;

        for (var machineIdx = 0; machineIdx < bakedMachines.Properties.Count; machineIdx++)
        {
            if (bakedMachines.Properties[machineIdx].GetValue(typeof(FStructFallback)) is not FStructFallback machineStruct)
                continue;

            // Extract MachineName
            var machineName = string.Empty;
            foreach (var prop in machineStruct.Properties)
            {
                if (prop.Name.Text == "MachineName")
                {
                    machineName = prop.Tag?.GenericValue?.ToString() ?? string.Empty;
                    break;
                }
            }
            if (string.IsNullOrEmpty(machineName))
                continue;

            // Associate FAnimNode_StateMachine nodes that reference this machine index
            var machineIdxStr = machineIdx.ToString();
            foreach (var (propName, structType) in animNodeProps)
            {
                if (!structType.Contains("StateMachine", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!nodeByName.TryGetValue(propName, out var smNode))
                    continue;
                if (!smNode.AdditionalProperties.TryGetValue("StateMachineIndexInClass", out var idxStr))
                    continue;
                if (idxStr == machineIdxStr)
                    smNode.AdditionalProperties["StateMachineName"] = machineName;
            }

            var metadata = new StateMachineMetadata { MachineName = machineName };

            // Extract state names and mark root nodes with BelongsToStateMachine
            foreach (var prop in machineStruct.Properties)
            {
                if (prop.Name.Text != "States") continue;
                if (prop.Tag?.GenericValue is not UScriptArray states) break;

                for (var stateIdx = 0; stateIdx < states.Properties.Count; stateIdx++)
                {
                    if (states.Properties[stateIdx].GetValue(typeof(FStructFallback)) is not FStructFallback stateStruct)
                    {
                        metadata.StateNames.Add($"State_{stateIdx}");
                        continue;
                    }

                    // Extract state name
                    var stateName = $"State_{stateIdx}";
                    if (stateStruct.TryGetValue(out FName stateNameProp, "StateName"))
                        stateName = stateNameProp.Text;

                    metadata.StateNames.Add(stateName);

                    // Mark root node
                    if (!stateStruct.TryGetValue(out int stateRootIndex, "StateRootNodeIndex"))
                        continue;

                    if (stateRootIndex < 0 || stateRootIndex >= animNodeProps.Count)
                        continue;

                    var rootPropName = animNodeProps[stateRootIndex].name;
                    if (nodeByName.TryGetValue(rootPropName, out var rootNode))
                        rootNode.AdditionalProperties["BelongsToStateMachine"] = machineName;
                }
                break;
            }

            // Extract machine-level transitions (PreviousState → NextState)
            foreach (var prop in machineStruct.Properties)
            {
                if (prop.Name.Text != "Transitions") continue;
                if (prop.Tag?.GenericValue is not UScriptArray transitions) break;

                foreach (var transProp in transitions.Properties)
                {
                    if (transProp.GetValue(typeof(FStructFallback)) is not FStructFallback transStruct)
                        continue;

                    if (!transStruct.TryGetValue(out int previousState, "PreviousState"))
                        continue;
                    if (!transStruct.TryGetValue(out int nextState, "NextState"))
                        continue;

                    if (previousState >= 0 && nextState >= 0 &&
                        previousState < metadata.StateNames.Count && nextState < metadata.StateNames.Count)
                    {
                        metadata.Transitions.Add((previousState, nextState));
                    }
                }
                break;
            }

            smMetadata.Add(metadata);
        }
    }

    /// <summary>
    /// Arranges nodes within a layer in a left-to-right flow layout
    /// based on connection topology (sinks on the left, sources on the right).
    /// </summary>
    private static void LayoutLayerNodes(AnimGraphLayer layer)
    {
        if (layer.Nodes.Count == 0) return;

        // Build directed adjacency: target -> sources (who feeds into target)
        var incomingEdges = new Dictionary<AnimGraphNode, List<AnimGraphNode>>();
        var outgoingEdges = new Dictionary<AnimGraphNode, List<AnimGraphNode>>();
        foreach (var node in layer.Nodes)
        {
            incomingEdges[node] = [];
            outgoingEdges[node] = [];
        }

        foreach (var conn in layer.Connections)
        {
            // SourceNode's output feeds into TargetNode's input
            outgoingEdges[conn.SourceNode].Add(conn.TargetNode);
            incomingEdges[conn.TargetNode].Add(conn.SourceNode);
        }

        // Topological sort to assign depth levels (longest path from leaves)
        var depth = new Dictionary<AnimGraphNode, int>();
        var layerSet = new HashSet<AnimGraphNode>(layer.Nodes);

        // Find sink nodes (nodes with no outgoing edges within this layer)
        var sinkNodes = layer.Nodes.Where(n => outgoingEdges[n].Count == 0).ToList();

        // BFS from sinks to assign depth
        foreach (var node in layer.Nodes)
            depth[node] = 0;

        var queue = new Queue<AnimGraphNode>();
        foreach (var sink in sinkNodes)
        {
            queue.Enqueue(sink);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var source in incomingEdges[current])
            {
                var newDepth = depth[current] + 1;
                if (newDepth > depth[source])
                {
                    depth[source] = newDepth;
                    queue.Enqueue(source);
                }
            }
        }

        // Group by depth level and assign positions
        var maxDepth = depth.Values.DefaultIfEmpty(0).Max();
        var nodesAtDepth = new Dictionary<int, List<AnimGraphNode>>();
        foreach (var (node, d) in depth)
        {
            if (!nodesAtDepth.TryGetValue(d, out var list))
                nodesAtDepth[d] = list = [];
            list.Add(node);
        }

        // Position: sources (high depth) on the right, sinks (depth 0) on the left
        for (var d = 0; d <= maxDepth; d++)
        {
            if (!nodesAtDepth.TryGetValue(d, out var nodesInColumn)) continue;

            var x = (maxDepth - d) * NodeHorizontalSpacing;
            for (var i = 0; i < nodesInColumn.Count; i++)
            {
                nodesInColumn[i].NodePosX = x;
                nodesInColumn[i].NodePosY = i * NodeVerticalSpacing;
            }
        }
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
            var value = prop.Tag?.GenericValue?.ToString() ?? string.Empty;

            switch (name)
            {
                case "NodeComment":
                    node.NodeComment = value;
                    break;
                default:
                    // Store additional properties for display
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
        // Handle arrays of pose links (e.g., BlendPose TArray<FPoseLink>)
        if (tag.GenericValue is UScriptArray array)
        {
            for (var i = 0; i < array.Properties.Count; i++)
            {
                TryResolvePoseLink(array.Properties[i], $"{pinName}[{i}]", sourceNode, animNodeProps, nodeByName, vm);
            }
            return;
        }

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
