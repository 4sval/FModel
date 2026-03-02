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
    private readonly Dictionary<AnimGraphNode, Point> _nodePositions = new();
    private readonly Dictionary<AnimGraphNode, (Border border, double width, double height)> _nodeVisuals = new();
    private readonly Dictionary<(AnimGraphNode node, string pinName, bool isOutput), Point> _pinPositions = new();

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

        DrawGraph();
        FitToView();
    }

    private void DrawGraph()
    {
        GraphCanvas.Children.Clear();
        _nodePositions.Clear();
        _nodeVisuals.Clear();
        _pinPositions.Clear();

        // Use positions from the view model (auto-layout grid positions)
        foreach (var node in _viewModel.Nodes)
        {
            _nodePositions[node] = new Point(node.NodePosX, node.NodePosY);
        }

        // Auto-layout nodes that have 0,0 positions
        AutoLayoutZeroPositionNodes();

        // Draw nodes on top
        foreach (var node in _viewModel.Nodes)
        {
            DrawNode(node);
        }

        // Draw connection lines between pin positions
        foreach (var conn in _viewModel.Connections)
        {
            DrawConnectionLine(conn);
        }
    }

    private void AutoLayoutZeroPositionNodes()
    {
        var zeroNodes = _viewModel.Nodes.Where(n => n.NodePosX == 0 && n.NodePosY == 0).ToList();
        if (zeroNodes.Count <= 1) return;

        // If most nodes have zero positions, do a simple grid layout
        var nonZeroCount = _viewModel.Nodes.Count - zeroNodes.Count;
        if (nonZeroCount > zeroNodes.Count) return; // Only a few are zero, leave them

        var cols = (int)Math.Ceiling(Math.Sqrt(zeroNodes.Count));
        for (var i = 0; i < zeroNodes.Count; i++)
        {
            var col = i % cols;
            var row = i / cols;
            _nodePositions[zeroNodes[i]] = new Point(col * (NodeWidth + 80), row * 200);
        }
    }

    private void DrawNode(AnimGraphNode node)
    {
        var pos = _nodePositions[node];
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
            SnapsToDevicePixels = true
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
        GraphCanvas.Children.Add(border);

        _nodeVisuals[node] = (border, NodeWidth, nodeHeight);

        // Calculate pin positions for connections
        for (var i = 0; i < inputPins.Count; i++)
        {
            var pinPos = new Point(pos.X, pos.Y + NodeHeaderHeight + 4 + i * PinRowHeight + PinRowHeight / 2);
            _pinPositions[(node, inputPins[i].PinName, false)] = pinPos;
        }

        for (var i = 0; i < outputPins.Count; i++)
        {
            var pinPos = new Point(pos.X + NodeWidth, pos.Y + NodeHeaderHeight + 4 + i * PinRowHeight + PinRowHeight / 2);
            _pinPositions[(node, outputPins[i].PinName, true)] = pinPos;
        }

        // Add tooltip
        border.ToolTip = BuildNodeTooltip(node);

        // Click to select
        border.MouseLeftButtonDown += (s, e) =>
        {
            SelectedNodeText.Text = $"Selected: {node.ExportType} - {node.Name}";
            e.Handled = true;
        };
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

    private void DrawConnectionLine(AnimGraphConnection conn)
    {
        var sourceKey = (conn.SourceNode, conn.SourcePinName, true);
        var targetKey = (conn.TargetNode, conn.TargetPinName, false);

        if (!_pinPositions.TryGetValue(sourceKey, out var startPos))
        {
            // Fallback: use node center-right
            if (_nodePositions.TryGetValue(conn.SourceNode, out var srcNodePos))
                startPos = new Point(srcNodePos.X + NodeWidth, srcNodePos.Y + NodeHeaderHeight + 10);
            else
                return;
        }

        if (!_pinPositions.TryGetValue(targetKey, out var endPos))
        {
            // Fallback: use node center-left
            if (_nodePositions.TryGetValue(conn.TargetNode, out var tgtNodePos))
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
        GraphCanvas.Children.Add(path);
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

    private static string BuildNodeTooltip(AnimGraphNode node)
    {
        var lines = new List<string>
        {
            $"Name: {node.Name}",
            $"Type: {node.ExportType}",
            $"Position: ({node.NodePosX}, {node.NodePosY})"
        };

        if (!string.IsNullOrEmpty(node.NodeComment))
            lines.Add($"Comment: {node.NodeComment}");

        lines.Add($"Input Pins: {node.Pins.Count(p => !p.IsOutput)}");
        lines.Add($"Output Pins: {node.Pins.Count(p => p.IsOutput)}");

        foreach (var pin in node.Pins)
        {
            var dir = pin.IsOutput ? "Out" : "In";
            var defaultVal = string.IsNullOrEmpty(pin.DefaultValue) ? "" : $" = {pin.DefaultValue}";
            lines.Add($"  [{dir}] {pin.PinName} ({pin.PinType}){defaultVal}");
        }

        // Show additional properties
        foreach (var (key, value) in node.AdditionalProperties)
        {
            lines.Add($"  {key}: {value}");
        }

        return string.Join("\n", lines);
    }

    // Zoom & Pan
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var factor = e.Delta > 0 ? 1.1 : 1.0 / 1.1;
        var pos = e.GetPosition(GraphCanvas);

        ScaleTransform.ScaleX *= factor;
        ScaleTransform.ScaleY *= factor;

        // Zoom toward mouse position
        TranslateTransform.X = pos.X * (1 - factor) + TranslateTransform.X * factor;
        TranslateTransform.Y = pos.Y * (1 - factor) + TranslateTransform.Y * factor;

        // Prevent ScaleX from exceeding limits that would cause WPF layout issues
        ScaleTransform.ScaleX = Math.Clamp(ScaleTransform.ScaleX, 0.05, 5.0);
        ScaleTransform.ScaleY = Math.Clamp(ScaleTransform.ScaleY, 0.05, 5.0);

        ZoomText.Text = $"Zoom: {ScaleTransform.ScaleX * 100:F0}%";
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
        if (!_isPanning) return;
        var currentPos = e.GetPosition(this);
        var delta = currentPos - _lastMousePos;
        TranslateTransform.X += delta.X;
        TranslateTransform.Y += delta.Y;
        _lastMousePos = currentPos;
    }

    private void OnFitToView(object sender, RoutedEventArgs e)
    {
        FitToView();
    }

    private void FitToView()
    {
        if (_nodePositions.Count == 0) return;

        var minX = _nodePositions.Values.Min(p => p.X);
        var minY = _nodePositions.Values.Min(p => p.Y);
        var maxX = _nodePositions.Values.Max(p => p.X) + NodeWidth;
        var maxY = _nodePositions.Values.Max(p => p.Y) + 150;

        var graphWidth = maxX - minX;
        var graphHeight = maxY - minY;
        if (graphWidth < 1 || graphHeight < 1) return;

        var viewWidth = ActualWidth > 0 ? ActualWidth : 800;
        var viewHeight = ActualHeight > 0 ? ActualHeight - 80 : 600;

        var scaleX = viewWidth / graphWidth * 0.9;
        var scaleY = viewHeight / graphHeight * 0.9;
        var scale = Math.Min(Math.Min(scaleX, scaleY), 2.0);

        ScaleTransform.ScaleX = scale;
        ScaleTransform.ScaleY = scale;

        TranslateTransform.X = -minX * scale + (viewWidth - graphWidth * scale) / 2;
        TranslateTransform.Y = -minY * scale + (viewHeight - graphHeight * scale) / 2;

        ZoomText.Text = $"Zoom: {scale * 100:F0}%";
    }

    private void OnResetZoom(object sender, RoutedEventArgs e)
    {
        ScaleTransform.ScaleX = 1;
        ScaleTransform.ScaleY = 1;
        TranslateTransform.X = 0;
        TranslateTransform.Y = 0;
        ZoomText.Text = "Zoom: 100%";
    }
}
