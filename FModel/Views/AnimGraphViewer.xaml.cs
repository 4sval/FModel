using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using FModel.ViewModels;

namespace FModel.Views;

public partial class AnimGraphViewer
{
    private const double NodeWidth = 220;
    private const double NodeHeaderHeight = 26;
    private const double PinRowHeight = 24;
    private const double NodeCornerRadius = 6;
    private const double PinCircleRadius = 5;
    private const double PinLabelOffset = 14; // PinCircleRadius * 2 + padding
    private const double HeaderGradientDarkenFactor = 0.6;
    private const double DefaultGraphWidthRatio = 0.65;
    private const double StateNodeWidth = 180;
    private const double StateNodeHeight = 50;
    private const double StateNodeCornerRadius = 24;
    private const double EntryNodeSize = 30;
    private const double TransitionArrowSize = 10;
    private const double TransitionCircleRadius = 8;
    private const double TransitionCircleSpacing = 3;
    private const double TransitionMultiOffset = 12;
    private const double DistanceEpsilon = 0.001;

    private readonly AnimGraphViewModel _viewModel;

    // Per-layer state
    private readonly Dictionary<AnimGraphLayer, LayerCanvasState> _layerStates = new();
    private LayerCanvasState? _currentLayerState;

    // Currently selected node (for properties panel)
    private AnimGraphNode? _selectedNode;
    private Border? _selectedBorder;

    // Currently selected transition (for properties panel)
    private AnimGraphConnection? _selectedTransition;
    private Path? _selectedTransitionPath;
    private Ellipse? _selectedTransitionCircle;
    private Color _selectedTransitionOriginalColor;

    private bool _isPanning;
    private bool _potentialPan;
    private Point _panStartPos;
    private Point _lastMousePos;
    private const double PanThreshold = 5.0;

    public AnimGraphViewer(AnimGraphViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PackageNameText.Text = _viewModel.PackageName;
        NodeCountText.Text = $"Nodes: {_viewModel.Nodes.Count}";
        ConnectionCountText.Text = $"Connections: {_viewModel.Connections.Count}";

        BuildLayerList();
        BuildLayerTabs();
    }

    private void BuildLayerList()
    {
        LayerListBox.ItemsSource = GetSidebarLayers().ToList();
    }

    /// <summary>
    /// Creates a tab for the final output pose layer only.
    /// Only the AnimGraph layer (containing the Root node) is shown initially.
    /// </summary>
    private void BuildLayerTabs()
    {
        LayerTabControl.Items.Clear();
        _layerStates.Clear();

        if (_viewModel.FullGraphLayer == null && _viewModel.Layers.Count == 0)
            return;

        // Show the combined graph first when available, otherwise fall back to AnimGraph.
        var outputLayer = _viewModel.FullGraphLayer
            ?? _viewModel.Layers.FirstOrDefault(l =>
                l.Name.Equals("AnimGraph", StringComparison.OrdinalIgnoreCase))
            ?? _viewModel.Layers[0];

        AddLayerTab(outputLayer, closable: false);

        if (LayerTabControl.Items.Count > 0)
            LayerTabControl.SelectedIndex = 0;
    }

    /// <summary>
    /// Creates a new tab for the given layer and selects it.
    /// </summary>
    private void AddLayerTab(AnimGraphLayer layer, bool closable = true)
    {
        var tabItem = new System.Windows.Controls.TabItem { Tag = layer };

        if (closable)
        {
            // Build a header with text + close button
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            headerPanel.Children.Add(new TextBlock
            {
                Text = layer.Name,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 200,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            var closeBtn = new Button
            {
                Content = "×",
                FontSize = 12,
                Padding = new Thickness(4, 0, 4, 0),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Foreground = Brushes.Gray
            };
            closeBtn.Click += (_, _) => CloseTab(tabItem);
            headerPanel.Children.Add(closeBtn);
            tabItem.Header = headerPanel;
        }
        else
        {
            tabItem.Header = new TextBlock
            {
                Text = layer.Name,
                MaxWidth = 200,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
        }

        var canvasBorder = new Border
        {
            ClipToBounds = true,
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 24))
        };

        var canvas = new Canvas();
        var scaleTransform = new ScaleTransform(1, 1);
        var translateTransform = new TranslateTransform(0, 0);
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(scaleTransform);
        transformGroup.Children.Add(translateTransform);
        canvas.RenderTransform = transformGroup;

        canvasBorder.Child = canvas;
        canvasBorder.MouseWheel += OnMouseWheel;
        canvasBorder.AddHandler(UIElement.MouseLeftButtonDownEvent,
            new MouseButtonEventHandler(OnCanvasMouseDown), true);
        canvasBorder.MouseLeftButtonUp += OnCanvasMouseUp;
        canvasBorder.MouseMove += OnCanvasMouseMove;

        tabItem.Content = canvasBorder;

        var state = new LayerCanvasState
        {
            Layer = layer,
            Canvas = canvas,
            ScaleTransform = scaleTransform,
            TranslateTransform = translateTransform
        };
        _layerStates[layer] = state;

        LayerTabControl.Items.Add(tabItem);
        LayerTabControl.SelectedItem = tabItem;
    }

    /// <summary>
    /// Closes the given tab and cleans up its layer state.
    /// </summary>
    private void CloseTab(System.Windows.Controls.TabItem tabItem)
    {
        if (tabItem.Tag is AnimGraphLayer layer)
            _layerStates.Remove(layer);

        var index = LayerTabControl.Items.IndexOf(tabItem);
        LayerTabControl.Items.Remove(tabItem);

        // Select the previous tab or the first one
        if (LayerTabControl.Items.Count > 0)
        {
            LayerTabControl.SelectedIndex = Math.Max(0, index - 1);
        }
    }

    private void OnLayerTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LayerTabControl.SelectedItem is not System.Windows.Controls.TabItem { Tag: AnimGraphLayer layer })
            return;

        if (LayerListBox.Items.Contains(layer))
        {
            LayerListBox.SelectedItem = layer;
            LayerListBox.ScrollIntoView(layer);
        }
        else
        {
            LayerListBox.SelectedItem = null;
        }

        if (!_layerStates.TryGetValue(layer, out var state))
            return;

        _currentLayerState = state;

        // Draw graph for this layer if not yet drawn
        if (!state.IsDrawn)
        {
            DrawLayerGraph(state);
            state.IsDrawn = true;

            // Fit to view after first draw
            Dispatcher.BeginInvoke(() => FitToView(state));
        }

