using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
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

    private readonly AnimGraphViewModel _viewModel;

    // Per-layer state
    private readonly Dictionary<AnimGraphLayer, LayerCanvasState> _layerStates = new();
    private LayerCanvasState? _currentLayerState;

    // Currently selected node (for properties panel)
    private AnimGraphNode? _selectedNode;
    private Border? _selectedBorder;

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

        BuildLayerTabs();
    }

    /// <summary>
    /// Creates a tab for the final output pose layer only.
    /// Only the AnimGraph layer (containing the Root node) is shown initially.
    /// </summary>
    private void BuildLayerTabs()
    {
        LayerTabControl.Items.Clear();
        _layerStates.Clear();

        // If no layers were built (empty graph), show nothing
        if (_viewModel.Layers.Count == 0)
            return;

        // Show only the final output pose layer (AnimGraph) initially
        var outputLayer = _viewModel.Layers.FirstOrDefault(l =>
            l.Nodes.Any(n => n.ExportType.Contains("Root", StringComparison.OrdinalIgnoreCase) ||
                             n.ExportType.Contains("Result", StringComparison.OrdinalIgnoreCase)))
            ?? _viewModel.Layers[0];

        var layersToShow = new[] { outputLayer };

        foreach (var layer in layersToShow)
        {
            var tabItem = new System.Windows.Controls.TabItem
            {
                Header = layer.Name,
                Tag = layer
            };

            // Create canvas container for this layer
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
        }

        // Select the first tab
        if (LayerTabControl.Items.Count > 0)
            LayerTabControl.SelectedIndex = 0;
    }

    private void OnLayerTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LayerTabControl.SelectedItem is not System.Windows.Controls.TabItem { Tag: AnimGraphLayer layer })
            return;

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

        // Draw nodes
        foreach (var node in state.Layer.Nodes)
        {
            DrawNode(state, node);
        }

        // Draw connections
        foreach (var conn in state.Layer.Connections)
        {
            DrawConnectionLine(state, conn);
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
            Text = GetNodeDisplayName(node),
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

        // Click to select node and show properties
        border.MouseLeftButtonDown += (s, e) =>
        {
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
        // Deselect previous
        if (_selectedBorder != null)
        {
            _selectedBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            _selectedBorder.BorderThickness = new Thickness(1.5);
        }

        // Highlight selected
        _selectedNode = node;
        _selectedBorder = border;
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 160, 0));
        border.BorderThickness = new Thickness(2);

        SelectedNodeText.Text = $"Selected: {node.ExportType} - {node.Name}";
        PopulatePropertiesPanel(node);
    }

    /// <summary>
    /// Fills the properties panel with the selected node's information,
    /// similar to UE's Details panel when a node is selected.
    /// </summary>
    private void PopulatePropertiesPanel(AnimGraphNode node)
    {
        PropertiesPanel.Children.Clear();
        PropertiesTitleText.Text = $"Properties - {GetNodeDisplayName(node)}";

        // Node header section
        AddPropertySection("Node Info");
        AddPropertyRow("Name", node.Name);
        AddPropertyRow("Type", node.ExportType);
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

    private void DrawConnectionLine(LayerCanvasState state, AnimGraphConnection conn)
    {
        var sourceKey = (conn.SourceNode, conn.SourcePinName, true);
        var targetKey = (conn.TargetNode, conn.TargetPinName, false);

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

        // Determine wire color from source pin type
        var sourcePin = conn.SourceNode.Pins.FirstOrDefault(p => p.PinName == conn.SourcePinName && p.IsOutput);
        var wireColor = sourcePin != null ? GetPinColor(sourcePin.PinType) : Color.FromRgb(200, 200, 220);

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
