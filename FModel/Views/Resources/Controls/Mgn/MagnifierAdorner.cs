using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FModel.Views.Resources.Controls;

public class MagnifierAdorner : Adorner
{
    private Magnifier _magnifier;
    private Point _currentMousePosition;
    private double _currentZoomFactor;

    public MagnifierAdorner(UIElement element, Magnifier magnifier) : base(element)
    {
        _magnifier = magnifier;
        _currentZoomFactor = _magnifier.ZoomFactor;

        UpdateViewBox();
        AddVisualChild(_magnifier);

        Loaded += (_, _) => InputManager.Current.PostProcessInput += OnProcessInput;
        Unloaded += (_, _) => InputManager.Current.PostProcessInput -= OnProcessInput;
    }

    private void OnProcessInput(object sender, ProcessInputEventArgs e)
    {
        var pt = Mouse.GetPosition(this);
        if (_currentMousePosition == pt && _magnifier.ZoomFactor == _currentZoomFactor)
            return;

        if (_magnifier.IsFrozen)
            return;

        _currentMousePosition = pt;
        _currentZoomFactor = _magnifier.ZoomFactor;

        UpdateViewBox();
        InvalidateArrange();
    }

    public void UpdateViewBox()
    {
        var viewBoxLocation = CalculateViewBoxLocation();
        _magnifier.ViewBox = new Rect(viewBoxLocation, _magnifier.ViewBox.Size);
    }

    private Point CalculateViewBoxLocation()
    {
        double offsetX, offsetY;
        var adorner = Mouse.GetPosition(this);
        var element = Mouse.GetPosition(AdornedElement);

        offsetX = element.X - adorner.X;
        offsetY = element.Y - adorner.Y;

        var parentOffsetVector = VisualTreeHelper.GetOffset(_magnifier.Target);
        var parentOffset = new Point(parentOffsetVector.X, parentOffsetVector.Y);

        var left = _currentMousePosition.X - (_magnifier.ViewBox.Width / 2 + offsetX) + parentOffset.X;
        var top = _currentMousePosition.Y - (_magnifier.ViewBox.Height / 2 + offsetY) + parentOffset.Y;
        return new Point(left, top);
    }

    protected override Visual GetVisualChild(int index)
    {
        return _magnifier;
    }

    protected override int VisualChildrenCount => 1;

    protected override Size MeasureOverride(Size constraint)
    {
        _magnifier.Measure(constraint);
        return base.MeasureOverride(constraint);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = _currentMousePosition.X - _magnifier.Width / 2;
        var y = _currentMousePosition.Y - _magnifier.Height / 2;
        _magnifier.Arrange(new Rect(x, y, _magnifier.Width, _magnifier.Height));
        return base.ArrangeOverride(finalSize);
    }
}