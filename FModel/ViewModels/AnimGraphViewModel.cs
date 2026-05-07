using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;

namespace FModel.ViewModels;

public class AnimGraphNode
{
    public AnimGraphNode CanonicalNode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExportType { get; set; } = string.Empty;
    public FAnimNodePropertyCollection ResolvedProperties { get; set; } = null!;
    public int AnimNodePropertyIndex { get; set; } = -1;
    public int ChildPropertyIndex { get; set; } = -1;
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
    public int? LinkIndex { get; set; }
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
    public bool IsFunctionLayer { get; set; }
    public bool IsCombinedGraph { get; set; }
    public List<AnimGraphNode> Nodes { get; } = [];
    public List<AnimGraphConnection> Connections { get; } = [];

    public override string ToString() => Name;
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
    public List<bool> StateIsConduit { get; } = [];
    public List<int> StateRootNodeIndices { get; } = [];
    public List<int> EntryRuleNodeIndices { get; } = [];
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
    public AnimGraphLayer FullGraphLayer { get; private set; }
    /// <summary>
    /// Animation blueprint graph layers, each defined by a unique AnimGraphNode_Root.
    /// </summary>
    public List<AnimGraphLayer> Layers { get; } = [];
    public IEnumerable<AnimGraphLayer> FunctionLayers => Layers.Where(static layer => layer.IsFunctionLayer);
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
        var animBlueprintGeneratedClass = animBlueprintClass as UAnimBlueprintGeneratedClass;

        // Load the ClassDefaultObject which contains the actual property values
        var cdo = animBlueprintClass.ClassDefaultObject.Load();

        // Extract animation node properties from ChildProperties metadata
        // and their corresponding values from the CDO
        var childProps = animBlueprintClass.ChildProperties;
        if (childProps == null || childProps.Length == 0)
            return vm;

        var cdoProperties = cdo.Properties;

        // Collect all anim node struct properties from the class definition
        var animNodeProps = animBlueprintGeneratedClass is { AnimNodePropertyData.Length: > 0 }
            ? new List<(string name, string structType, int childPropertyIndex)>(
                animBlueprintGeneratedClass.AnimNodePropertyData.Select(data =>
                    (data.Name, data.StructName, data.ChildPropertyIndex)))
            : new List<(string name, string structType, int childPropertyIndex)>();

        if (animNodeProps.Count == 0)
        {
            for (var childPropertyIndex = 0; childPropertyIndex < childProps.Length; childPropertyIndex++)
            {
                var field = childProps[childPropertyIndex];
                if (field is not FStructProperty structProp) continue;

                var structName = structProp.Struct.ResolvedObject?.Name.Text ?? string.Empty;
                // Prefer Unreal's real type hierarchy over naming conventions: animation
                // nodes derive from AnimNode_Base even when plugin/vendor prefixes hide the
                // usual AnimNode_/AnimGraphNode_ name markers. Keep a name-based fallback
                // only for assets whose struct metadata cannot be resolved.
                if (!IsAnimNodeStruct(structProp, field.Name.Text))
                    continue;

                animNodeProps.Add((field.Name.Text, structName, childPropertyIndex));
            }
        }

