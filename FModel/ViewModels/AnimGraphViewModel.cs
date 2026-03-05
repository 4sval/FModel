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
    /// <summary>
    /// Additional properties for state machine transitions (e.g. CrossfadeDuration, LogicType).
    /// </summary>
    public Dictionary<string, string> TransitionProperties { get; set; } = new();
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
    public List<string> StateRootPropNames { get; } = [];
    public List<(int PreviousState, int NextState, Dictionary<string, string> Properties)> Transitions { get; } = [];
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
    /// <summary>
    /// Animation blueprint graph layers, each defined by a unique AnimGraphNode_Root.
    /// </summary>
    public List<AnimGraphLayer> Layers { get; } = [];
    /// <summary>
    /// State machine state sub-graphs, keyed by the _StateResult root node's property name
    /// (derived from StateRootNodeIndex) for unique identification.
    /// </summary>
    public Dictionary<string, AnimGraphLayer> StateSubGraphs { get; } = new();

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
    /// Groups nodes into layers based on root nodes. In Unreal Engine:
    /// - Each animation blueprint layer has a unique AnimGraphNode_Root
    /// - Each state machine state sub-graph has a unique AnimGraphNode_StateResult
    /// These two types are processed in separate passes to keep graph layers
    /// and state machine state sub-graphs independent.
    /// </summary>
    private static void BuildLayers(AnimGraphViewModel vm)
    {
        if (vm.Nodes.Count == 0) return;

        // Build upstream map: for each node, which nodes feed into it
        // Connection direction: SourceNode (provider) → TargetNode (consumer)
        var upstreamOf = new Dictionary<AnimGraphNode, List<AnimGraphNode>>();
        foreach (var node in vm.Nodes)
            upstreamOf[node] = [];

        foreach (var conn in vm.Connections)
            upstreamOf[conn.TargetNode].Add(conn.SourceNode);

        var assigned = new HashSet<AnimGraphNode>();
        var layerIndex = 0;

        // Pass 1: Build graph layers from AnimGraphNode_Root nodes.
        // Each _Root node defines an animation blueprint layer (e.g. "AnimGraph").
        var graphRoots = vm.Nodes
            .Where(n => n.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase))
            .ToList();

        AnimGraphLayer? primaryGraphLayer = null;
        foreach (var rootNode in graphRoots)
        {
            if (!assigned.Add(rootNode)) continue;
            var layerNodes = CollectUpstream(rootNode, upstreamOf, assigned);
            AddLayer(vm, layerNodes, layerIndex++);
            primaryGraphLayer ??= vm.Layers[^1];
        }

        // Determine the outermost animation blueprint layer for fallback.
        // In UE, the outermost layer is always named "AnimGraph".
        // If found, use it; otherwise fall back to the first _Root layer.
        var outermostGraphLayer = vm.Layers.FirstOrDefault(l =>
            l.Name.Equals("AnimGraph", StringComparison.OrdinalIgnoreCase)) ?? primaryGraphLayer;

        // Pass 2: Build state machine state sub-graphs from AnimGraphNode_StateResult nodes.
        // Each _StateResult node defines a state's sub-graph within a state machine.
        // These are stored in StateSubGraphs keyed by the root node's property name.
        // SaveCachedPose nodes are excluded because they belong to the parent AnimGraph layer.
        var stateResultRoots = vm.Nodes
            .Where(n => n.ExportType.EndsWith("_StateResult", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var stateResultNode in stateResultRoots)
        {
            if (!assigned.Add(stateResultNode)) continue;
            var layerNodes = CollectUpstream(stateResultNode, upstreamOf, assigned,
                excludeNode: IsSaveCachedPoseNode);
            AddStateSubGraph(vm, layerNodes, stateResultNode.Name, layerIndex++);
        }

        // Pass 3: Assign unassigned SaveCachedPose nodes to their correct _Root layer.
        // SaveCachedPose can only exist in animation blueprint layers (defined by _Root nodes).
        // Determine the correct _Root layer by tracing downstream UseCachedPose consumers
        // back through the state machine hierarchy to their parent animation blueprint layer.
        // Each SaveCachedPose's upstream chain excludes other SaveCachedPose nodes so that
        // chained SaveCachedPose → UseCachedPose → SaveCachedPose are independently placed.
        //
        // When chains exist (e.g. UseCachedPose(A) → … → SaveCachedPose(B) →
        // UseCachedPose(B) → … → SaveCachedPose(C)), the UseCachedPose consumers of
        // upstream SaveCachedPose nodes may only appear in a layer after the downstream
        // SaveCachedPose is processed. We therefore iterate: each pass rebuilds lookups
        // and resolves nodes whose consumers are already placed, deferring the rest.
        if (outermostGraphLayer != null)
        {
            var pending = vm.Nodes
                .Where(n => !assigned.Contains(n) && IsSaveCachedPoseNode(n))
                .ToList();

            if (pending.Count > 0)
            {
                var affectedLayers = new HashSet<AnimGraphLayer>();

                bool madeProgress;
                var maxPasses = pending.Count;
                do
                {
                    madeProgress = false;
                    var lookups = BuildLayerLookups(vm);

                    for (var i = pending.Count - 1; i >= 0; i--)
                    {
                        var saveNode = pending[i];
                        var targetLayer = FindOwnerRootLayer(saveNode, vm, lookups);

                        // If no layer was found, check whether the failure is because
                        // some consumer nodes are not yet in any layer (deferred
                        // dependency). If so, skip this node and retry next pass.
                        if (targetLayer == null)
                        {
                            var hasDeferredConsumer = false;
                            foreach (var conn in vm.Connections)
                            {
                                if (conn.SourceNode == saveNode &&
                                    !lookups.NodeToLayer.ContainsKey(conn.TargetNode))
                                {
                                    hasDeferredConsumer = true;
                                    break;
                                }
                            }

                            if (hasDeferredConsumer)
                                continue;

                            targetLayer = outermostGraphLayer;
                        }

                        assigned.Add(saveNode);
                        var inputChain = CollectUpstream(saveNode, upstreamOf, assigned,
                            excludeNode: IsSaveCachedPoseNode);
                        targetLayer.Nodes.AddRange(inputChain);
                        affectedLayers.Add(targetLayer);
                        pending.RemoveAt(i);
                        madeProgress = true;
                    }
                } while (madeProgress && pending.Count > 0 && --maxPasses > 0);

                // Fallback: remaining nodes (circular deps or truly unresolvable)
                foreach (var saveNode in pending)
                {
                    if (!assigned.Add(saveNode)) continue;
                    var inputChain = CollectUpstream(saveNode, upstreamOf, assigned,
                        excludeNode: IsSaveCachedPoseNode);
                    outermostGraphLayer.Nodes.AddRange(inputChain);
                    affectedLayers.Add(outermostGraphLayer);
                }

                foreach (var layer in affectedLayers)
                    RebuildLayerConnections(vm, layer);
            }
        }

        // Fallback: any remaining unassigned nodes go into connected-component layers
        var remaining = vm.Nodes.Where(n => !assigned.Contains(n)).ToList();
        if (remaining.Count > 0)
        {
            var adjacency = new Dictionary<AnimGraphNode, HashSet<AnimGraphNode>>();
            foreach (var node in remaining)
                adjacency[node] = [];

            foreach (var conn in vm.Connections)
            {
                if (adjacency.ContainsKey(conn.SourceNode) && adjacency.ContainsKey(conn.TargetNode))
                {
                    adjacency[conn.SourceNode].Add(conn.TargetNode);
                    adjacency[conn.TargetNode].Add(conn.SourceNode);
                }
            }

            foreach (var node in remaining)
            {
                if (!assigned.Add(node)) continue;

                var component = new List<AnimGraphNode> { node };
                var queue = new Queue<AnimGraphNode>();
                queue.Enqueue(node);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var neighbor in adjacency[current])
                    {
                        if (assigned.Add(neighbor))
                        {
                            component.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                AddLayer(vm, component, layerIndex++);
            }
        }

        // Final enforcement: SaveCachedPose nodes can only exist in animation
        // blueprint layers (layers that contain a _Root node). After all passes
        // above, scan every non-_Root layer (state sub-graphs and fallback layers)
        // and move any SaveCachedPose nodes to the correct _Root layer.
        if (outermostGraphLayer != null)
        {
            EnforceSaveCachedPoseInRootLayers(vm, outermostGraphLayer);
        }
    }

    /// <summary>
    /// Ensures every SaveCachedPose node resides in an animation blueprint layer
    /// (one that contains a _Root node). Any SaveCachedPose found in a state
    /// sub-graph or a fallback connected-component layer is moved to the correct
    /// _Root layer determined by tracing its UseCachedPose consumers.
    /// Falls back to <paramref name="fallbackLayer"/> if no better target is found.
    /// </summary>
    private static void EnforceSaveCachedPoseInRootLayers(
        AnimGraphViewModel vm, AnimGraphLayer fallbackLayer)
    {
        // Identify animation-blueprint layers (layers that contain a _Root node)
        var animBlueprintLayers = new HashSet<AnimGraphLayer>();
        foreach (var layer in vm.Layers)
        {
            if (layer.Nodes.Any(n => n.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase)))
                animBlueprintLayers.Add(layer);
        }

        // Collect SaveCachedPose nodes from non-_Root layers and state sub-graphs
        var moves = new List<(AnimGraphNode node, AnimGraphLayer sourceLayer)>();

        foreach (var layer in vm.Layers)
        {
            if (animBlueprintLayers.Contains(layer)) continue;
            foreach (var node in layer.Nodes.Where(IsSaveCachedPoseNode).ToList())
                moves.Add((node, layer));
        }

        foreach (var (_, layer) in vm.StateSubGraphs)
        {
            foreach (var node in layer.Nodes.Where(IsSaveCachedPoseNode).ToList())
                moves.Add((node, layer));
        }

        if (moves.Count == 0) return;

        var lookups = BuildLayerLookups(vm);
        var affectedLayers = new HashSet<AnimGraphLayer>();
        foreach (var (node, sourceLayer) in moves)
        {
            var targetLayer = FindOwnerRootLayer(node, vm, lookups) ?? fallbackLayer;
            sourceLayer.Nodes.Remove(node);
            targetLayer.Nodes.Add(node);
            affectedLayers.Add(sourceLayer);
            affectedLayers.Add(targetLayer);
        }

        foreach (var layer in affectedLayers)
            RebuildLayerConnections(vm, layer);
    }

    /// <summary>
    /// Pre-computed lookup maps for layer hierarchy traversal.
    /// </summary>
    private readonly struct LayerLookups(
        Dictionary<AnimGraphNode, AnimGraphLayer> nodeToLayer,
        Dictionary<string, AnimGraphLayer> machineToLayer)
    {
        public Dictionary<AnimGraphNode, AnimGraphLayer> NodeToLayer { get; } = nodeToLayer;
        public Dictionary<string, AnimGraphLayer> MachineToLayer { get; } = machineToLayer;
    }

    /// <summary>
    /// Builds the lookup maps needed by <see cref="FindOwnerRootLayer"/> and
    /// <see cref="GetAncestorRootLayer"/>. Call once and reuse for multiple queries.
    /// </summary>
    private static LayerLookups BuildLayerLookups(AnimGraphViewModel vm)
    {
        var nodeToLayer = new Dictionary<AnimGraphNode, AnimGraphLayer>();
        foreach (var layer in vm.Layers)
            foreach (var node in layer.Nodes)
                nodeToLayer.TryAdd(node, layer);
        foreach (var (_, subGraph) in vm.StateSubGraphs)
            foreach (var node in subGraph.Nodes)
                nodeToLayer.TryAdd(node, subGraph);

        var machineToLayer = new Dictionary<string, AnimGraphLayer>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in vm.Layers)
            foreach (var node in layer.Nodes)
                if (node.AdditionalProperties.TryGetValue("StateMachineName", out var mn))
                    machineToLayer.TryAdd(mn, layer);
        foreach (var (_, subGraph) in vm.StateSubGraphs)
            foreach (var node in subGraph.Nodes)
                if (node.AdditionalProperties.TryGetValue("StateMachineName", out var mn))
                    machineToLayer.TryAdd(mn, subGraph);

        return new LayerLookups(nodeToLayer, machineToLayer);
    }

    /// <summary>
    /// Finds the correct animation blueprint layer (_Root layer) for a SaveCachedPose node
    /// by tracing ALL downstream UseCachedPose consumers through the state machine
    /// hierarchy to find each consumer's ancestor _Root layer. If all consumers trace
    /// to the same _Root layer, that layer is used. If consumers span different _Root
    /// layers, the SaveCachedPose must be placed in the outermost layer (AnimGraph).
    /// Returns null if no suitable layer is found.
    /// </summary>
    private static AnimGraphLayer? FindOwnerRootLayer(
        AnimGraphNode saveNode, AnimGraphViewModel vm, LayerLookups lookups)
    {
        // Collect ancestor _Root layers from ALL consumers
        var consumerRootLayers = new HashSet<AnimGraphLayer>();
        foreach (var conn in vm.Connections)
        {
            if (conn.SourceNode != saveNode) continue;

            if (!lookups.NodeToLayer.TryGetValue(conn.TargetNode, out var consumerLayer))
                continue;

            var rootLayer = GetAncestorRootLayer(consumerLayer, lookups.MachineToLayer);
            if (rootLayer != null)
                consumerRootLayers.Add(rootLayer);
        }

        if (consumerRootLayers.Count == 0)
            return null;

        // If all consumers trace to the same _Root layer, use it
        if (consumerRootLayers.Count == 1)
            return consumerRootLayers.First();

        // Consumers span different _Root layers: the SaveCachedPose must be placed
        // in the outermost animation blueprint layer (AnimGraph) since it needs to
        // be accessible from all consumer layers. Return null to trigger fallback
        // to the outermost layer.
        return null;
    }

    /// <summary>
    /// Walks up the layer hierarchy to find the nearest ancestor animation blueprint
    /// layer (one containing a _Root node). For state sub-graphs, traces through the
    /// BelongsToStateMachine → StateMachineName chain to find the parent layer.
    /// </summary>
    private static AnimGraphLayer? GetAncestorRootLayer(
        AnimGraphLayer layer,
        Dictionary<string, AnimGraphLayer> machineToLayer)
    {
        var visited = new HashSet<AnimGraphLayer>();
        var current = layer;

        while (current != null && visited.Add(current))
        {
            // If this layer contains a _Root node, it's an animation blueprint layer
            if (current.Nodes.Any(n => n.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase)))
                return current;

            // Find the state machine this sub-graph belongs to
            string? smName = null;
            foreach (var node in current.Nodes)
            {
                if (node.AdditionalProperties.TryGetValue("BelongsToStateMachine", out var val) &&
                    !string.IsNullOrEmpty(val))
                {
                    smName = val;
                    break;
                }
            }

            if (string.IsNullOrEmpty(smName) || !machineToLayer.TryGetValue(smName, out current))
                break;
        }

        return null;
    }

    /// <summary>
    /// Rebuilds a layer's connection list from vm.Connections based on which
    /// nodes are currently in the layer, then re-runs layout.
    /// </summary>
    private static void RebuildLayerConnections(AnimGraphViewModel vm, AnimGraphLayer layer)
    {
        var nodeSet = new HashSet<AnimGraphNode>(layer.Nodes);
        layer.Connections.Clear();
        foreach (var conn in vm.Connections)
        {
            if (nodeSet.Contains(conn.SourceNode) && nodeSet.Contains(conn.TargetNode))
                layer.Connections.Add(conn);
        }
        LayoutLayerNodes(layer);
    }

    /// <summary>
    /// Collects a root node and all its upstream providers via BFS.
    /// When <paramref name="excludeNode"/> is provided, matching upstream nodes
    /// are skipped (not collected and their inputs are not traversed).
    /// </summary>
    private static List<AnimGraphNode> CollectUpstream(
        AnimGraphNode root,
        Dictionary<AnimGraphNode, List<AnimGraphNode>> upstreamOf,
        HashSet<AnimGraphNode> assigned,
        Func<AnimGraphNode, bool>? excludeNode = null)
    {
        var nodes = new List<AnimGraphNode> { root };
        var queue = new Queue<AnimGraphNode>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var upstream in upstreamOf[current])
            {
                if (excludeNode != null && excludeNode(upstream))
                    continue;
                if (assigned.Add(upstream))
                {
                    nodes.Add(upstream);
                    queue.Enqueue(upstream);
                }
            }
        }

        return nodes;
    }

    /// <summary>
    /// Creates an <see cref="AnimGraphLayer"/> from a set of nodes, assigns
    /// the relevant connections, lays out the nodes and adds the layer to the VM.
    /// </summary>
    private static void AddLayer(AnimGraphViewModel vm, List<AnimGraphNode> nodes, int index)
    {
        var nodeSet = new HashSet<AnimGraphNode>(nodes);
        var layer = new AnimGraphLayer { Name = GetLayerName(nodes, index) };
        layer.Nodes.AddRange(nodes);

        foreach (var conn in vm.Connections)
        {
            if (nodeSet.Contains(conn.SourceNode) && nodeSet.Contains(conn.TargetNode))
                layer.Connections.Add(conn);
        }

        LayoutLayerNodes(layer);
        vm.Layers.Add(layer);
    }

    /// <summary>
    /// Creates an <see cref="AnimGraphLayer"/> for a state machine state sub-graph
    /// and stores it in <see cref="AnimGraphViewModel.StateSubGraphs"/> keyed by
    /// <paramref name="rootNodePropName"/> (the root node's property name from StateRootNodeIndex).
    /// </summary>
    private static void AddStateSubGraph(AnimGraphViewModel vm, List<AnimGraphNode> nodes, string rootNodePropName, int index)
    {
        var nodeSet = new HashSet<AnimGraphNode>(nodes);
        var layer = new AnimGraphLayer { Name = GetLayerName(nodes, index) };
        layer.Nodes.AddRange(nodes);

        foreach (var conn in vm.Connections)
        {
            if (nodeSet.Contains(conn.SourceNode) && nodeSet.Contains(conn.TargetNode))
                layer.Connections.Add(conn);
        }

        LayoutLayerNodes(layer);
        vm.StateSubGraphs[rootNodePropName] = layer;
    }

    /// <summary>
    /// Renames state machine internal layers with a parent path prefix
    /// (e.g., "AnimGraph > Locomotion" for the overview, or
    /// "AnimGraph > Locomotion > Idle" for per-state sub-graphs).
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

        // Iteratively rename state sub-graphs and discover nested StateMachine nodes.
        // Each pass renames sub-graphs whose parent SM is already known, then registers
        // any nested SM nodes found in the newly-renamed sub-graphs for the next pass.
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var (_, layer) in vm.StateSubGraphs)
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

                if (!smParentLayer.TryGetValue(smName, out var parentName))
                    continue;

                // Per-state layers: use the _StateResult node's Name additional property for display
                var stateResultNode = layer.Nodes.FirstOrDefault(n =>
                    n.ExportType.EndsWith("_StateResult", StringComparison.OrdinalIgnoreCase));
                var stateName = stateResultNode?.AdditionalProperties.GetValueOrDefault("Name") ?? layer.Name;
                var expectedName = $"{parentName}{SubGraphPathSeparator}{smName}{SubGraphPathSeparator}{stateName}";

                if (layer.Name != expectedName)
                {
                    layer.Name = expectedName;
                    changed = true;
                }

                // Register any nested StateMachine nodes within this sub-graph
                foreach (var node in layer.Nodes)
                {
                    if (node.AdditionalProperties.TryGetValue("StateMachineName", out var nestedMachineName))
                    {
                        if (smParentLayer.TryAdd(nestedMachineName, layer.Name))
                            changed = true;
                    }
                }
            }
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
        // Also scan state sub-graphs for nested StateMachine nodes (already renamed by PrefixStateMachineLayerNames)
        foreach (var (_, layer) in vm.StateSubGraphs)
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

            // State sub-graphs are now in StateSubGraphs (no need to remove from Layers)

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
                // Store root node property name for StateRootNodeIndex-based lookup
                if (i < sm.StateRootPropNames.Count && !string.IsNullOrEmpty(sm.StateRootPropNames[i]))
                    stateNode.AdditionalProperties["StateRootNodeName"] = sm.StateRootPropNames[i];
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
            foreach (var (prevIdx, nextIdx, transProps) in sm.Transitions)
            {
                if (prevIdx < stateNodes.Count && nextIdx < stateNodes.Count)
                {
                    var conn = new AnimGraphConnection
                    {
                        SourceNode = stateNodes[prevIdx],
                        SourcePinName = "Out",
                        TargetNode = stateNodes[nextIdx],
                        TargetPinName = "In"
                    };
                    foreach (var (k, v) in transProps)
                        conn.TransitionProperties[k] = v;
                    overviewLayer.Connections.Add(conn);
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
    /// Animation blueprint layers use the _Root node's "Name" property (e.g., "AnimGraph").
    /// State machine state sub-graphs use the _StateResult root node's unique property name
    /// (from StateRootNodeIndex) to avoid duplicate name collisions.
    /// </summary>
    private static string GetLayerName(List<AnimGraphNode> nodes, int index)
    {
        // Animation blueprint layers: use _Root node's Name property
        var rootNode = nodes.FirstOrDefault(n =>
            n.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase) &&
            n.AdditionalProperties.TryGetValue("Name", out _));
        if (rootNode != null &&
            rootNode.AdditionalProperties.TryGetValue("Name", out var rootName) &&
            !string.IsNullOrEmpty(rootName))
            return rootName;

        // State machine state sub-graphs: use the _StateResult root node's property name
        // (unique identifier from StateRootNodeIndex, avoids duplicate state name collisions)
        var stateResultNode = nodes.FirstOrDefault(n =>
            n.ExportType.EndsWith("_StateResult", StringComparison.OrdinalIgnoreCase));
        if (stateResultNode != null)
            return stateResultNode.Name;

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
                        metadata.StateRootPropNames.Add(string.Empty);
                        continue;
                    }

                    // Extract state name
                    var stateName = $"State_{stateIdx}";
                    if (stateStruct.TryGetValue(out FName stateNameProp, "StateName"))
                        stateName = stateNameProp.Text;

                    metadata.StateNames.Add(stateName);

                    // Mark root node via StateRootNodeIndex
                    // UE stores node indices in reverse order relative to ChildProperties,
                    // so the actual index into animNodeProps is (Count - 1 - stateRootIndex).
                    if (!stateStruct.TryGetValue(out int stateRootIndex, "StateRootNodeIndex") ||
                        stateRootIndex < 0 || stateRootIndex >= animNodeProps.Count)
                    {
                        metadata.StateRootPropNames.Add(string.Empty);
                        continue;
                    }

                    var mappedIndex = animNodeProps.Count - 1 - stateRootIndex;
                    var rootPropName = animNodeProps[mappedIndex].name;
                    metadata.StateRootPropNames.Add(rootPropName);
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
                        var transProps = new Dictionary<string, string>();
                        foreach (var tp in transStruct.Properties)
                        {
                            var name = tp.Name.Text;
                            if (name is "PreviousState" or "NextState") continue;
                            var val = tp.Tag?.GenericValue?.ToString();
                            if (!string.IsNullOrEmpty(val))
                                transProps[name] = val.Length > MaxPropertyValueDisplayLength
                                    ? val[..MaxPropertyValueDisplayLength] + "…" : val;
                        }
                        metadata.Transitions.Add((previousState, nextState, transProps));
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

    private static bool IsSaveCachedPoseNode(AnimGraphNode node)
    {
        return node.ExportType.Contains("SaveCachedPose", StringComparison.OrdinalIgnoreCase) ||
               node.Name.Contains("SaveCachedPose", StringComparison.OrdinalIgnoreCase);
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
