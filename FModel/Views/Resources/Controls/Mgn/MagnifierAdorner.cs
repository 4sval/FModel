using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// Avalonia replacement for WPF MagnifierAdorner.
/// Placed in the AdornerLayer as a Canvas that positions the <see cref="Magnifier"/> at the cursor.
/// </summary>
public class MagnifierAdorner : Canvas
{
    private readonly Control _adornedElement;
    private readonly Magnifier _magnifier;
    private Point _currentPointerPosition;
    private Point _currentElementPosition;
    private double _currentZoomFactor;

    public MagnifierAdorner(Control adornedElement, Magnifier magnifier)
    {
        _adornedElement = adornedElement;
        _magnifier = magnifier;
        _currentZoomFactor = magnifier.ZoomFactor;

        // The canvas must be transparent to hit-testing so pointer events reach the adorned control.
        IsHitTestVisible = false;
        // Start hidden so the first ShowAdorner() call triggers IsVisible false→true,
        // which fires OnPropertyChanged and subscribes PointerMoved correctly.
        IsVisible = false;

        Children.Add(_magnifier);
        UpdateViewBox();

        // Subscribe to pointer events on the adorned element.
        // PointerPressed fires first (manager handles ShowAdorner before this handler runs,
        // so the adorner is already in the visual tree when we call e.GetPosition(this)).
        // PointerMoved is subscribed/unsubscribed dynamically via OnPropertyChanged(IsVisible).
        _adornedElement.PointerPressed += OnAdornedElementPointerPressed;
    }

    public void Detach()
    {
        _adornedElement.PointerPressed -= OnAdornedElementPointerPressed;
        _adornedElement.PointerMoved -= OnAdornedElementPointerMoved;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty)
        {
            if (change.GetNewValue<bool>())
                _adornedElement.PointerMoved += OnAdornedElementPointerMoved;
            else
                _adornedElement.PointerMoved -= OnAdornedElementPointerMoved;
        }
    }

    private void OnAdornedElementPointerPressed(object? sender, PointerPressedEventArgs e)
        => HandlePointerEvent(e);

    private void OnAdornedElementPointerMoved(object? sender, PointerEventArgs e)
        => HandlePointerEvent(e);

    private void HandlePointerEvent(PointerEventArgs e)
    {
        // Adorner-canvas-relative position (used for magnifier placement on the canvas).
        var pt = e.GetPosition(this);

        // Before the first layout pass the canvas Bounds are zero — positions are
        // meaningless until the adorner has been measured.  The next PointerMoved
        // event (which fires after layout) will have correct coordinates.
        if (Bounds.Width == 0 && Bounds.Height == 0)
            return;

        if (_currentPointerPosition == pt && _magnifier.ZoomFactor == _currentZoomFactor)
            return;

        if (_magnifier.IsFrozen)
            return;

        _currentPointerPosition = pt;
        // Element-relative position (used for viewbox origin calculation — avoids PointToScreen round-trip).
        _currentElementPosition = e.GetPosition(_adornedElement);
        _currentZoomFactor = _magnifier.ZoomFactor;

        UpdateViewBox();
        PositionMagnifier();
    }

    public void UpdateViewBox()
    {
        var location = CalculateViewBoxLocation();
        _magnifier.ViewBox = new Rect(location, _magnifier.ViewBox.Size);
        _magnifier.UpdateViewBox();
    }

    private Point CalculateViewBoxLocation()
    {
        // offsetX/offsetY = coordinate delta between adorner-canvas space and adorned-element space.
        // Both positions come from the same PointerEventArgs so they share the same root transform,
        // making this DPI-safe without any PointToScreen / PointToClient round-trip.
        var offsetX = _currentElementPosition.X - _currentPointerPosition.X;
        var offsetY = _currentElementPosition.Y - _currentPointerPosition.Y;

        // Account for the target control's offset within its parent coordinate space.
        Point parentOffset = default;
        if (_magnifier.Target != null)
        {
            var offsetVec = _magnifier.Target.TranslatePoint(default, _adornedElement);
            if (offsetVec.HasValue)
                parentOffset = offsetVec.Value;
        }

        var left = _currentPointerPosition.X - (_magnifier.ViewBox.Width / 2 + offsetX) + parentOffset.X;
        var top = _currentPointerPosition.Y - (_magnifier.ViewBox.Height / 2 + offsetY) + parentOffset.Y;
        return new Point(left, top);
    }

    private void PositionMagnifier()
    {
        var x = _currentPointerPosition.X - _magnifier.Width / 2;
        var y = _currentPointerPosition.Y - _magnifier.Height / 2;
        SetLeft(_magnifier, x);
        SetTop(_magnifier, y);
    }
}