        // Build nodes from the collected properties
        var nodeByName = new Dictionary<string, AnimGraphNode>();
        for (var animNodePropertyIndex = 0; animNodePropertyIndex < animNodeProps.Count; animNodePropertyIndex++)
        {
            var (propName, structType, childPropertyIndex) = animNodeProps[animNodePropertyIndex];
            var node = new AnimGraphNode
            {
                CanonicalNode = null,
                Name = propName,
                ExportType = structType,
                AnimNodePropertyIndex = animNodePropertyIndex,
                ChildPropertyIndex = childPropertyIndex
            };
            node.CanonicalNode = node;

            // Try to extract property values from the CDO
            if (cdo != null)
            {
                ExtractNodeProperties(cdo, propName, node);
            }

            if (animBlueprintGeneratedClass != null &&
                animBlueprintGeneratedClass.TryGetAnimNodeProperties(animNodePropertyIndex, out var resolvedProperties))
            {
                node.ResolvedProperties = resolvedProperties;
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
        AssociateStateMachineNames(animBlueprintClass, cdo, childProps.Length, animNodeProps, nodeByName, smMetadata);

        // Group nodes into layers (connected subgraphs)
        BuildLayers(animBlueprintClass, cdo, vm, childProps.Length, animNodeProps, nodeByName);

        // Prefix state machine internal layers with their parent path to avoid name collisions
        PrefixStateMachineLayerNames(vm);

        // Build state machine overview layers (Entry + State nodes + Transitions)
        BuildStateMachineOverviewLayers(vm, smMetadata);

        // Build a single recursively-expanded graph starting from AnimGraph roots.
        BuildFullGraphLayer(vm);

        return vm;
    }

    /// <summary>
    /// Groups nodes into layers based on root nodes. In Unreal Engine:
    /// - Each animation blueprint layer is surfaced by a UFunction whose first
    ///   OutParm PoseLink stores the layer root node index
    /// - The referenced node is typically an AnimGraphNode_Root
    /// - Each state machine state sub-graph has a unique AnimGraphNode_StateResult
    /// These two types are processed in separate passes to keep graph layers
    /// and state machine state sub-graphs independent.
    /// </summary>
    private static void BuildLayers(UClass animBlueprintClass, UObject cdo,
        AnimGraphViewModel vm, int childPropertyCount,
        List<(string name, string structType, int childPropertyIndex)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName)
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

        // Pass 1: Build graph layers from UFunction OutParm PoseLink roots.
        // In cooked anim blueprints, each public graph/layer function points at its
        // root node through the first PoseLink OutParm. Fall back to scanning _Root
        // nodes only when that metadata cannot be resolved.
        var resolvedGraphRoots = ResolveGraphLayerRoots(animBlueprintClass, cdo,
            childPropertyCount, animNodeProps, nodeByName);
        var hasFunctionBackedRoots = resolvedGraphRoots.Count > 0;

        var graphRoots = resolvedGraphRoots;
        if (graphRoots.Count == 0)
        {
            var graphLayerFunctionNames = GetGraphLayerFunctionNames(animBlueprintClass);
            graphRoots = vm.Nodes
                .Where(n => n.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase))
                .Select(n => (rootNode: n, layerName: InferGraphLayerName(n, upstreamOf, graphLayerFunctionNames)))
                .OrderBy(static entry => entry.layerName.Equals("AnimGraph", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(static entry => entry.layerName, StringComparer.Ordinal)
                .ToList();
        }

        AnimGraphLayer primaryGraphLayer = null;
        foreach (var (rootNode, layerName) in graphRoots)
        {
            if (!assigned.Add(rootNode)) continue;
            var layerNodes = CollectUpstream(rootNode, upstreamOf, assigned);
            AddLayer(vm, layerNodes, layerIndex++, layerName, hasFunctionBackedRoots);
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

    private static List<(AnimGraphNode rootNode, string layerName)> ResolveGraphLayerRoots(
        UClass animBlueprintClass, UObject cdo, int childPropertyCount,
        List<(string name, string structType, int childPropertyIndex)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName)
    {
        if (animBlueprintClass is UAnimBlueprintGeneratedClass animBlueprintGeneratedClass)
        {
            var generatedClassRoots = ResolveGraphLayerRoots(animBlueprintGeneratedClass, nodeByName);
            if (generatedClassRoots.Count > 0)
                return generatedClassRoots;
        }

        var roots = new List<(AnimGraphNode rootNode, string layerName)>();
        var seenNodes = new HashSet<AnimGraphNode>();

        foreach (var (functionName, functionIndex) in animBlueprintClass.FuncMap)
        {
            if (!functionIndex.TryLoad(out var export) || export is not UFunction function)
                continue;

            if (!TryGetFirstOutParmPoseProperty(function, out var outParmName))
                continue;

            var rootNode = ResolveFunctionRootNode(functionName.Text, outParmName,
                cdo, childPropertyCount, animNodeProps, nodeByName);
            if (rootNode == null || !seenNodes.Add(rootNode))
                continue;

            rootNode.AdditionalProperties["LayerName"] = functionName.Text;
            roots.Add((rootNode, functionName.Text));
        }

        return roots;
    }

    private static List<(AnimGraphNode rootNode, string layerName)> ResolveGraphLayerRoots(
        UAnimBlueprintGeneratedClass animBlueprintGeneratedClass,
        Dictionary<string, AnimGraphNode> nodeByName)
    {
        var roots = new List<(AnimGraphNode rootNode, string layerName)>();
        var seenNodes = new HashSet<AnimGraphNode>();

        foreach (var function in animBlueprintGeneratedClass.AnimBlueprintFunctions)
        {
            if (!function.HasOutputPose ||
                !animBlueprintGeneratedClass.TryGetRootNodePropertyForFunction(function.Name, out var rootProperty) ||
                rootProperty is null ||
                !nodeByName.TryGetValue(rootProperty.Name, out var rootNode) ||
                !seenNodes.Add(rootNode))
            {
                continue;
            }

            rootNode.AdditionalProperties["LayerName"] = function.Name;
            if (function.OutputPoseLinkID >= 0)
                rootNode.AdditionalProperties["RootLinkID"] = function.OutputPoseLinkID.ToString();

            roots.Add((rootNode, function.Name));
        }

        return roots;
    }

    private static HashSet<string> GetGraphLayerFunctionNames(UClass animBlueprintClass)
    {
        if (animBlueprintClass is UAnimBlueprintGeneratedClass animBlueprintGeneratedClass)
        {
            return animBlueprintGeneratedClass.AnimBlueprintFunctions
                .Where(function => function.HasOutputPose)
                .Select(function => function.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (functionName, functionIndex) in animBlueprintClass.FuncMap)
        {
            if (!functionIndex.TryLoad(out var export) || export is not UFunction function)
                continue;

            if (TryGetFirstOutParmPoseProperty(function, out _))
                names.Add(functionName.Text);
        }

        return names;
    }

    private static string InferGraphLayerName(AnimGraphNode rootNode,
        Dictionary<AnimGraphNode, List<AnimGraphNode>> upstreamOf,
        HashSet<string> graphLayerFunctionNames)
    {
        var layerNodes = CollectConnectedUpstreamNodes(rootNode, upstreamOf);

        var graphNames = layerNodes
            .Where(node => node.ExportType.Contains("LinkedInputPose", StringComparison.OrdinalIgnoreCase) &&
                           node.AdditionalProperties.TryGetValue("Graph", out var graphName) &&
                           graphLayerFunctionNames.Contains(graphName))
            .Select(node => node.AdditionalProperties["Graph"])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (graphNames.Count == 1)
            return graphNames[0];

        if (rootNode.Name.Equals("AnimGraphNode_Root", StringComparison.OrdinalIgnoreCase) &&
            graphLayerFunctionNames.Contains("AnimGraph"))
            return "AnimGraph";

        return rootNode.AdditionalProperties.GetValueOrDefault("LayerName") ?? rootNode.Name;
    }

    private static List<AnimGraphNode> CollectConnectedUpstreamNodes(AnimGraphNode rootNode,
        Dictionary<AnimGraphNode, List<AnimGraphNode>> upstreamOf)
    {
        var visited = new HashSet<AnimGraphNode> { rootNode };
        var nodes = new List<AnimGraphNode> { rootNode };
        var queue = new Queue<AnimGraphNode>();
        queue.Enqueue(rootNode);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var upstream in upstreamOf[current])
            {
                if (!visited.Add(upstream))
                    continue;

                nodes.Add(upstream);
                queue.Enqueue(upstream);
            }
        }

        return nodes;
    }

    private static bool TryGetFirstOutParmPoseProperty(UFunction function, out string outParmName)
    {
        foreach (var childProperty in function.ChildProperties ?? [])
        {
            if (childProperty is not FStructProperty structProp)
                continue;

            if (!structProp.PropertyFlags.HasFlag(EPropertyFlags.OutParm) ||
                structProp.PropertyFlags.HasFlag(EPropertyFlags.ReturnParm) ||
                !IsPoseLinkStruct(structProp))
                continue;

            outParmName = structProp.Name.Text;
            return true;
        }

        outParmName = string.Empty;
        return false;
    }

    private static bool IsPoseLinkStruct(FStructProperty structProp)
    {
        var structName = structProp.Struct.ResolvedObject?.Name.Text ?? string.Empty;
        return structName.Equals("PoseLink", StringComparison.OrdinalIgnoreCase) ||
               structName.Equals("FPoseLink", StringComparison.OrdinalIgnoreCase) ||
               structName.Equals("ComponentSpacePoseLink", StringComparison.OrdinalIgnoreCase) ||
               structName.Equals("FComponentSpacePoseLink", StringComparison.OrdinalIgnoreCase);
    }

    private static AnimGraphNode ResolveFunctionRootNode(string functionName, string outParmName,
        UObject cdo, int childPropertyCount,
        List<(string name, string structType, int childPropertyIndex)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName)
    {
        if (cdo == null)
            return null;

        if (TryResolveFunctionRootNodeFromHolder(cdo, functionName, outParmName,
                childPropertyCount, animNodeProps, nodeByName, out var rootNode))
            return rootNode;

        if (cdo.SerializedSparseClassData != null &&
            TryResolveFunctionRootNodeFromHolder(cdo.SerializedSparseClassData, functionName, outParmName,
                childPropertyCount, animNodeProps, nodeByName, out rootNode))
            return rootNode;

        return null;
    }

    private static bool TryResolveFunctionRootNodeFromHolder(IPropertyHolder holder,
        string functionName, string outParmName, int childPropertyCount,
        List<(string name, string structType, int childPropertyIndex)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName,
        out AnimGraphNode rootNode)
    {
        if (holder.TryGetValue(out FStructFallback functionStruct, functionName) &&
            TryResolveFunctionRootNodeFromStruct(functionStruct, outParmName, childPropertyCount,
                animNodeProps, nodeByName, out rootNode))
            return true;

        if (!outParmName.Equals(functionName, StringComparison.OrdinalIgnoreCase) &&
            holder.TryGetValue(out FStructFallback outParmStruct, outParmName) &&
            TryResolveRootNodeFromPoseLinkStruct(outParmStruct, childPropertyCount,
                animNodeProps, nodeByName, out rootNode))
            return true;

        rootNode = null;
        return false;
    }

    private static bool TryResolveFunctionRootNodeFromStruct(FStructFallback functionStruct,
        string outParmName, int childPropertyCount,
        List<(string name, string structType, int childPropertyIndex)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName,
        out AnimGraphNode rootNode)
    {
        if (functionStruct.TryGetValue(out FStructFallback outParmStruct, outParmName) &&
            TryResolveRootNodeFromPoseLinkStruct(outParmStruct, childPropertyCount,
                animNodeProps, nodeByName, out rootNode))
            return true;

        if (TryResolveRootNodeFromPoseLinkStruct(functionStruct, childPropertyCount,
                animNodeProps, nodeByName, out rootNode))
            return true;

        rootNode = null;
        return false;
    }

    private static bool TryResolveRootNodeFromPoseLinkStruct(FStructFallback poseLinkStruct,
        int childPropertyCount,
        List<(string name, string structType, int childPropertyIndex)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName,
        out AnimGraphNode rootNode)
    {
        rootNode = null;
        if (!poseLinkStruct.TryGetValue(out int rootNodeIndex, "LinkID"))
            return false;

        var rootPropName = ResolveGraphRootPropName(rootNodeIndex, childPropertyCount,
            animNodeProps, nodeByName);
        if (string.IsNullOrEmpty(rootPropName) || !nodeByName.TryGetValue(rootPropName, out rootNode))
            return false;

        return rootNode.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveGraphRootPropName(int rootNodeIndex, int childPropertyCount,
        List<(string name, string structType, int childPropertyIndex)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName)
    {
        var candidates = new List<string>();

        AddCandidateByAnimNodeIndex(rootNodeIndex);
        AddCandidateByChildPropertyIndex(rootNodeIndex);
        AddCandidateByAnimNodeIndex(animNodeProps.Count - 1 - rootNodeIndex);
        AddCandidateByChildPropertyIndex(childPropertyCount - 1 - rootNodeIndex);

        foreach (var candidate in candidates)
        {
            if (nodeByName.TryGetValue(candidate, out var node) &&
                node.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return candidates.FirstOrDefault() ?? string.Empty;

        void AddCandidateByAnimNodeIndex(int animNodeIndex)
        {
            if (animNodeIndex < 0 || animNodeIndex >= animNodeProps.Count)
                return;

            var name = animNodeProps[animNodeIndex].name;
            if (!string.IsNullOrEmpty(name) && !candidates.Any(existing => string.Equals(existing, name, StringComparison.Ordinal)))
                candidates.Add(name);
        }

        void AddCandidateByChildPropertyIndex(int childPropertyIndex)
        {
            if (childPropertyIndex < 0)
                return;

            var match = animNodeProps.FirstOrDefault(prop => prop.childPropertyIndex == childPropertyIndex);
            if (!string.IsNullOrEmpty(match.name) && !candidates.Any(existing => string.Equals(existing, match.name, StringComparison.Ordinal)))
                candidates.Add(match.name);
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
    private static void AddLayer(AnimGraphViewModel vm, List<AnimGraphNode> nodes, int index,
        string layerNameOverride = null, bool isFunctionLayer = false)
    {
        var nodeSet = new HashSet<AnimGraphNode>(nodes);
        var layer = new AnimGraphLayer
        {
            Name = string.IsNullOrWhiteSpace(layerNameOverride) ? GetLayerName(nodes, index) : layerNameOverride,
            IsFunctionLayer = isFunctionLayer
        };
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
        var remainingPasses = Math.Max(1, vm.StateSubGraphs.Count + smParentLayer.Count);
        bool changed = true;
        while (changed && remainingPasses-- > 0)
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
                var stateName = stateResultNode?.AdditionalProperties.GetValueOrDefault("Name");
                if (string.IsNullOrWhiteSpace(stateName))
                    stateName = stateResultNode?.Name;
                if (string.IsNullOrWhiteSpace(stateName))
                    stateName = GetTailSegment(layer.Name);

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

    private static string GetTailSegment(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var separatorIndex = path.LastIndexOf(SubGraphPathSeparator, StringComparison.Ordinal);
        return separatorIndex >= 0 ? path[(separatorIndex + SubGraphPathSeparator.Length)..] : path;
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
                var stateName = sm.StateNames[i];
                var stateNode = new AnimGraphNode
                {
                    Name = stateName,
                    ExportType = i < sm.StateIsConduit.Count && sm.StateIsConduit[i] ? "Conduit" : "State",
                    IsStateMachineState = true
                };
                stateNode.AdditionalProperties["StateMachineName"] = sm.MachineName;
                stateNode.AdditionalProperties["StateSubGraphName"] =
                    $"{overviewLayerName}{SubGraphPathSeparator}{stateName}";
                if (i < sm.StateIsConduit.Count && sm.StateIsConduit[i])
                    stateNode.AdditionalProperties["IsConduit"] = bool.TrueString;
                if (i < sm.StateRootNodeIndices.Count)
                    stateNode.AdditionalProperties["StateRootNodeIndex"] = sm.StateRootNodeIndices[i].ToString();
                if (i < sm.EntryRuleNodeIndices.Count && sm.EntryRuleNodeIndices[i] >= 0)
                    stateNode.AdditionalProperties["EntryRuleNodeIndex"] = sm.EntryRuleNodeIndices[i].ToString();
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
    /// Builds a single recursively-expanded graph that starts from the AnimGraph root,
    /// walks upstream through normal pose connections, and additionally expands
    /// linked animation layers, cached pose links, and state machine state sub-graphs.
    /// The resulting layer uses cloned nodes so the combined layout does not overwrite
    /// the per-layer node positions used elsewhere in the viewer.
    /// </summary>
    private static void BuildFullGraphLayer(AnimGraphViewModel vm)
    {
        var animGraphLayer = vm.Layers.FirstOrDefault(layer =>
            layer.Name.Equals("AnimGraph", StringComparison.OrdinalIgnoreCase))
            ?? vm.FunctionLayers.FirstOrDefault();
        if (animGraphLayer == null)
            return;

        var rootNodes = animGraphLayer.Nodes
            .Where(IsAnimationBlueprintRootNode)
            .ToList();
        if (rootNodes.Count == 0)
            rootNodes = [.. animGraphLayer.Nodes.Take(1)];
        if (rootNodes.Count == 0)
            return;

        var includedNodes = new HashSet<AnimGraphNode>();
        var includedConnections = new HashSet<AnimGraphConnection>();
        var syntheticConnections = new List<AnimGraphConnection>();
        var queuedNodes = new HashSet<AnimGraphNode>(rootNodes);
        var expandedStateMachines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var expandedLinkedLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var incomingByTarget = vm.Connections
            .GroupBy(connection => connection.TargetNode)
            .ToDictionary(group => group.Key, group => group.ToList());

        var queue = new Queue<AnimGraphNode>(rootNodes);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            includedNodes.Add(current);

            if (incomingByTarget.TryGetValue(current, out var incoming))
            {
                foreach (var connection in incoming)
                {
                    includedConnections.Add(connection);
                    if (queuedNodes.Add(connection.SourceNode))
                        queue.Enqueue(connection.SourceNode);
                }
            }

            ExpandStateMachineNode(current);
            ExpandLinkedLayerNode(current);
        }

        var allConnections = includedConnections
            .Concat(syntheticConnections)
            .Where(connection => includedNodes.Contains(connection.SourceNode) && includedNodes.Contains(connection.TargetNode))
            .GroupBy(connection => (connection.SourceNode, connection.SourcePinName, connection.TargetNode, connection.TargetPinName), ReferenceTupleComparer.Instance)
            .Select(static group => group.First())
            .ToList();

        vm.FullGraphLayer = CloneLayerGraph("Full Graph", includedNodes, allConnections, isCombinedGraph: true);

        void ExpandStateMachineNode(AnimGraphNode node)
        {
            if (!node.ExportType.Contains("StateMachine", StringComparison.OrdinalIgnoreCase) ||
                !node.AdditionalProperties.TryGetValue("StateMachineName", out var machineName) ||
                string.IsNullOrEmpty(machineName) ||
                !expandedStateMachines.Add(machineName))
            {
                return;
            }

            foreach (var stateLayer in vm.StateSubGraphs.Values)
            {
                var stateRoot = stateLayer.Nodes.FirstOrDefault(static candidate =>
                    candidate.ExportType.EndsWith("_StateResult", StringComparison.OrdinalIgnoreCase));
                if (stateRoot == null)
                    continue;

                if (!stateRoot.AdditionalProperties.TryGetValue("BelongsToStateMachine", out var ownerMachine) ||
                    !machineName.Equals(ownerMachine, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                includedNodes.Add(stateRoot);
                AddSyntheticConnection(stateRoot, node, $"State:{stateRoot.Name}");
                if (queuedNodes.Add(stateRoot))
                    queue.Enqueue(stateRoot);
            }
        }

        void ExpandLinkedLayerNode(AnimGraphNode node)
        {
            if (!node.ExportType.Contains("LinkedAnimLayer", StringComparison.OrdinalIgnoreCase) &&
                !node.ExportType.Contains("LinkedAnimGraph", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string linkedLayerName = null;
            if (!node.AdditionalProperties.TryGetValue("Layer", out linkedLayerName) || string.IsNullOrEmpty(linkedLayerName))
                node.AdditionalProperties.TryGetValue("Graph", out linkedLayerName);
            if (string.IsNullOrEmpty(linkedLayerName) || !expandedLinkedLayers.Add(linkedLayerName))
                return;

            var linkedLayer = vm.Layers.FirstOrDefault(layer =>
                !layer.IsCombinedGraph &&
                layer.Name.Equals(linkedLayerName, StringComparison.OrdinalIgnoreCase));
            var linkedRoot = linkedLayer?.Nodes.FirstOrDefault(IsAnimationBlueprintRootNode);
            if (linkedRoot == null)
                return;

            includedNodes.Add(linkedRoot);
            AddSyntheticConnection(linkedRoot, node, $"Layer:{linkedLayerName}");
            if (queuedNodes.Add(linkedRoot))
                queue.Enqueue(linkedRoot);
        }

        void AddSyntheticConnection(AnimGraphNode sourceNode, AnimGraphNode targetNode, string targetPinName)
        {
            syntheticConnections.Add(new AnimGraphConnection
            {
                SourceNode = sourceNode,
                SourcePinName = "Output",
                TargetNode = targetNode,
                TargetPinName = targetPinName
            });
        }
    }

    private static AnimGraphLayer CloneLayerGraph(
        string layerName,
        IEnumerable<AnimGraphNode> sourceNodes,
        IEnumerable<AnimGraphConnection> sourceConnections,
        bool isCombinedGraph)
    {
        var cloneMap = new Dictionary<AnimGraphNode, AnimGraphNode>();
        var orderedNodes = sourceNodes
            .OrderBy(static node => node.AnimNodePropertyIndex >= 0 ? 0 : 1)
            .ThenBy(static node => node.AnimNodePropertyIndex)
            .ThenBy(static node => node.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var layer = new AnimGraphLayer
        {
            Name = layerName,
            IsFunctionLayer = true,
            IsCombinedGraph = isCombinedGraph
        };

        foreach (var originalNode in orderedNodes)
        {
            var clone = new AnimGraphNode
            {
                CanonicalNode = originalNode.CanonicalNode ?? originalNode,
                Name = originalNode.Name,
                ExportType = originalNode.ExportType,
                ResolvedProperties = originalNode.ResolvedProperties,
                AnimNodePropertyIndex = originalNode.AnimNodePropertyIndex,
                ChildPropertyIndex = originalNode.ChildPropertyIndex,
                NodeComment = originalNode.NodeComment,
                IsStateMachineState = originalNode.IsStateMachineState,
                IsEntryNode = originalNode.IsEntryNode
            };

            foreach (var (key, value) in originalNode.AdditionalProperties)
                clone.AdditionalProperties[key] = value;

            foreach (var pin in originalNode.Pins)
            {
                clone.Pins.Add(new AnimGraphPin
                {
                    PinName = pin.PinName,
                    IsOutput = pin.IsOutput,
                    PinType = pin.PinType,
                    DefaultValue = pin.DefaultValue,
                    LinkIndex = pin.LinkIndex,
                    OwnerNode = clone
                });
            }

            cloneMap[originalNode] = clone;
            layer.Nodes.Add(clone);
        }

        foreach (var connection in sourceConnections)
        {
            if (!cloneMap.TryGetValue(connection.SourceNode, out var clonedSource) ||
                !cloneMap.TryGetValue(connection.TargetNode, out var clonedTarget))
            {
                continue;
            }

            var clonedConnection = new AnimGraphConnection
            {
                SourceNode = clonedSource,
                SourcePinName = connection.SourcePinName,
                TargetNode = clonedTarget,
                TargetPinName = connection.TargetPinName
            };

            foreach (var (key, value) in connection.TransitionProperties)
                clonedConnection.TransitionProperties[key] = value;

            layer.Connections.Add(clonedConnection);
        }

        LayoutLayerNodes(layer);
        return layer;
    }

    private static bool IsAnimationBlueprintRootNode(AnimGraphNode node)
    {
        return node.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ReferenceTupleComparer : IEqualityComparer<(AnimGraphNode SourceNode, string SourcePinName, AnimGraphNode TargetNode, string TargetPinName)>
    {
        public static ReferenceTupleComparer Instance { get; } = new();

        public bool Equals(
            (AnimGraphNode SourceNode, string SourcePinName, AnimGraphNode TargetNode, string TargetPinName) x,
            (AnimGraphNode SourceNode, string SourcePinName, AnimGraphNode TargetNode, string TargetPinName) y)
        {
            return ReferenceEquals(x.SourceNode, y.SourceNode) &&
                   string.Equals(x.SourcePinName, y.SourcePinName, StringComparison.Ordinal) &&
                   ReferenceEquals(x.TargetNode, y.TargetNode) &&
                   string.Equals(x.TargetPinName, y.TargetPinName, StringComparison.Ordinal);
        }

        public int GetHashCode((AnimGraphNode SourceNode, string SourcePinName, AnimGraphNode TargetNode, string TargetPinName) obj)
        {
            return HashCode.Combine(obj.SourceNode, obj.SourcePinName, obj.TargetNode, obj.TargetPinName);
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

    private static string ResolveStateRootPropName(
        int stateRootIndex,
        int childPropertyCount,
        List<(string name, string structType, int childPropertyIndex)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName)
    {
        var candidates = new List<string>();

        AddCandidateByChildPropertyIndex(childPropertyCount - 1 - stateRootIndex);
        AddCandidateByAnimNodeIndex(animNodeProps.Count - 1 - stateRootIndex);
        AddCandidateByChildPropertyIndex(stateRootIndex);
        AddCandidateByAnimNodeIndex(stateRootIndex);

        foreach (var candidate in candidates)
        {
            if (nodeByName.TryGetValue(candidate, out var node) &&
                node.ExportType.EndsWith("_StateResult", StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return candidates.FirstOrDefault() ?? string.Empty;

        void AddCandidateByChildPropertyIndex(int childPropertyIndex)
        {
            if (childPropertyIndex < 0)
                return;

            var match = animNodeProps.FirstOrDefault(prop => prop.childPropertyIndex == childPropertyIndex);
            if (!string.IsNullOrEmpty(match.name) && !candidates.Any(existing => string.Equals(existing, match.name, StringComparison.Ordinal)))
                candidates.Add(match.name);
        }

        void AddCandidateByAnimNodeIndex(int animNodeIndex)
        {
            if (animNodeIndex < 0 || animNodeIndex >= animNodeProps.Count)
                return;

            var name = animNodeProps[animNodeIndex].name;
            if (!string.IsNullOrEmpty(name) && !candidates.Any(existing => string.Equals(existing, name, StringComparison.Ordinal)))
                candidates.Add(name);
        }
    }

    /// <summary>
    /// Reads BakedStateMachines from the animation blueprint class to associate
    /// FAnimNode_StateMachine nodes with their machine names, mark internal
    /// state root nodes, and collect state/transition metadata for overview layers.
    /// </summary>
    private static void AssociateStateMachineNames(UClass animBlueprintClass, UObject? cdo,
        int childPropertyCount,
        List<(string name, string structType, int childPropertyIndex)> animNodeProps,
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
            foreach (var (propName, structType, _) in animNodeProps)
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
                        metadata.StateIsConduit.Add(false);
                        metadata.StateRootNodeIndices.Add(-1);
                        metadata.EntryRuleNodeIndices.Add(-1);
                        continue;
                    }

                    // Extract state name
                    var stateName = $"State_{stateIdx}";
                    if (stateStruct.TryGetValue(out FName stateNameProp, "StateName"))
                        stateName = stateNameProp.Text;

                    metadata.StateNames.Add(stateName);
                    metadata.StateIsConduit.Add(stateStruct.TryGetValue(out bool isConduit, "bIsAConduit") && isConduit);
                    metadata.EntryRuleNodeIndices.Add(
                        stateStruct.TryGetValue(out int entryRuleNodeIndex, "EntryRuleNodeIndex") ? entryRuleNodeIndex : -1);

                    // Mark root node via StateRootNodeIndex.
                    // Cooked assets do not always expose this index relative to the same base,
                    // so resolve against several candidate index spaces and prefer a StateResult node.
                    if (!stateStruct.TryGetValue(out int stateRootIndex, "StateRootNodeIndex") || stateRootIndex < 0)
                    {
                        metadata.StateRootNodeIndices.Add(-1);
                        metadata.StateRootPropNames.Add(string.Empty);
                        continue;
                    }

                    metadata.StateRootNodeIndices.Add(stateRootIndex);

                    var rootPropName = ResolveStateRootPropName(stateRootIndex, childPropertyCount, animNodeProps, nodeByName);
                    if (string.IsNullOrEmpty(rootPropName))
                    {
                        metadata.StateRootPropNames.Add(string.Empty);
                        continue;
                    }

                    metadata.StateRootPropNames.Add(rootPropName);
                    if (nodeByName.TryGetValue(rootPropName, out var rootNode))
                    {
                        rootNode.AdditionalProperties["BelongsToStateMachine"] = machineName;
                        rootNode.AdditionalProperties["Name"] = stateName;
                    }
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
    /// Arranges nodes within a layer as a compact left-to-right layered graph.
    /// The layout first assigns nodes to the left-most feasible column, then
    /// iteratively reorders each column to reduce edge crossings while keeping
    /// roots/results visually stable.
    /// </summary>
    private static void LayoutLayerNodes(AnimGraphLayer layer)
    {
        if (layer.Nodes.Count == 0) return;

        var originalOrder = layer.Nodes
            .Select((node, index) => (node, index))
            .ToDictionary(static pair => pair.node, static pair => pair.index);

        // Build directed adjacency for the layer-local graph.
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

        // Assign initial depths from sinks so result/root nodes stay on the left.
        var depth = new Dictionary<AnimGraphNode, int>();

        // Find sink nodes (nodes with no outgoing edges within this layer).
        // Cyclic graphs may not have any, so fall back to root-like nodes and then all nodes.
        var sinkNodes = layer.Nodes.Where(n => outgoingEdges[n].Count == 0).ToList();
        if (sinkNodes.Count == 0)
        {
            sinkNodes = layer.Nodes
                .Where(node => IsTerminalGraphNode(node) || incomingEdges[node].Count == 0)
                .ToList();
        }
        if (sinkNodes.Count == 0)
            sinkNodes = layer.Nodes.ToList();

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

        // Left-bias columns where possible: if a node has slack between its
        // downstream constraints and upstream constraints, push it toward the
        // left-most feasible column to shorten long-span edges and reduce crossings.
        var depthAdjusted = true;
        while (depthAdjusted)
        {
            depthAdjusted = false;

            foreach (var node in layer.Nodes.OrderByDescending(node => depth[node]).ThenBy(node => originalOrder[node]))
            {
                if (incomingEdges[node].Count == 0)
                    continue;

                var minimumDepth = outgoingEdges[node].Count == 0
                    ? depth[node]
                    : outgoingEdges[node].Max(target => depth[target]) + 1;
                var maximumDepth = incomingEdges[node].Min(source => depth[source]) - 1;
                var leftBiasedDepth = Math.Max(minimumDepth, maximumDepth);

                if (leftBiasedDepth > depth[node])
                {
                    depth[node] = leftBiasedDepth;
                    depthAdjusted = true;
                }
            }
        }

        // Group the compacted depths into display columns.
        var maxDepth = depth.Values.DefaultIfEmpty(0).Max();
        var nodesAtDepth = new Dictionary<int, List<AnimGraphNode>>();
        foreach (var (node, d) in depth)
        {
            if (!nodesAtDepth.TryGetValue(d, out var list))
                nodesAtDepth[d] = list = [];
            list.Add(node);
        }

        var orderedColumns = new Dictionary<int, List<AnimGraphNode>>();
        for (var d = 0; d <= maxDepth; d++)
        {
            if (!nodesAtDepth.TryGetValue(d, out var nodesInColumn))
                continue;

            orderedColumns[d] = nodesInColumn
                .OrderBy(GetNodePriority)
                .ThenBy(node => originalOrder[node])
                .ToList();
        }

        var connectionList = layer.Connections
            .Where(conn => depth.ContainsKey(conn.SourceNode) && depth.ContainsKey(conn.TargetNode))
            .ToList();
        var incidentConnections = BuildIncidentConnections(layer.Nodes, connectionList);

        // Iterative sweeps: first align nodes to neighboring columns, then
        // locally swap adjacent nodes when doing so reduces measured crossings.
        for (var iteration = 0; iteration < 6; iteration++)
        {
            var beforeSignature = GetColumnSignature(orderedColumns);
            var orderMap = BuildOrderMap(orderedColumns);

            for (var d = 1; d <= maxDepth; d++)
            {
                SortColumnByAnchor(d, orderedColumns, outgoingEdges, incomingEdges, orderMap, originalOrder, preferOutgoing: true);
                orderMap = BuildOrderMap(orderedColumns);
            }

            for (var d = maxDepth - 1; d >= 0; d--)
            {
                SortColumnByAnchor(d, orderedColumns, outgoingEdges, incomingEdges, orderMap, originalOrder, preferOutgoing: false);
                orderMap = BuildOrderMap(orderedColumns);
            }

            var reducedCrossings = false;
            for (var d = 0; d <= maxDepth; d++)
                reducedCrossings |= ReduceCrossingsInColumn(
                    d,
                    orderedColumns,
                    depth,
                    connectionList,
                    incidentConnections,
                    outgoingEdges,
                    incomingEdges,
                    originalOrder);

            var afterSignature = GetColumnSignature(orderedColumns);
            if (!reducedCrossings && string.Equals(beforeSignature, afterSignature, StringComparison.Ordinal))
                break;
        }

        var columnCenter = new Dictionary<int, double>();
        for (var d = 0; d <= maxDepth; d++)
        {
            if (!orderedColumns.TryGetValue(d, out var nodesInColumn)) continue;

            var x = (maxDepth - d) * NodeHorizontalSpacing;
            var desiredPositions = nodesInColumn
                .Select(node => GetDesiredY(node, outgoingEdges[node], incomingEdges[node], columnCenter))
                .ToList();
            var actualPositions = SpreadNodes(desiredPositions, NodeVerticalSpacing);

            for (var i = 0; i < nodesInColumn.Count; i++)
            {
                nodesInColumn[i].NodePosX = x;
                nodesInColumn[i].NodePosY = (int)Math.Round(actualPositions[i]);
            }

            columnCenter[d] = actualPositions.Count == 0 ? 0 : actualPositions.Average();
        }

        var minY = layer.Nodes.Min(static node => node.NodePosY);
        if (minY < 0)
        {
            foreach (var node in layer.Nodes)
                node.NodePosY -= minY;
        }

        static bool IsTerminalGraphNode(AnimGraphNode node)
        {
            return node.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase) ||
                   node.ExportType.EndsWith("_StateResult", StringComparison.OrdinalIgnoreCase) ||
                   node.IsEntryNode;
        }

        static int GetNodePriority(AnimGraphNode node)
        {
            if (node.ExportType.EndsWith("_Root", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (node.ExportType.EndsWith("_StateResult", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (node.IsEntryNode)
                return 2;
            if (node.IsStateMachineState)
                return 3;
            return 10;
        }

        static Dictionary<AnimGraphNode, int> BuildOrderMap(Dictionary<int, List<AnimGraphNode>> orderedColumns)
        {
            var orderMap = new Dictionary<AnimGraphNode, int>();
            foreach (var column in orderedColumns.Values)
            {
                for (var index = 0; index < column.Count; index++)
                    orderMap[column[index]] = index;
            }

            return orderMap;
        }

        static void SortColumnByAnchor(
            int columnDepth,
            Dictionary<int, List<AnimGraphNode>> orderedColumns,
            Dictionary<AnimGraphNode, List<AnimGraphNode>> outgoingEdges,
            Dictionary<AnimGraphNode, List<AnimGraphNode>> incomingEdges,
            Dictionary<AnimGraphNode, int> orderMap,
            Dictionary<AnimGraphNode, int> originalOrder,
            bool preferOutgoing)
        {
            if (!orderedColumns.TryGetValue(columnDepth, out var column) || column.Count <= 1)
                return;

            orderedColumns[columnDepth] = column
                .OrderBy(node => GetNeighborAnchor(node, outgoingEdges[node], incomingEdges[node], orderMap, originalOrder, preferOutgoing))
                .ThenBy(GetNodePriority)
                .ThenBy(node => originalOrder[node])
                .ToList();
        }

        static double GetNeighborAnchor(
            AnimGraphNode node,
            List<AnimGraphNode> outgoing,
            List<AnimGraphNode> incoming,
            Dictionary<AnimGraphNode, int> orderMap,
            Dictionary<AnimGraphNode, int> originalOrder,
            bool preferOutgoing)
        {
            var primary = preferOutgoing ? outgoing : incoming;
            var secondary = preferOutgoing ? incoming : outgoing;

            var positions = primary.Where(orderMap.ContainsKey).Select(neighbor => (double) orderMap[neighbor]).ToList();
            if (positions.Count == 0)
                positions = secondary.Where(orderMap.ContainsKey).Select(neighbor => (double) orderMap[neighbor]).ToList();

            return positions.Count > 0 ? positions.Average() : originalOrder[node];
        }

        static bool ReduceCrossingsInColumn(
            int columnDepth,
            Dictionary<int, List<AnimGraphNode>> orderedColumns,
            Dictionary<AnimGraphNode, int> depth,
            List<AnimGraphConnection> connections,
            Dictionary<AnimGraphNode, List<AnimGraphConnection>> incidentConnections,
            Dictionary<AnimGraphNode, List<AnimGraphNode>> outgoingEdges,
            Dictionary<AnimGraphNode, List<AnimGraphNode>> incomingEdges,
            Dictionary<AnimGraphNode, int> originalOrder)
        {
            if (!orderedColumns.TryGetValue(columnDepth, out var column) || column.Count <= 1)
                return false;

            var improvedAny = false;
            for (var pass = 0; pass < column.Count * 2; pass++)
            {
                var improvedThisPass = false;
                for (var i = 0; i < column.Count - 1; i++)
                {
                    var firstNode = column[i];
                    var secondNode = column[i + 1];
                    var impactedConnections = GetImpactedConnections(firstNode, secondNode, incidentConnections);
                    var currentCrossings = CountCrossingsForConnections(orderedColumns, depth, connections, impactedConnections);
                    var currentPenalty = GetLocalAnchorPenalty(firstNode, secondNode, orderedColumns, outgoingEdges, incomingEdges, originalOrder);

                    (column[i], column[i + 1]) = (column[i + 1], column[i]);

                    var swappedCrossings = CountCrossingsForConnections(orderedColumns, depth, connections, impactedConnections);
                    var swappedPenalty = GetLocalAnchorPenalty(firstNode, secondNode, orderedColumns, outgoingEdges, incomingEdges, originalOrder);

                    if (swappedCrossings < currentCrossings ||
                        (swappedCrossings == currentCrossings && swappedPenalty < currentPenalty))
                    {
                        improvedThisPass = true;
                        improvedAny = true;
                    }
                    else
                    {
                        (column[i], column[i + 1]) = (column[i + 1], column[i]);
                    }
                }

                if (!improvedThisPass)
                    break;
            }

            return improvedAny;
        }

        static double GetLocalAnchorPenalty(
            AnimGraphNode firstNode,
            AnimGraphNode secondNode,
            Dictionary<int, List<AnimGraphNode>> orderedColumns,
            Dictionary<AnimGraphNode, List<AnimGraphNode>> outgoingEdges,
            Dictionary<AnimGraphNode, List<AnimGraphNode>> incomingEdges,
            Dictionary<AnimGraphNode, int> originalOrder)
        {
            var orderMap = BuildOrderMap(orderedColumns);
            return GetAnchorPenalty(firstNode, orderMap, outgoingEdges, incomingEdges, originalOrder) +
                   GetAnchorPenalty(secondNode, orderMap, outgoingEdges, incomingEdges, originalOrder);
        }

        static double GetAnchorPenalty(
            AnimGraphNode node,
            Dictionary<AnimGraphNode, int> orderMap,
            Dictionary<AnimGraphNode, List<AnimGraphNode>> outgoingEdges,
            Dictionary<AnimGraphNode, List<AnimGraphNode>> incomingEdges,
            Dictionary<AnimGraphNode, int> originalOrder)
        {
            var anchor = GetNeighborAnchor(node, outgoingEdges[node], incomingEdges[node], orderMap, originalOrder, preferOutgoing: true);
            return Math.Abs(orderMap[node] - anchor);
        }

        static Dictionary<AnimGraphNode, List<AnimGraphConnection>> BuildIncidentConnections(
            IEnumerable<AnimGraphNode> nodes,
            IEnumerable<AnimGraphConnection> connections)
        {
            var incident = nodes.ToDictionary(static node => node, static _ => new List<AnimGraphConnection>());
            foreach (var connection in connections)
            {
                incident[connection.SourceNode].Add(connection);
                incident[connection.TargetNode].Add(connection);
            }

            return incident;
        }

        static List<AnimGraphConnection> GetImpactedConnections(
            AnimGraphNode firstNode,
            AnimGraphNode secondNode,
            Dictionary<AnimGraphNode, List<AnimGraphConnection>> incidentConnections)
        {
            var impacted = new HashSet<AnimGraphConnection>();
            foreach (var connection in incidentConnections[firstNode])
                impacted.Add(connection);
            foreach (var connection in incidentConnections[secondNode])
                impacted.Add(connection);
            return impacted.ToList();
        }

        static int CountCrossingsForConnections(
            Dictionary<int, List<AnimGraphNode>> orderedColumns,
            Dictionary<AnimGraphNode, int> depth,
            List<AnimGraphConnection> allConnections,
            List<AnimGraphConnection> subjectConnections)
        {
            if (subjectConnections.Count == 0 || allConnections.Count < 2)
                return 0;

            var positions = BuildVirtualPositions(orderedColumns, depth);
            var crossings = 0;
            foreach (var first in subjectConnections)
            {
                if (!TryGetVirtualSegment(first, positions, out var a1, out var a2))
                    continue;

                foreach (var second in allConnections)
                {
                    if (ReferenceEquals(first, second))
                        continue;
                    if (ReferenceEquals(first.SourceNode, second.SourceNode) ||
                        ReferenceEquals(first.SourceNode, second.TargetNode) ||
                        ReferenceEquals(first.TargetNode, second.SourceNode) ||
                        ReferenceEquals(first.TargetNode, second.TargetNode))
                        continue;

                    if (!TryGetVirtualSegment(second, positions, out var b1, out var b2))
                        continue;

                    if (SegmentsIntersectStrict(a1, a2, b1, b2))
                        crossings++;
                }
            }

            return crossings / 2;
        }

        static Dictionary<AnimGraphNode, (double X, double Y)> BuildVirtualPositions(
            Dictionary<int, List<AnimGraphNode>> orderedColumns,
            Dictionary<AnimGraphNode, int> depth)
        {
            var positions = new Dictionary<AnimGraphNode, (double X, double Y)>();
            var maxDepth = depth.Values.DefaultIfEmpty(0).Max();
            foreach (var (columnDepth, column) in orderedColumns)
            {
                var x = maxDepth - columnDepth;
                for (var i = 0; i < column.Count; i++)
                    positions[column[i]] = (x, i);
            }

            return positions;
        }

        static bool TryGetVirtualSegment(
            AnimGraphConnection connection,
            Dictionary<AnimGraphNode, (double X, double Y)> positions,
            out (double X, double Y) start,
            out (double X, double Y) end)
        {
            start = default;
            end = default;
            if (!positions.TryGetValue(connection.SourceNode, out start) ||
                !positions.TryGetValue(connection.TargetNode, out end))
                return false;

            if (Math.Abs(start.X - end.X) < double.Epsilon)
                return false;

            return true;
        }

        static bool SegmentsIntersectStrict(
            (double X, double Y) a1,
            (double X, double Y) a2,
            (double X, double Y) b1,
            (double X, double Y) b2)
        {
            var o1 = Cross(a1, a2, b1);
            var o2 = Cross(a1, a2, b2);
            var o3 = Cross(b1, b2, a1);
            var o4 = Cross(b1, b2, a2);

            return Math.Sign(o1) != Math.Sign(o2) && Math.Sign(o3) != Math.Sign(o4);
        }

        static double Cross((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        static string GetColumnSignature(Dictionary<int, List<AnimGraphNode>> orderedColumns)
        {
            return string.Join("|", orderedColumns
                .OrderBy(static pair => pair.Key)
                .Select(static pair => string.Join(",", pair.Value.Select(node => node.Name))));
        }

        static double GetDesiredY(
            AnimGraphNode node,
            List<AnimGraphNode> outgoing,
            List<AnimGraphNode> incoming,
            Dictionary<int, double> columnCenter)
        {
            var targetYs = outgoing.Select(static neighbor => (double)neighbor.NodePosY).ToList();
            if (targetYs.Count > 0)
                return targetYs.Average();

            var sourceYs = incoming.Select(static neighbor => (double)neighbor.NodePosY).ToList();
            if (sourceYs.Count > 0)
                return sourceYs.Average();

            return columnCenter.Count == 0 ? 0 : columnCenter.Values.Average();
        }

        static List<double> SpreadNodes(List<double> desiredPositions, int spacing)
        {
            if (desiredPositions.Count == 0)
                return [];

            var positions = desiredPositions.ToArray();
            for (var i = 1; i < positions.Length; i++)
            {
                var minAllowed = positions[i - 1] + spacing;
                if (positions[i] < minAllowed)
                    positions[i] = minAllowed;
            }

            var desiredCenter = desiredPositions.Average();
            var actualCenter = positions.Average();
            var shift = desiredCenter - actualCenter;
            for (var i = 0; i < positions.Length; i++)
                positions[i] += shift;

            return positions.ToList();
        }
    }

    private static bool IsAnimNodeStruct(FStructProperty structProp, string fieldName)
    {
        var structType = structProp.Struct.Load<UStruct>();
        if (structType != null && InheritsFromAnimNodeBase(structType))
            return true;

        var structName = structProp.Struct.ResolvedObject?.Name.Text ?? string.Empty;
        return IsAnimNodeStructName(structName) || IsAnimNodeStructName(fieldName);
    }

    private static bool InheritsFromAnimNodeBase(UStruct structType)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var current = structType; current != null; current = current.SuperStruct?.Load<UStruct>())
        {
            var currentName = current.Name;
            if (string.IsNullOrEmpty(currentName) || !visited.Add(currentName))
                break;

            if (currentName.Equals("AnimNode_Base", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsAnimNodeStructName(string name)
    {
        return name.Contains("AnimNode", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("AnimGraphNode", StringComparison.OrdinalIgnoreCase);
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
                case "BoneToModify":
                    // Store additional properties for display
                    if (value.Length <= MaxPropertyValueDisplayLength)
                    {
                        var a = prop.Tag?.GenericValue?.ToString();
                        var V = prop.Tag?.GenericValue;
                        var vv = V as CUE4Parse.UE4.Assets.Objects.FScriptStruct;
                        var vvv = vv?.StructType as CUE4Parse.UE4.Assets.Objects.FStructFallback;
                        var vvvv = vvv?.Properties[0].Tag.ToString();
                        node.AdditionalProperties[name] = vvvv is not null ? vvvv : value;
                    }
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
            AddInputPins(node, prop.Name.Text, prop.Tag);
        }
    }

    private static void AddInputPins(AnimGraphNode node, string pinName, FPropertyTagType? tag)
    {
        if (!IsPoseProperty(pinName) && !IsLinkedNodeProperty(pinName))
            return;

        if (tag?.GenericValue is UScriptArray array)
        {
            for (var i = 0; i < array.Properties.Count; i++)
            {
                AddPinIfMissing(node, $"{pinName}[{i}]", i);
            }

            if (array.Properties.Count == 0)
            {
                AddPinIfMissing(node, pinName);
            }

            return;
        }

        AddPinIfMissing(node, pinName);
    }

    private static void AddPinIfMissing(AnimGraphNode node, string pinName, int? linkIndex = null)
    {
        if (node.Pins.Any(pin => !pin.IsOutput && pin.PinName.Equals(pinName, StringComparison.OrdinalIgnoreCase)))
            return;

        node.Pins.Add(new AnimGraphPin
        {
            PinName = pinName,
            IsOutput = false,
            PinType = "pose",
            LinkIndex = linkIndex,
            OwnerNode = node
        });
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

    private static void ResolveConnections(UObject cdo, List<(string name, string structType, int childPropertyIndex)> animNodeProps,
        Dictionary<string, AnimGraphNode> nodeByName, AnimGraphViewModel vm)
    {
        // Animation node connections in cooked assets are encoded via
        // FPoseLink / FComponentSpacePoseLink struct properties within each node.
        // These contain a "LinkID" integer that maps to the index of the target node
        // in the class's animation node property list.

        foreach (var (propName, _, _) in animNodeProps)
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
        AnimGraphNode sourceNode, List<(string name, string structType, int childPropertyIndex)> animNodeProps,
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