        ZoomText.Text = $"Zoom: {state.ScaleTransform.ScaleX * 100:F0}%";
    }

    private void OnLayerListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LayerListBox.SelectedItem is AnimGraphLayer layer)
            OpenLayerTab(layer);
    }

    private void DrawLayerGraph(LayerCanvasState state)
    {
        state.Canvas.Children.Clear();
        state.NodePositions.Clear();
        state.NodeVisuals.Clear();
        state.PinPositions.Clear();

        // Use positions from the view model
        foreach (var node in state.Layer.Nodes)
        {
            state.NodePositions[node] = new Point(node.NodePosX, node.NodePosY);
        }

        // Draw connections first (behind nodes) for state machine overview
        // Group transition connections by unordered node pair so A→B and B→A are handled together
        var pairGroups = new Dictionary<(AnimGraphNode, AnimGraphNode), List<AnimGraphConnection>>();
        foreach (var conn in state.Layer.Connections)
        {
            // Skip connections between SaveCachedPose and UseCachedPose nodes
            if (!state.Layer.IsCombinedGraph && IsCachedPoseConnection(conn))
                continue;

            var isTransition = (conn.SourceNode.IsStateMachineState || conn.SourceNode.IsEntryNode) &&
                               (conn.TargetNode.IsStateMachineState || conn.TargetNode.IsEntryNode);
            if (isTransition)
            {
                // Stable unordered key: use node Name for deterministic ordering
                var a = conn.SourceNode;
                var b = conn.TargetNode;
                var key = string.Compare(a.Name, b.Name, StringComparison.Ordinal) <= 0 ? (a, b) : (b, a);
                if (!pairGroups.TryGetValue(key, out var list))
                {
                    list = [];
                    pairGroups[key] = list;
                }
                list.Add(conn);
            }
            else
            {
                DrawConnectionLine(state, conn);
            }
        }

        // Draw transitions: different directions on opposite sides, same-direction circles offset along line
        foreach (var (pair, allConns) in pairGroups)
        {
            var (nodeA, nodeB) = pair;
            var forward = allConns.Where(c => c.SourceNode == nodeA).ToList();
            var backward = allConns.Where(c => c.SourceNode == nodeB).ToList();
            var hasBothDirections = forward.Count > 0 && backward.Count > 0;

            // Compute a stable perpendicular vector based on the canonical pair direction (nodeA→nodeB)
            // so that forward and backward connections are offset to opposite sides consistently
            var pairPerpX = 0.0;
            var pairPerpY = 0.0;
            if (hasBothDirections)
            {
                var centerA = GetNodeCenter(state, nodeA);
                var centerB = GetNodeCenter(state, nodeB);
                var pdx = centerB.X - centerA.X;
                var pdy = centerB.Y - centerA.Y;
                var pLen = Math.Sqrt(pdx * pdx + pdy * pdy);
                if (pLen > DistanceEpsilon)
                {
                    pairPerpX = -pdy / pLen;
                    pairPerpY = pdx / pLen;
                }
            }

            for (var i = 0; i < forward.Count; i++)
                DrawConnectionLine(state, forward[i], i, forward.Count,
                    pairPerpX, pairPerpY, perpSide: 1, hasBothDirections: hasBothDirections);

            for (var i = 0; i < backward.Count; i++)
                DrawConnectionLine(state, backward[i], i, backward.Count,
                    pairPerpX, pairPerpY, perpSide: -1, hasBothDirections: hasBothDirections);
        }

        // Draw nodes
        foreach (var node in state.Layer.Nodes)
        {
            if (node.IsEntryNode)
                DrawEntryNode(state, node);
            else if (node.IsStateMachineState)
                DrawStateNode(state, node);
            else
                DrawNode(state, node);
        }
    }

    private void DrawNode(LayerCanvasState state, AnimGraphNode node)
    {
        var pos = state.NodePositions[node];
        var inputPins = node.Pins.Where(p => !p.IsOutput).ToList();
        var outputPins = node.Pins.Where(p => p.IsOutput).ToList();
        var maxPins = Math.Max(inputPins.Count, outputPins.Count);
        var nodeHeight = NodeHeaderHeight + Math.Max(maxPins, 1) * PinRowHeight + 10;

        // Node shadow
        var shadow = new Border
        {
            Width = NodeWidth,
            Height = nodeHeight,
            CornerRadius = new CornerRadius(NodeCornerRadius),
            Background = Brushes.Black,
            Opacity = 0.4,
            Effect = new BlurEffect { Radius = 8 }
        };
        Canvas.SetLeft(shadow, pos.X + 3);
        Canvas.SetTop(shadow, pos.Y + 3);
        Panel.SetZIndex(shadow, 0);
        state.Canvas.Children.Add(shadow);

        // Node body
        var border = new Border
        {
            Width = NodeWidth,
            Height = nodeHeight,
            CornerRadius = new CornerRadius(NodeCornerRadius),
            Background = new SolidColorBrush(Color.FromArgb(230, 42, 42, 42)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
            BorderThickness = new Thickness(1.5),
            SnapsToDevicePixels = true
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(NodeHeaderHeight) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Header with gradient
        var headerColor = GetNodeHeaderColor(node.ExportType);
        var headerBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        headerBrush.GradientStops.Add(new GradientStop(headerColor, 0.0));
        headerBrush.GradientStops.Add(new GradientStop(
            Color.FromArgb(headerColor.A,
                (byte)(headerColor.R * HeaderGradientDarkenFactor),
                (byte)(headerColor.G * HeaderGradientDarkenFactor),
                (byte)(headerColor.B * HeaderGradientDarkenFactor)), 1.0));

        var headerBorder = new Border
        {
            Background = headerBrush,
            CornerRadius = new CornerRadius(NodeCornerRadius, NodeCornerRadius, 0, 0),
            BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };

        var headerText = new TextBlock
        {
            Text = GetNodeHeaderText(node),
            Foreground = Brushes.White,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 10, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        headerBorder.Child = headerText;
        Grid.SetRow(headerBorder, 0);
        grid.Children.Add(headerBorder);

        // Pins area
        var pinsCanvas = new Canvas();

        for (var i = 0; i < inputPins.Count; i++)
        {
            var pinY = 6 + i * PinRowHeight + PinRowHeight / 2;
            var pinColor = GetPinColor(inputPins[i].PinType);
            AddPinVisual(pinsCanvas, inputPins[i], pinY, true, pinColor);
        }

        for (var i = 0; i < outputPins.Count; i++)
        {
            var pinY = 6 + i * PinRowHeight + PinRowHeight / 2;
            var pinColor = GetPinColor(outputPins[i].PinType);
            AddPinVisual(pinsCanvas, outputPins[i], pinY, false, pinColor);
        }

        Grid.SetRow(pinsCanvas, 1);
        grid.Children.Add(pinsCanvas);

        border.Child = grid;

        Canvas.SetLeft(border, pos.X);
        Canvas.SetTop(border, pos.Y);
        Panel.SetZIndex(border, 1);
        state.Canvas.Children.Add(border);

        state.NodeVisuals[node] = (border, NodeWidth, nodeHeight);

        // Calculate pin positions for connections (in canvas space)
        for (var i = 0; i < inputPins.Count; i++)
        {
            var pinPos = new Point(pos.X, pos.Y + NodeHeaderHeight + 6 + i * PinRowHeight + PinRowHeight / 2);
            state.PinPositions[(node, inputPins[i].PinName, false)] = pinPos;
        }

        for (var i = 0; i < outputPins.Count; i++)
        {
            var pinPos = new Point(pos.X + NodeWidth, pos.Y + NodeHeaderHeight + 6 + i * PinRowHeight + PinRowHeight / 2);
            state.PinPositions[(node, outputPins[i].PinName, true)] = pinPos;
        }

        // Draw pin circles on the node edges (over the border)
        for (var i = 0; i < inputPins.Count; i++)
        {
            var pinY = pos.Y + NodeHeaderHeight + 6 + i * PinRowHeight + PinRowHeight / 2;
            var pinColor = GetPinColor(inputPins[i].PinType);
            DrawPinCircle(state, pos.X, pinY, pinColor);
        }

        for (var i = 0; i < outputPins.Count; i++)
        {
            var pinY = pos.Y + NodeHeaderHeight + 6 + i * PinRowHeight + PinRowHeight / 2;
            var pinColor = GetPinColor(outputPins[i].PinType);
            DrawPinCircle(state, pos.X + NodeWidth, pinY, pinColor);
        }

        border.ToolTip = $"{node.ExportType}\n{node.Name}";

        // Click to select, double-click to open linked layer
        border.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ClickCount == 2)
            {
                TryOpenSubGraph(node);
                e.Handled = true;
                return;
            }
            SelectNode(node, border);
            e.Handled = true;
        };
    }

    /// <summary>
    /// Draws an Entry node as a small filled circle, matching UE's state machine editor.
    /// </summary>
    private void DrawEntryNode(LayerCanvasState state, AnimGraphNode node)
    {
        var pos = state.NodePositions[node];

        var circle = new Ellipse
        {
            Width = EntryNodeSize,
            Height = EntryNodeSize,
            Fill = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            Stroke = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            StrokeThickness = 2,
            SnapsToDevicePixels = true
        };
        Canvas.SetLeft(circle, pos.X);
        Canvas.SetTop(circle, pos.Y);
        Panel.SetZIndex(circle, 1);
        state.Canvas.Children.Add(circle);

        var label = new TextBlock
        {
            Text = "Entry",
            Foreground = Brushes.White,
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center
        };
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(label, pos.X + EntryNodeSize / 2 - label.DesiredSize.Width / 2);
        Canvas.SetTop(label, pos.Y + EntryNodeSize + 4);
        Panel.SetZIndex(label, 1);
        state.Canvas.Children.Add(label);

        // Output pin position (right edge of circle)
        state.PinPositions[(node, "Output", true)] = new Point(
            pos.X + EntryNodeSize, pos.Y + EntryNodeSize / 2);

        // Store visuals with a dummy border for selection
        var hitArea = new Border
        {
            Width = EntryNodeSize,
            Height = EntryNodeSize,
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(EntryNodeSize / 2)
        };
        Canvas.SetLeft(hitArea, pos.X);
        Canvas.SetTop(hitArea, pos.Y);
        Panel.SetZIndex(hitArea, 2);
        state.Canvas.Children.Add(hitArea);
        state.NodeVisuals[node] = (hitArea, EntryNodeSize, EntryNodeSize);

        hitArea.MouseLeftButtonDown += (s, e) =>
        {
            SelectNode(node, hitArea);
            e.Handled = true;
        };
    }

    /// <summary>
    /// Draws a state machine state node as a rounded rectangle with a centered name,
    /// matching UE's state machine editor visual style.
    /// </summary>
    private void DrawStateNode(LayerCanvasState state, AnimGraphNode node)
    {
        var pos = state.NodePositions[node];
        var isConduit = IsConduitNode(node);

        // Shadow
        var shadow = new Border
        {
            Width = StateNodeWidth,
            Height = StateNodeHeight,
            CornerRadius = new CornerRadius(StateNodeCornerRadius),
            Background = Brushes.Black,
            Opacity = 0.4,
            Effect = new BlurEffect { Radius = 6 }
        };
        Canvas.SetLeft(shadow, pos.X + 2);
        Canvas.SetTop(shadow, pos.Y + 2);
        Panel.SetZIndex(shadow, 0);
        state.Canvas.Children.Add(shadow);

        // State body
        var border = new Border
        {
            Width = StateNodeWidth,
            Height = StateNodeHeight,
            CornerRadius = new CornerRadius(StateNodeCornerRadius),
            Background = new SolidColorBrush(isConduit
                ? Color.FromArgb(245, 83, 57, 18)
                : Color.FromArgb(240, 55, 55, 55)),
            BorderBrush = new SolidColorBrush(isConduit
                ? Color.FromRgb(242, 170, 76)
                : Color.FromRgb(120, 120, 120)),
            BorderThickness = new Thickness(2),
            SnapsToDevicePixels = true,
            Cursor = isConduit ? Cursors.Arrow : Cursors.Hand
        };

        var contentPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (isConduit)
        {
            contentPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(210, 242, 170, 76)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(0, 0, 0, 3),
                Child = new TextBlock
                {
                    Text = "CONDUIT",
                    Foreground = new SolidColorBrush(Color.FromRgb(38, 26, 12)),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            });
        }

        contentPanel.Children.Add(new TextBlock
        {
            Text = node.Name,
            Foreground = Brushes.White,
            FontSize = isConduit ? 12 : 13,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(10, 0, 10, 0)
        });

        border.Child = contentPanel;

        Canvas.SetLeft(border, pos.X);
        Canvas.SetTop(border, pos.Y);
        Panel.SetZIndex(border, 1);
        state.Canvas.Children.Add(border);

        state.NodeVisuals[node] = (border, StateNodeWidth, StateNodeHeight);

        // Pin positions (left = input, right = output)
        state.PinPositions[(node, "In", false)] = new Point(
            pos.X, pos.Y + StateNodeHeight / 2);
        state.PinPositions[(node, "Out", true)] = new Point(
            pos.X + StateNodeWidth, pos.Y + StateNodeHeight / 2);

        border.ToolTip = isConduit
            ? $"Conduit: {node.Name}\nThis is a transition conduit and does not have a state sub-graph."
            : $"State: {node.Name}\nDouble-click to open the state sub-graph.";

        border.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ClickCount == 2)
            {
                TryOpenSubGraph(node);
                e.Handled = true;
                return;
            }
            SelectNode(node, border);
            e.Handled = true;
        };
    }

    private void AddPinVisual(Canvas pinsCanvas, AnimGraphPin pin, double y, bool isInput, Color pinColor)
    {
        var displayName = string.IsNullOrEmpty(pin.PinName) ? "(unnamed)" : pin.PinName;
        var label = new TextBlock
        {
            Text = displayName,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        if (isInput)
        {
            Canvas.SetLeft(label, PinLabelOffset);
        }
        else
        {
            label.TextAlignment = TextAlignment.Right;
            label.Width = NodeWidth - PinLabelOffset;
            Canvas.SetLeft(label, -PinLabelOffset);
        }

        Canvas.SetTop(label, y - label.FontSize * 0.7);
        pinsCanvas.Children.Add(label);
    }

    private static void DrawPinCircle(LayerCanvasState state, double cx, double cy, Color pinColor)
    {
        // Outer ring
        var outerCircle = new Ellipse
        {
            Width = PinCircleRadius * 2 + 2,
            Height = PinCircleRadius * 2 + 2,
            Fill = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
            Stroke = new SolidColorBrush(pinColor),
            StrokeThickness = 1.5,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(outerCircle, cx - PinCircleRadius - 1);
        Canvas.SetTop(outerCircle, cy - PinCircleRadius - 1);
        Panel.SetZIndex(outerCircle, 2);
        state.Canvas.Children.Add(outerCircle);

        // Inner filled circle
        var innerCircle = new Ellipse
        {
            Width = PinCircleRadius,
            Height = PinCircleRadius,
            Fill = new SolidColorBrush(pinColor),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(innerCircle, cx - PinCircleRadius / 2);
        Canvas.SetTop(innerCircle, cy - PinCircleRadius / 2);
        Panel.SetZIndex(innerCircle, 3);
        state.Canvas.Children.Add(innerCircle);
    }

    /// <summary>
    /// Selects a node and populates the properties panel with its details.
    /// </summary>
    private void SelectNode(AnimGraphNode node, Border border)
    {
        // Deselect previous node
        if (_selectedBorder != null)
        {
            _selectedBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            _selectedBorder.BorderThickness = new Thickness(1.5);
        }

        // Deselect previous transition
        DeselectTransition();

        // Highlight selected
        _selectedNode = node;
        _selectedBorder = border;
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 160, 0));
        border.BorderThickness = new Thickness(2);

        SelectedNodeText.Text = $"Selected: {node.ExportType} - {node.Name}";
        PopulatePropertiesPanel(node);
    }

    /// <summary>
    /// When a LinkedAnimLayer, StateMachine, or UseCachedPose node is double-clicked,
    /// opens its corresponding sub-graph/layer in a new tab.
    /// - LinkedAnimLayer: matches "Layer" property → layer name from Root node's "Name"
    /// - StateMachine: matches "StateMachineName" property → layer name from BakedStateMachines
    /// - UseCachedPose: finds the matching SaveCachedPose node via connections
    ///   and navigates to the layer containing it
    /// </summary>
    private void TryOpenSubGraph(AnimGraphNode node)
    {
        var canonicalNode = GetCanonicalNode(node);
        string? layerName = null;

        if (NodeMatchesType(canonicalNode, "UseCachedPose"))
        {
            TryNavigateToSaveCachedPose(canonicalNode);
            return;
        }

        if (canonicalNode.ExportType.Contains("LinkedAnimLayer", StringComparison.OrdinalIgnoreCase))
        {
            canonicalNode.AdditionalProperties.TryGetValue("Layer", out layerName);
        }
        else if (canonicalNode.IsStateMachineState)
        {
            if (canonicalNode.AdditionalProperties.TryGetValue("IsConduit", out var isConduit) &&
                bool.TryParse(isConduit, out var conduit) && conduit)
                return;

            if (canonicalNode.AdditionalProperties.TryGetValue("StateSubGraphName", out var stateSubGraphName) &&
                !string.IsNullOrEmpty(stateSubGraphName))
            {
                var namedStateLayer = _viewModel.StateSubGraphs.Values.FirstOrDefault(layer =>
                    layer.Name.Equals(stateSubGraphName, StringComparison.OrdinalIgnoreCase));
                if (namedStateLayer != null)
                {
                    OpenLayerTab(namedStateLayer);
                    return;
                }
            }

            // State nodes within an overview: find the per-state sub-graph by StateRootNodeIndex
            // The root node's property name is stored on the overview state node
            if (canonicalNode.AdditionalProperties.TryGetValue("StateRootNodeName", out var rootNodeName) &&
                !string.IsNullOrEmpty(rootNodeName) &&
                _viewModel.StateSubGraphs.TryGetValue(rootNodeName, out var stateLayer))
            {
                OpenLayerTab(stateLayer);
                return;
            }

            // Fallback: overview layers are named as "<parent> > <state machine>", while
            // per-state sub-graphs are named as "<parent> > <state machine> > <state>".
            // This avoids relying solely on StateRootNodeIndex-based metadata, which can be
            // absent or mismapped for some cooked assets.
            var currentOverviewName = _currentLayerState?.Layer.Name;
            if (!string.IsNullOrEmpty(currentOverviewName))
            {
                var expectedStateLayerName = $"{currentOverviewName}{AnimGraphViewModel.SubGraphPathSeparator}{canonicalNode.Name}";
                var fallbackStateLayer = _viewModel.StateSubGraphs.Values.FirstOrDefault(layer =>
                    layer.Name.Equals(expectedStateLayerName, StringComparison.OrdinalIgnoreCase));

                if (fallbackStateLayer != null)
                {
                    OpenLayerTab(fallbackStateLayer);
                    return;
                }
            }
        }
        else if (canonicalNode.ExportType.Contains("StateMachine", StringComparison.OrdinalIgnoreCase))
        {
            // State machine internal layers are prefixed with parent path
            if (canonicalNode.AdditionalProperties.TryGetValue("StateMachineName", out var smName))
            {
                var parentName = _currentLayerState?.Layer.IsCombinedGraph == true
                    ? "AnimGraph"
                    : _currentLayerState?.Layer.Name ?? "AnimGraph";
                layerName = $"{parentName}{AnimGraphViewModel.SubGraphPathSeparator}{smName}";
            }
        }

        if (string.IsNullOrEmpty(layerName))
            return;

        var targetLayer = _viewModel.Layers.FirstOrDefault(l =>
            l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase));
        if (targetLayer == null)
            return;

        OpenLayerTab(targetLayer);
    }

    private void OpenLayerTab(AnimGraphLayer targetLayer)
    {
        // If tab already exists, just select it
        foreach (System.Windows.Controls.TabItem tab in LayerTabControl.Items)
        {
            if (tab.Tag == targetLayer)
            {
                LayerTabControl.SelectedItem = tab;
                return;
            }
        }

        AddLayerTab(targetLayer);
    }

    private IEnumerable<AnimGraphLayer> GetSidebarLayers()
    {
        if (_viewModel.FullGraphLayer != null)
        {
            var visibleLayers = new List<AnimGraphLayer> { _viewModel.FullGraphLayer };
            var sidebarFunctionLayers = _viewModel.FunctionLayers.ToList();
            if (sidebarFunctionLayers.Count == 0)
                sidebarFunctionLayers = [.. _viewModel.Layers];

            visibleLayers.AddRange(sidebarFunctionLayers);

            return visibleLayers
                .Distinct()
                .OrderBy(layer => layer.IsCombinedGraph ? 0 : layer.Name.Equals("AnimGraph", StringComparison.OrdinalIgnoreCase) ? 1 : 2)
                .ThenBy(layer => layer.Name, StringComparer.OrdinalIgnoreCase);
        }

        var functionLayers = _viewModel.FunctionLayers.ToList();
        if (functionLayers.Count == 0)
            functionLayers = [.. _viewModel.Layers];

        return functionLayers
            .OrderBy(layer => layer.Name.Equals("AnimGraph", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(layer => layer.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Navigates from a UseCachedPose node to its corresponding SaveCachedPose node.
    /// Finds the SaveCachedPose through direct connections in the graph, locates the
    /// layer containing it, switches to that layer's tab, and selects the node.
    /// </summary>
    private void TryNavigateToSaveCachedPose(AnimGraphNode useCachedPoseNode)
    {
        useCachedPoseNode = GetCanonicalNode(useCachedPoseNode);

        // Find the matching SaveCachedPose node via connections
        AnimGraphNode? savePoseNode = null;
        foreach (var conn in _viewModel.Connections)
        {
            if (conn.SourceNode == useCachedPoseNode && NodeMatchesType(conn.TargetNode, "SaveCachedPose"))
            {
                savePoseNode = conn.TargetNode;
                break;
            }
            if (conn.TargetNode == useCachedPoseNode && NodeMatchesType(conn.SourceNode, "SaveCachedPose"))
            {
                savePoseNode = conn.SourceNode;
                break;
            }
        }

        if (savePoseNode == null)
            return;

        // Find which layer contains the SaveCachedPose node
        AnimGraphLayer? targetLayer = null;
        foreach (var layer in _viewModel.Layers)
        {
            if (layer.Nodes.Contains(savePoseNode))
            {
                targetLayer = layer;
                break;
            }
        }
        if (targetLayer == null)
        {
            foreach (var (_, layer) in _viewModel.StateSubGraphs)
            {
                if (layer.Nodes.Contains(savePoseNode))
                {
                    targetLayer = layer;
                    break;
                }
            }
        }
        if (targetLayer == null)
            return;

        // Switch to the layer's tab (or open it)
        System.Windows.Controls.TabItem? existingTab = null;
        foreach (System.Windows.Controls.TabItem tab in LayerTabControl.Items)
        {
            if (tab.Tag == targetLayer)
            {
                existingTab = tab;
                break;
            }
        }

        if (existingTab != null)
            LayerTabControl.SelectedItem = existingTab;
        else
            AddLayerTab(targetLayer);

        // Select, highlight, and center the SaveCachedPose node in the target layer.
        // Use Dispatcher.BeginInvoke to ensure the visual tree is fully updated
        // after the tab switch before attempting to select the node.
        Dispatcher.BeginInvoke(() =>
        {
            if (_layerStates.TryGetValue(targetLayer, out var state) &&
                state.NodeVisuals.TryGetValue(savePoseNode, out var visual))
            {
                SelectNode(savePoseNode, visual.border);
                CenterOnNode(savePoseNode, state);
            }
        });
    }

    private static AnimGraphNode GetCanonicalNode(AnimGraphNode node)
    {
        return node.CanonicalNode ?? node;
    }

    /// <summary>
    /// Fills the properties panel with the selected node's information,
    /// similar to UE's Details panel when a node is selected.
    /// </summary>
    private void PopulatePropertiesPanel(AnimGraphNode node)
    {
        PropertiesPanel.Children.Clear();
        PropertiesTitleText.Text = $"Properties - {GetNodeDisplayName(node)}";

        var canonicalNode = GetCanonicalNode(node);
        var resolvedProperties = node.ResolvedProperties ?? canonicalNode.ResolvedProperties;

        // Node header section
        AddPropertySection("Node Info");
        AddPropertyRow("Name", node.Name);
        AddPropertyRow("Type", node.ExportType);
        if (node.AnimNodePropertyIndex >= 0)
            AddPropertyRow("AnimNode Index", node.AnimNodePropertyIndex.ToString());
        if (node.ChildPropertyIndex >= 0)
            AddPropertyRow("Child Property Index", node.ChildPropertyIndex.ToString());
        if (!string.IsNullOrEmpty(node.NodeComment))
            AddPropertyRow("Comment", node.NodeComment);

        // Pins section
        var inputPins = node.Pins.Where(p => !p.IsOutput).ToList();
        var outputPins = node.Pins.Where(p => p.IsOutput).ToList();

        if (inputPins.Count > 0)
        {
            AddPropertySection("Input Pins");
            foreach (var pin in inputPins)
            {
                var defaultVal = string.IsNullOrEmpty(pin.DefaultValue) ? "" : $" = {pin.DefaultValue}";
                AddPropertyRow(pin.PinName, $"{pin.PinType}{defaultVal}");
            }
        }

        if (outputPins.Count > 0)
        {
            AddPropertySection("Output Pins");
            foreach (var pin in outputPins)
            {
                AddPropertyRow(pin.PinName, pin.PinType);
            }
        }

        if (resolvedProperties?.Properties.Count > 0)
        {
            AddPropertySection("AnimNode Properties");
            foreach (var property in resolvedProperties.Properties)
            {
                var preferredValue = property.Values.Count > 0 ? GetPreferredPropertyValue(property) : null;
                var formattedValue = preferredValue != null
                    ? FormatPropertyValue(preferredValue)
                    : string.Empty;

                if (!string.IsNullOrEmpty(property.DeclaredType))
                    formattedValue = string.IsNullOrEmpty(formattedValue)
                        ? $"<{property.DeclaredType}>"
                        : $"{formattedValue} [{property.DeclaredType}]";

                if (preferredValue != null)
                    formattedValue = string.IsNullOrEmpty(formattedValue)
                        ? GetPropertySourceDisplayName(preferredValue.Source)
                        : $"{formattedValue} ({GetPropertySourceDisplayName(preferredValue.Source)})";
                else if (string.IsNullOrEmpty(formattedValue))
                    formattedValue = "<No resolved value>";

                AddPropertyRow(property.Name, formattedValue);
            }

            var detailedValues = resolvedProperties.Properties
                .Where(static property => property.Values.Count > 1)
                .ToList();

            if (detailedValues.Count > 0)
            {
                AddPropertySection("Property Sources");
                foreach (var property in detailedValues)
                {
                    foreach (var value in property.Values.OrderBy(static candidate => GetPropertySourcePriority(candidate.Source)))
                    {
                        var detailLabel = $"{property.Name} [{GetPropertySourceDisplayName(value.Source)}]";
                        var detailValue = FormatPropertyValue(value);
                        if (!string.IsNullOrEmpty(property.DeclaredType))
                            detailValue = string.IsNullOrEmpty(detailValue)
                                ? $"<{property.DeclaredType}>"
                                : $"{detailValue} [{property.DeclaredType}]";

                        AddPropertyRow(detailLabel, detailValue);
                    }
                }
            }
        }

        // Additional properties
        if (node.AdditionalProperties.Count > 0)
        {
            AddPropertySection("Details");
            foreach (var (key, value) in node.AdditionalProperties)
            {
                AddPropertyRow(key, value);
            }
        }
    }

    private void AddPropertySection(string title)
    {
        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Margin = new Thickness(0, PropertiesPanel.Children.Count > 0 ? 12 : 4, 0, 4),
            Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220))
        });

        PropertiesPanel.Children.Add(new Separator
        {
            Margin = new Thickness(0, 0, 0, 4),
            Opacity = 0.3
        });
    }

    private void AddPropertyRow(string key, string value)
    {
        var rowGrid = new Grid { Margin = new Thickness(0, 1, 0, 1) };
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var keyText = new TextBlock
        {
            Text = key,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(140, 160, 180)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(4, 2, 4, 2)
        };
        Grid.SetColumn(keyText, 0);
        rowGrid.Children.Add(keyText);

        var valueText = new TextBlock
        {
            Text = value,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 240)),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(4, 2, 4, 2)
        };
        Grid.SetColumn(valueText, 1);
        rowGrid.Children.Add(valueText);

        PropertiesPanel.Children.Add(rowGrid);
    }

    private static FAnimNodePropertyValue GetPreferredPropertyValue(FAnimNodeResolvedProperty property)
    {
        return property.Values
            .OrderBy(static candidate => GetPropertySourcePriority(candidate.Source))
            .ThenBy(static candidate => candidate.ArrayIndex)
            .First();
    }

    private static int GetPropertySourcePriority(EAnimNodePropertySource source)
    {
        return source switch
        {
            EAnimNodePropertySource.MutableDataEntry => 0,
            EAnimNodePropertySource.ConstantDataEntry => 1,
            EAnimNodePropertySource.MutableData => 2,
            EAnimNodePropertySource.ConstantData => 3,
            EAnimNodePropertySource.NodeData => 4,
            EAnimNodePropertySource.DefaultObject => 5,
            _ => 99
        };
    }

    private static string GetPropertySourceDisplayName(EAnimNodePropertySource source)
    {
        return source switch
        {
            EAnimNodePropertySource.DefaultObject => "CDO",
            EAnimNodePropertySource.NodeData => "NodeData",
            EAnimNodePropertySource.ConstantData => "ConstantData",
            EAnimNodePropertySource.MutableData => "MutableData",
            EAnimNodePropertySource.ConstantDataEntry => "AnimNodeData Constant Entry",
            EAnimNodePropertySource.MutableDataEntry => "AnimNodeData Mutable Entry",
            _ => source.ToString()
        };
    }

    private static string FormatPropertyValue(FAnimNodePropertyValue value)
    {
         return FormatPropertyObject(value.ResolvedValue)
             ?? FormatPropertyObject(value.PropertyTag.Tag?.GenericValue)
               ?? value.PropertyTag.Tag?.ToString()
               ?? string.Empty;
    }

    private static string FormatPropertyObject(object value)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case string text:
                return text;
            case FName name:
                return name.Text;
            case FText textValue:
                return textValue.Text;
            case bool boolValue:
                return boolValue ? "True" : "False";
            case Enum enumValue:
                return enumValue.ToString();
            case FGuid guid:
                return guid.ToString();
            case FPackageIndex packageIndex:
                return packageIndex.ToString();
            case FStructFallback structValue:
                return FormatStructFallback(structValue);
            case UScriptArray arrayValue:
                return $"Array[{arrayValue.Properties.Count}]";
            case Array array:
                return FormatArray(array);
            default:
                return value.ToString() ?? string.Empty;
        }
    }

    private static string FormatStructFallback(FStructFallback structValue)
    {
        if (structValue.Properties.Count == 0)
            return "Struct {}";

        var parts = new List<string>();
        foreach (var property in structValue.Properties.Take(3))
        {
            var formattedValue = FormatPropertyObject(property.Tag?.GenericValue);
            if (string.IsNullOrEmpty(formattedValue))
                continue;

            parts.Add($"{property.Name.Text}={formattedValue}");
        }

        var summary = parts.Count > 0 ? string.Join(", ", parts) : $"{structValue.Properties.Count} fields";
        if (structValue.Properties.Count > 3)
            summary += ", …";

        return $"Struct {{{summary}}}";
    }

    private static string FormatArray(Array array)
    {
        if (array.Length == 0)
            return "[]";

        var builder = new StringBuilder();
        builder.Append('[');
        var count = Math.Min(array.Length, 4);
        for (var index = 0; index < count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append(FormatPropertyObject(array.GetValue(index)));
        }

        if (array.Length > count)
            builder.Append(", …");

        builder.Append(']');
        return builder.ToString();
    }

    private void DrawConnectionLine(LayerCanvasState state, AnimGraphConnection conn,
        int sameDirectionIndex = 0, int sameDirectionCount = 1,
        double pairPerpX = 0, double pairPerpY = 0,
        int perpSide = 0, bool hasBothDirections = false)
    {
        var sourceKey = (conn.SourceNode, conn.SourcePinName, true);
        var targetKey = (conn.TargetNode, conn.TargetPinName, false);

        var isTransition = (conn.SourceNode.IsStateMachineState || conn.SourceNode.IsEntryNode) &&
                           (conn.TargetNode.IsStateMachineState || conn.TargetNode.IsEntryNode);

        // Determine wire color from source pin type
        var sourcePin = conn.SourceNode.Pins.FirstOrDefault(p => p.PinName == conn.SourcePinName && p.IsOutput);
        var wireColor = sourcePin != null ? GetPinColor(sourcePin.PinType) : Color.FromRgb(200, 200, 220);

        if (isTransition)
        {
            // Compute edge-to-edge shortest path between node bounding boxes
            var (startPos, endPos) = ComputeEdgeToEdgePoints(state, conn.SourceNode, conn.TargetNode);

            // Offset lines: different directions go on opposite perpendicular sides
            var cdx = endPos.X - startPos.X;
            var cdy = endPos.Y - startPos.Y;
            var cLen = Math.Sqrt(cdx * cdx + cdy * cdy);
            double circleLineOffset = 0;

            if (cLen > DistanceEpsilon)
            {
                // Use the pre-computed canonical pair perpendicular for bidirectional separation
                if (hasBothDirections)
                {
                    var sideOffset = perpSide * TransitionMultiOffset;
                    startPos = new Point(startPos.X + pairPerpX * sideOffset, startPos.Y + pairPerpY * sideOffset);
                    endPos = new Point(endPos.X + pairPerpX * sideOffset, endPos.Y + pairPerpY * sideOffset);
                }

                // Multiple same-direction transitions: offset circles along the line direction
                if (sameDirectionCount > 1)
                {
                    circleLineOffset = (sameDirectionIndex - (sameDirectionCount - 1) / 2.0) * TransitionCircleRadius * TransitionCircleSpacing;
                }
            }

            DrawTransitionArrow(state, conn, startPos, endPos, wireColor, circleLineOffset);
        }
        else
        {
            if (!state.PinPositions.TryGetValue(sourceKey, out var startPos))
            {
                if (state.NodePositions.TryGetValue(conn.SourceNode, out var srcNodePos))
                    startPos = new Point(srcNodePos.X + NodeWidth, srcNodePos.Y + NodeHeaderHeight + 10);
                else
                    return;
            }

            if (!state.PinPositions.TryGetValue(targetKey, out var endPos))
            {
                if (state.NodePositions.TryGetValue(conn.TargetNode, out var tgtNodePos))
                    endPos = new Point(tgtNodePos.X, tgtNodePos.Y + NodeHeaderHeight + 10);
                else
                    return;
            }

            var dx = Math.Max(Math.Abs(endPos.X - startPos.X) * 0.5, 50);
            var pathFigure = new PathFigure { StartPoint = startPos };
            pathFigure.Segments.Add(new BezierSegment(
                new Point(startPos.X + dx, startPos.Y),
                new Point(endPos.X - dx, endPos.Y),
                endPos, true));

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            var path = new Path
            {
                Data = pathGeometry,
                Stroke = new SolidColorBrush(wireColor),
                StrokeThickness = 2.5,
                Opacity = 0.85,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };
            Panel.SetZIndex(path, 0);
            state.Canvas.Children.Add(path);
        }
    }

    /// <summary>
    /// Returns true if the connection links a SaveCachedPose node to a UseCachedPose node
    /// (or vice-versa). These connections are not drawn because the cached pose link is
    /// an implicit data dependency, not a visible wire in UE's animation blueprint editor.
    /// </summary>
    private static bool IsCachedPoseConnection(AnimGraphConnection conn)
    {
        var srcIsSave = NodeMatchesType(conn.SourceNode, "SaveCachedPose");
        var srcIsUse  = NodeMatchesType(conn.SourceNode, "UseCachedPose");
        var tgtIsSave = NodeMatchesType(conn.TargetNode, "SaveCachedPose");
        var tgtIsUse  = NodeMatchesType(conn.TargetNode, "UseCachedPose");

        return (srcIsSave && tgtIsUse) || (srcIsUse && tgtIsSave);
    }

    private static bool NodeMatchesType(AnimGraphNode node, string typeFragment)
    {
        return node.ExportType.Contains(typeFragment, StringComparison.OrdinalIgnoreCase) ||
               node.Name.Contains(typeFragment, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Computes the shortest straight-line connection points between two node edges.
    /// For state nodes (rounded rectangles) and entry nodes (circles), finds the
    /// intersection of the center-to-center line with each node's bounding shape.
    /// </summary>
    private (Point start, Point end) ComputeEdgeToEdgePoints(LayerCanvasState state, AnimGraphNode sourceNode, AnimGraphNode targetNode)
    {
        var srcCenter = GetNodeCenter(state, sourceNode);
        var tgtCenter = GetNodeCenter(state, targetNode);

        var startEdge = ClipToNodeEdge(state, sourceNode, srcCenter, tgtCenter);
        var endEdge = ClipToNodeEdge(state, targetNode, tgtCenter, srcCenter);

        return (startEdge, endEdge);
    }

    private static Point GetNodeCenter(LayerCanvasState state, AnimGraphNode node)
    {
        if (!state.NodePositions.TryGetValue(node, out var pos))
            return default;

        if (node.IsEntryNode)
            return new Point(pos.X + EntryNodeSize / 2, pos.Y + EntryNodeSize / 2);

        if (node.IsStateMachineState)
            return new Point(pos.X + StateNodeWidth / 2, pos.Y + StateNodeHeight / 2);

        return new Point(pos.X + NodeWidth / 2, pos.Y + NodeHeaderHeight / 2);
    }

    /// <summary>
    /// Clips a line from <paramref name="from"/> toward <paramref name="to"/>
    /// to the edge of the node's bounding shape.
    /// </summary>
    private static Point ClipToNodeEdge(LayerCanvasState state, AnimGraphNode node, Point from, Point to)
    {
        if (!state.NodePositions.TryGetValue(node, out var pos))
            return from;

        if (node.IsEntryNode)
        {
            // Circle clipping
            var cx = pos.X + EntryNodeSize / 2;
            var cy = pos.Y + EntryNodeSize / 2;
            var radius = EntryNodeSize / 2;
            var dx = to.X - cx;
            var dy = to.Y - cy;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < DistanceEpsilon) return from;
            return new Point(cx + dx / dist * radius, cy + dy / dist * radius);
        }

        // Rectangle clipping (state nodes or regular nodes)
        double w, h;
        if (node.IsStateMachineState)
        {
            w = StateNodeWidth;
            h = StateNodeHeight;
        }
        else
        {
            w = NodeWidth;
            h = state.NodeVisuals.TryGetValue(node, out var vis) ? vis.height : 60;
        }

        var rectCx = pos.X + w / 2;
        var rectCy = pos.Y + h / 2;
        var dirX = to.X - rectCx;
        var dirY = to.Y - rectCy;
        if (Math.Abs(dirX) < DistanceEpsilon && Math.Abs(dirY) < DistanceEpsilon)
            return from;

        // Find intersection with rectangle edges
        var halfW = w / 2;
        var halfH = h / 2;
        double tMin = double.MaxValue;

        // Check left/right edges
        if (Math.Abs(dirX) > DistanceEpsilon)
        {
            var t = (dirX > 0 ? halfW : -halfW) / dirX;
            var iy = dirY * t;
            if (t > 0 && Math.Abs(iy) <= halfH)
                tMin = Math.Min(tMin, t);
        }
        // Check top/bottom edges
        if (Math.Abs(dirY) > DistanceEpsilon)
        {
            var t = (dirY > 0 ? halfH : -halfH) / dirY;
            var ix = dirX * t;
            if (t > 0 && Math.Abs(ix) <= halfW)
                tMin = Math.Min(tMin, t);
        }

        if (tMin < double.MaxValue)
            return new Point(rectCx + dirX * tMin, rectCy + dirY * tMin);

        return from;
    }

    /// <summary>
    /// Draws a directional transition arrow between state machine state nodes,
    /// with an arrowhead at the target end and a small circle at the midpoint
    /// for easy click selection (matching UE's transition icon style).
    /// </summary>
    private void DrawTransitionArrow(LayerCanvasState state, AnimGraphConnection conn, Point startPos, Point endPos, Color wireColor, double circleLineOffset = 0)
    {
        var brush = new SolidColorBrush(wireColor);

        var dx = endPos.X - startPos.X;
        var dy = endPos.Y - startPos.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1) return;

        var ux = dx / length;
        var uy = dy / length;

        // Pull the endpoint back by the arrowhead size so the line ends at the arrow base
        var lineEnd = new Point(endPos.X - ux * TransitionArrowSize, endPos.Y - uy * TransitionArrowSize);

        // Build a single path containing the line + arrowhead
        var pathFigure = new PathFigure { StartPoint = startPos };
        pathFigure.Segments.Add(new LineSegment(lineEnd, true));

        var arrowBase = lineEnd;
        var perpX = -uy * TransitionArrowSize * 0.5;
        var perpY = ux * TransitionArrowSize * 0.5;
        var arrowFigure = new PathFigure { StartPoint = endPos, IsFilled = true };
        arrowFigure.Segments.Add(new LineSegment(new Point(arrowBase.X + perpX, arrowBase.Y + perpY), true));
        arrowFigure.Segments.Add(new LineSegment(new Point(arrowBase.X - perpX, arrowBase.Y - perpY), true));
        arrowFigure.IsClosed = true;

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);
        pathGeometry.Figures.Add(arrowFigure);
        pathGeometry.Freeze();

        var path = new Path
        {
            Data = pathGeometry,
            Stroke = brush,
            StrokeThickness = 2.5,
            Fill = brush,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(path, 0);
        state.Canvas.Children.Add(path);

        // Invisible wider hit area on the line for fallback clicking
        var hitPath = new Path
        {
            Data = pathGeometry,
            Stroke = Brushes.Transparent,
            StrokeThickness = 10,
            Fill = Brushes.Transparent,
            IsHitTestVisible = true,
            Cursor = Cursors.Hand
        };
        Panel.SetZIndex(hitPath, 1);
        state.Canvas.Children.Add(hitPath);

        // Small circle at the midpoint (offset along line for same-direction transitions)
        var midX = (startPos.X + endPos.X) / 2 + ux * circleLineOffset;
        var midY = (startPos.Y + endPos.Y) / 2 + uy * circleLineOffset;
        var circle = new Ellipse
        {
            Width = TransitionCircleRadius * 2,
            Height = TransitionCircleRadius * 2,
            Fill = brush,
            Stroke = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            StrokeThickness = 1.5,
            SnapsToDevicePixels = true,
            IsHitTestVisible = true,
            Cursor = Cursors.Hand,
            ToolTip = $"{conn.SourceNode.Name} → {conn.TargetNode.Name}"
        };
        Canvas.SetLeft(circle, midX - TransitionCircleRadius);
        Canvas.SetTop(circle, midY - TransitionCircleRadius);
        Panel.SetZIndex(circle, 2);
        state.Canvas.Children.Add(circle);

        // Draw a small direction arrow icon inside the circle
        var iconSize = TransitionCircleRadius * 0.7;
        var iconGeometry = new PathGeometry();
        var iconFigure = new PathFigure
        {
            StartPoint = new Point(midX + ux * iconSize, midY + uy * iconSize),
            IsFilled = true
        };
        var iconPerpX = -uy * iconSize * 0.6;
        var iconPerpY = ux * iconSize * 0.6;
        iconFigure.Segments.Add(new LineSegment(
            new Point(midX - ux * iconSize * 0.5 + iconPerpX, midY - uy * iconSize * 0.5 + iconPerpY), true));
        iconFigure.Segments.Add(new LineSegment(
            new Point(midX - ux * iconSize * 0.5 - iconPerpX, midY - uy * iconSize * 0.5 - iconPerpY), true));
        iconFigure.IsClosed = true;
        iconGeometry.Figures.Add(iconFigure);
        iconGeometry.Freeze();

        var iconPath = new Path
        {
            Data = iconGeometry,
            Fill = Brushes.White,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(iconPath, 3);
        state.Canvas.Children.Add(iconPath);

        circle.MouseLeftButtonDown += (s, e) =>
        {
            SelectTransition(conn, path, circle, wireColor);
            e.Handled = true;
        };
        hitPath.MouseLeftButtonDown += (s, e) =>
        {
            SelectTransition(conn, path, circle, wireColor);
            e.Handled = true;
        };
    }

    /// <summary>
    /// Selects a transition arrow and shows its properties in the properties panel.
    /// </summary>
    private void SelectTransition(AnimGraphConnection conn, Path transitionPath, Ellipse transitionCircle, Color originalColor)
    {
        // Deselect previous node selection
        if (_selectedBorder != null)
        {
            _selectedBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            _selectedBorder.BorderThickness = new Thickness(1.5);
            _selectedBorder = null;
        }
        _selectedNode = null;

        // Deselect previous transition
        DeselectTransition();

        // Highlight selected transition
        _selectedTransition = conn;
        _selectedTransitionPath = transitionPath;
        _selectedTransitionCircle = transitionCircle;
        _selectedTransitionOriginalColor = originalColor;
        var highlightBrush = new SolidColorBrush(Color.FromRgb(230, 160, 0));
        transitionPath.Stroke = highlightBrush;
        transitionPath.Fill = highlightBrush;
        transitionPath.StrokeThickness = 3.5;
        transitionCircle.Fill = highlightBrush;
        transitionCircle.Stroke = new SolidColorBrush(Color.FromRgb(255, 200, 50));

        var sourceName = conn.SourceNode.Name;
        var targetName = conn.TargetNode.Name;
        SelectedNodeText.Text = $"Selected: Transition {sourceName} → {targetName}";
        PopulateTransitionProperties(conn);
    }

    /// <summary>
    /// Restores the previously selected transition to its original appearance.
    /// </summary>
    private void DeselectTransition()
    {
        if (_selectedTransitionPath != null)
        {
            var restoreBrush = new SolidColorBrush(_selectedTransitionOriginalColor);
            _selectedTransitionPath.Stroke = restoreBrush;
            _selectedTransitionPath.Fill = restoreBrush;
            _selectedTransitionPath.StrokeThickness = 2.5;
            _selectedTransitionPath = null;
        }
        if (_selectedTransitionCircle != null)
        {
            _selectedTransitionCircle.Fill = new SolidColorBrush(_selectedTransitionOriginalColor);
            _selectedTransitionCircle.Stroke = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            _selectedTransitionCircle = null;
        }
        _selectedTransition = null;
    }

    /// <summary>
    /// Fills the properties panel with the selected transition's information.
    /// </summary>
    private void PopulateTransitionProperties(AnimGraphConnection conn)
    {
        PropertiesPanel.Children.Clear();
        PropertiesTitleText.Text = $"Properties - Transition";

        AddPropertySection("Transition Info");
        AddPropertyRow("From", conn.SourceNode.Name);
        AddPropertyRow("To", conn.TargetNode.Name);

        if (conn.TransitionProperties.Count > 0)
        {
            AddPropertySection("Details");
            foreach (var (key, value) in conn.TransitionProperties)
            {
                AddPropertyRow(key, value);
            }
        }
    }

    private static string GetNodeDisplayName(AnimGraphNode node)
    {
        var type = node.ExportType;
        // Clean up common prefixes for display
        if (type.StartsWith("FAnimNode_"))
            type = type["FAnimNode_".Length..];
        else if (type.StartsWith("AnimNode_"))
            type = type["AnimNode_".Length..];
        else if (type.StartsWith("AnimGraphNode_"))
            type = type["AnimGraphNode_".Length..];
        else if (type.StartsWith("K2Node_"))
            type = type["K2Node_".Length..];

        if (!string.IsNullOrEmpty(node.NodeComment))
            return $"{type}: {node.NodeComment}";

        return type;
    }

    private static string GetNodeHeaderText(AnimGraphNode node)
    {
        var displayName = GetNodeDisplayName(node);
        return node.AnimNodePropertyIndex >= 0
            ? $"[{node.AnimNodePropertyIndex}] {displayName}"
            : displayName;
    }

    private static bool IsConduitNode(AnimGraphNode node)
    {
        return node.ExportType.Contains("Conduit", StringComparison.OrdinalIgnoreCase) ||
               (node.AdditionalProperties.TryGetValue("IsConduit", out var isConduit) &&
                bool.TryParse(isConduit, out var conduit) && conduit);
    }

    private static Color GetNodeHeaderColor(string exportType)
    {
        return exportType switch
        {
            _ when exportType.Contains("StateMachine") => Color.FromRgb(200, 80, 20),
            _ when exportType.Contains("Transition") => Color.FromRgb(180, 150, 0),
            _ when exportType.Contains("BlendSpace") => Color.FromRgb(60, 80, 180),
            _ when exportType.Contains("Blend") => Color.FromRgb(70, 100, 180),
            _ when exportType.Contains("Sequence") => Color.FromRgb(0, 140, 140),
            _ when exportType.Contains("Result") || exportType.Contains("Root") => Color.FromRgb(160, 50, 50),
            _ when exportType.Contains("AnimNode") || exportType.Contains("FAnimNode") => Color.FromRgb(20, 140, 80),
            _ => Color.FromRgb(80, 80, 100)
        };
    }

    private static Color GetPinColor(string pinType)
    {
        return pinType switch
        {
            "exec" => Color.FromRgb(255, 255, 255),
            "bool" => Color.FromRgb(139, 0, 0),
            "float" or "real" or "double" => Color.FromRgb(140, 255, 140),
            "int" or "int64" => Color.FromRgb(80, 220, 180),
            "struct" => Color.FromRgb(0, 120, 215),
            "object" => Color.FromRgb(0, 160, 200),
            "string" or "text" or "name" => Color.FromRgb(255, 80, 180),
            "delegate" => Color.FromRgb(255, 56, 56),
            "pose" => Color.FromRgb(0, 160, 100),
            "transition" => Color.FromRgb(200, 200, 200),
            _ => Color.FromRgb(180, 180, 200)
        };
    }

    // Zoom & Pan
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_currentLayerState == null) return;

        var factor = e.Delta > 0 ? 1.1 : 1.0 / 1.1;

        // Get mouse position relative to the parent Border (stable coordinate space)
        var mousePos = e.GetPosition((UIElement)sender);

        var oldScale = _currentLayerState.ScaleTransform.ScaleX;
        var newScale = Math.Clamp(oldScale * factor, 0.05, 5.0);

        // Calculate the canvas-local point under the mouse cursor
        var canvasX = (mousePos.X - _currentLayerState.TranslateTransform.X) / oldScale;
        var canvasY = (mousePos.Y - _currentLayerState.TranslateTransform.Y) / oldScale;

        // Apply new scale
        _currentLayerState.ScaleTransform.ScaleX = newScale;
        _currentLayerState.ScaleTransform.ScaleY = newScale;

        // Adjust translate so the canvas point under the mouse stays fixed
        _currentLayerState.TranslateTransform.X = mousePos.X - canvasX * newScale;
        _currentLayerState.TranslateTransform.Y = mousePos.Y - canvasY * newScale;

        ZoomText.Text = $"Zoom: {newScale * 100:F0}%";
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        _potentialPan = true;
        _isPanning = false;
        _panStartPos = e.GetPosition((UIElement)sender);
        _lastMousePos = _panStartPos;
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        _potentialPan = false;
        ((UIElement)sender).ReleaseMouseCapture();
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_potentialPan || _currentLayerState == null || e.LeftButton != MouseButtonState.Pressed)
        {
            _potentialPan = false;
            _isPanning = false;
            return;
        }

        var currentPos = e.GetPosition((UIElement)sender);

        if (!_isPanning)
        {
            // Start panning only after the mouse moves beyond the threshold
            if (Math.Abs(currentPos.X - _panStartPos.X) > PanThreshold ||
                Math.Abs(currentPos.Y - _panStartPos.Y) > PanThreshold)
            {
                _isPanning = true;
                ((UIElement)sender).CaptureMouse();
                _lastMousePos = currentPos;
            }
            return;
        }

        var delta = currentPos - _lastMousePos;
        _currentLayerState.TranslateTransform.X += delta.X;
        _currentLayerState.TranslateTransform.Y += delta.Y;
        _lastMousePos = currentPos;
    }

    private void OnFitToView(object sender, RoutedEventArgs e)
    {
        if (_currentLayerState != null)
            FitToView(_currentLayerState);
    }

    private void FitToView(LayerCanvasState state)
    {
        if (state.NodePositions.Count == 0) return;

        var minX = state.NodePositions.Values.Min(p => p.X);
        var minY = state.NodePositions.Values.Min(p => p.Y);
        var maxX = state.NodePositions.Values.Max(p => p.X) + NodeWidth;
        var maxY = state.NodePositions.Values.Max(p => p.Y) + 150;

        var graphWidth = maxX - minX;
        var graphHeight = maxY - minY;
        if (graphWidth < 1 || graphHeight < 1) return;

        // Get available size from the tab content area
        var tabContent = LayerTabControl.SelectedContent as FrameworkElement;
        var viewWidth = tabContent?.ActualWidth > 0 ? tabContent.ActualWidth : (ActualWidth > 0 ? ActualWidth * DefaultGraphWidthRatio : 800);
        var viewHeight = tabContent?.ActualHeight > 0 ? tabContent.ActualHeight : (ActualHeight > 0 ? ActualHeight - 120 : 600);

        var scaleX = viewWidth / graphWidth * 0.9;
        var scaleY = viewHeight / graphHeight * 0.9;
        var scale = Math.Min(Math.Min(scaleX, scaleY), 2.0);

        state.ScaleTransform.ScaleX = scale;
        state.ScaleTransform.ScaleY = scale;

        state.TranslateTransform.X = -minX * scale + (viewWidth - graphWidth * scale) / 2;
        state.TranslateTransform.Y = -minY * scale + (viewHeight - graphHeight * scale) / 2;

        ZoomText.Text = $"Zoom: {scale * 100:F0}%";
    }

    private void OnResetZoom(object sender, RoutedEventArgs e)
    {
        if (_currentLayerState == null) return;
        _currentLayerState.ScaleTransform.ScaleX = 1;
        _currentLayerState.ScaleTransform.ScaleY = 1;
        _currentLayerState.TranslateTransform.X = 0;
        _currentLayerState.TranslateTransform.Y = 0;
        ZoomText.Text = "Zoom: 100%";
    }

    /// <summary>
    /// Adjusts the translate transform so that the specified node is centered
    /// in the viewport, keeping the current zoom level unchanged.
    /// </summary>
    private void CenterOnNode(AnimGraphNode node, LayerCanvasState state)
    {
        if (!state.NodePositions.TryGetValue(node, out var pos))
            return;

        var scale = state.ScaleTransform.ScaleX;

        // Determine node dimensions
        double nodeW = NodeWidth, nodeH = 150;
        if (state.NodeVisuals.TryGetValue(node, out var vis))
        {
            nodeW = vis.width;
            nodeH = vis.height;
        }

        // Viewport size
        var tabContent = LayerTabControl.SelectedContent as FrameworkElement;
        var viewWidth = tabContent?.ActualWidth > 0 ? tabContent.ActualWidth : (ActualWidth > 0 ? ActualWidth * DefaultGraphWidthRatio : 800);
        var viewHeight = tabContent?.ActualHeight > 0 ? tabContent.ActualHeight : (ActualHeight > 0 ? ActualHeight - 120 : 600);

        // Center of the node in graph space
        var nodeCenterX = pos.X + nodeW / 2;
        var nodeCenterY = pos.Y + nodeH / 2;

        // Set translate so that the node center maps to the viewport center
        state.TranslateTransform.X = viewWidth / 2 - nodeCenterX * scale;
        state.TranslateTransform.Y = viewHeight / 2 - nodeCenterY * scale;
    }

    /// <summary>
    /// Holds per-layer canvas state (positions, visuals, transforms).
    /// </summary>
    private class LayerCanvasState
    {
        public AnimGraphLayer Layer { get; init; } = null!;
        public Canvas Canvas { get; init; } = null!;
        public ScaleTransform ScaleTransform { get; init; } = null!;
        public TranslateTransform TranslateTransform { get; init; } = null!;
        public bool IsDrawn { get; set; }

        public Dictionary<AnimGraphNode, Point> NodePositions { get; } = new();
        public Dictionary<AnimGraphNode, (Border border, double width, double height)> NodeVisuals { get; } = new();
        public Dictionary<(AnimGraphNode node, string pinName, bool isOutput), Point> PinPositions { get; } = new();
    }
}
