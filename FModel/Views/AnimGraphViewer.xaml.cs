using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FModel.ViewModels;

namespace FModel.Views;

public partial class AnimGraphViewer
{
    private const double NodeWidth = 200;
    private const double NodeHeaderHeight = 28;
    private const double PinRowHeight = 22;
    private const double NodeCornerRadius = 4;

    private readonly AnimGraphViewModel _viewModel;

    // Per-layer state
    private readonly Dictionary<AnimGraphLayer, LayerCanvasState> _layerStates = new();
    private LayerCanvasState? _currentLayerState;

    // Currently selected node (for properties panel)
    private AnimGraphNode? _selectedNode;
    private Border? _selectedBorder;

    private bool _isPanning;
    private Point _lastMousePos;

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
    /// Creates a tab for each layer in the animation graph.
    /// Each tab contains its own canvas with zoom/pan support.
    /// </summary>
    private void BuildLayerTabs()
    {
        LayerTabControl.Items.Clear();
        _layerStates.Clear();

        // If no layers were built (empty graph), show nothing
        if (_viewModel.Layers.Count == 0)
            return;

        foreach (var layer in _viewModel.Layers)
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
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 46))
            };

            var canvas = new Canvas { RenderTransformOrigin = new Point(0, 0) };
            var scaleTransform = new ScaleTransform(1, 1);
            var translateTransform = new TranslateTransform(0, 0);
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(translateTransform);
            canvas.RenderTransform = transformGroup;

            canvasBorder.Child = canvas;
            canvasBorder.MouseWheel += OnMouseWheel;
            canvasBorder.MouseLeftButtonDown += OnCanvasMouseDown;
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
            Dispatcher.BeginInvoke(new Action(() => FitToView(state)));
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
        var nodeHeight = NodeHeaderHeight + Math.Max(maxPins, 1) * PinRowHeight + 8;

        // Node background
        var border = new Border
        {
            Width = NodeWidth,
            Height = nodeHeight,
            CornerRadius = new CornerRadius(NodeCornerRadius),
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 65)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 110)),
            BorderThickness = new Thickness(1),
            SnapsToDevicePixels = true,
            Cursor = Cursors.Hand
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(NodeHeaderHeight) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Header
        var headerBorder = new Border
        {
            Background = GetNodeHeaderBrush(node.ExportType),
            CornerRadius = new CornerRadius(NodeCornerRadius, NodeCornerRadius, 0, 0)
        };

        var headerText = new TextBlock
        {
            Text = GetNodeDisplayName(node),
            Foreground = Brushes.White,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(4, 0, 4, 0)
        };
        headerBorder.Child = headerText;
        Grid.SetRow(headerBorder, 0);
        grid.Children.Add(headerBorder);

        // Pins area
        var pinsGrid = new Grid();
        pinsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pinsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Input pins
        var inputPanel = new StackPanel { Margin = new Thickness(4, 4, 0, 0) };
        foreach (var pin in inputPins)
        {
            inputPanel.Children.Add(CreatePinLabel(pin, HorizontalAlignment.Left));
        }
        Grid.SetColumn(inputPanel, 0);
        pinsGrid.Children.Add(inputPanel);

        // Output pins
        var outputPanel = new StackPanel { Margin = new Thickness(0, 4, 4, 0) };
        foreach (var pin in outputPins)
        {
            outputPanel.Children.Add(CreatePinLabel(pin, HorizontalAlignment.Right));
        }
        Grid.SetColumn(outputPanel, 1);
        pinsGrid.Children.Add(outputPanel);

        Grid.SetRow(pinsGrid, 1);
        grid.Children.Add(pinsGrid);

        border.Child = grid;

        Canvas.SetLeft(border, pos.X);
        Canvas.SetTop(border, pos.Y);
        Panel.SetZIndex(border, 1);
        state.Canvas.Children.Add(border);

        state.NodeVisuals[node] = (border, NodeWidth, nodeHeight);

        // Calculate pin positions for connections
        for (var i = 0; i < inputPins.Count; i++)
        {
            var pinPos = new Point(pos.X, pos.Y + NodeHeaderHeight + 4 + i * PinRowHeight + PinRowHeight / 2);
            state.PinPositions[(node, inputPins[i].PinName, false)] = pinPos;
        }

        for (var i = 0; i < outputPins.Count; i++)
        {
            var pinPos = new Point(pos.X + NodeWidth, pos.Y + NodeHeaderHeight + 4 + i * PinRowHeight + PinRowHeight / 2);
            state.PinPositions[(node, outputPins[i].PinName, true)] = pinPos;
        }

        // Add tooltip with basic info
        border.ToolTip = $"{node.ExportType}\n{node.Name}";

        // Click to select node and show properties
        border.MouseLeftButtonDown += (s, e) =>
        {
            SelectNode(node, border);
            e.Handled = true;
        };
    }

    /// <summary>
    /// Selects a node and populates the properties panel with its details.
    /// </summary>
    private void SelectNode(AnimGraphNode node, Border border)
    {
        // Deselect previous
        if (_selectedBorder != null)
        {
            _selectedBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 110));
            _selectedBorder.BorderThickness = new Thickness(1);
        }

        // Highlight selected
        _selectedNode = node;
        _selectedBorder = border;
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 160, 255));
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

    private static TextBlock CreatePinLabel(AnimGraphPin pin, HorizontalAlignment alignment)
    {
        var displayName = string.IsNullOrEmpty(pin.PinName) ? "(unnamed)" : pin.PinName;
        var pinColor = GetPinColor(pin.PinType);

        return new TextBlock
        {
            Text = alignment == HorizontalAlignment.Left ? $"● {displayName}" : $"{displayName} ●",
            Foreground = new SolidColorBrush(pinColor),
            FontSize = 10,
            Height = PinRowHeight,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Padding = new Thickness(2, 2, 2, 0)
        };
    }

    private void DrawConnectionLine(LayerCanvasState state, AnimGraphConnection conn)
    {
        var sourceKey = (conn.SourceNode, conn.SourcePinName, true);
        var targetKey = (conn.TargetNode, conn.TargetPinName, false);

        if (!state.PinPositions.TryGetValue(sourceKey, out var startPos))
        {
            // Fallback: use node center-right
            if (state.NodePositions.TryGetValue(conn.SourceNode, out var srcNodePos))
                startPos = new Point(srcNodePos.X + NodeWidth, srcNodePos.Y + NodeHeaderHeight + 10);
            else
                return;
        }

        if (!state.PinPositions.TryGetValue(targetKey, out var endPos))
        {
            // Fallback: use node center-left
            if (state.NodePositions.TryGetValue(conn.TargetNode, out var tgtNodePos))
                endPos = new Point(tgtNodePos.X, tgtNodePos.Y + NodeHeaderHeight + 10);
            else
                return;
        }

        var dx = Math.Abs(endPos.X - startPos.X) * 0.5;
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
            Stroke = new SolidColorBrush(Color.FromRgb(180, 180, 200)),
            StrokeThickness = 1.5,
            Opacity = 0.7,
            SnapsToDevicePixels = true
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

    private static Brush GetNodeHeaderBrush(string exportType)
    {
        return exportType switch
        {
            _ when exportType.Contains("StateMachine") => new SolidColorBrush(Color.FromRgb(140, 60, 20)),
            _ when exportType.Contains("Transition") => new SolidColorBrush(Color.FromRgb(140, 120, 0)),
            _ when exportType.Contains("BlendSpace") => new SolidColorBrush(Color.FromRgb(60, 60, 160)),
            _ when exportType.Contains("Blend") => new SolidColorBrush(Color.FromRgb(80, 80, 160)),
            _ when exportType.Contains("Sequence") => new SolidColorBrush(Color.FromRgb(0, 120, 120)),
            _ when exportType.Contains("Result") || exportType.Contains("Root") => new SolidColorBrush(Color.FromRgb(120, 40, 40)),
            _ when exportType.Contains("AnimNode") || exportType.Contains("FAnimNode") => new SolidColorBrush(Color.FromRgb(0, 120, 80)),
            _ => new SolidColorBrush(Color.FromRgb(70, 70, 90))
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
        var pos = e.GetPosition(_currentLayerState.Canvas);

        _currentLayerState.ScaleTransform.ScaleX *= factor;
        _currentLayerState.ScaleTransform.ScaleY *= factor;

        // Zoom toward mouse position
        _currentLayerState.TranslateTransform.X = pos.X * (1 - factor) + _currentLayerState.TranslateTransform.X * factor;
        _currentLayerState.TranslateTransform.Y = pos.Y * (1 - factor) + _currentLayerState.TranslateTransform.Y * factor;

        // Clamp scale
        _currentLayerState.ScaleTransform.ScaleX = Math.Clamp(_currentLayerState.ScaleTransform.ScaleX, 0.05, 5.0);
        _currentLayerState.ScaleTransform.ScaleY = Math.Clamp(_currentLayerState.ScaleTransform.ScaleY, 0.05, 5.0);

        ZoomText.Text = $"Zoom: {_currentLayerState.ScaleTransform.ScaleX * 100:F0}%";
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _lastMousePos = e.GetPosition(this);
        ((UIElement)sender).CaptureMouse();
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        ((UIElement)sender).ReleaseMouseCapture();
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning || _currentLayerState == null) return;
        var currentPos = e.GetPosition(this);
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
        var viewWidth = tabContent?.ActualWidth > 0 ? tabContent.ActualWidth : (ActualWidth > 0 ? ActualWidth * 0.65 : 800);
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
