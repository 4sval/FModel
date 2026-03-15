using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace FModel.Views.Resources.Controls;

public class MagnifierManager
{
    private MagnifierAdorner? _adorner;
    private Control? _element;

    // Stores active managers so OnMagnifierChanged can detach the old one when the property changes.
    private static readonly ConditionalWeakTable<Control, MagnifierManager> _managers = new();

    // -----------------------------------------------------------------------
    // Attached property — usage: <Image controls:MagnifierManager.Magnifier="..." />
    // -----------------------------------------------------------------------
    public static readonly AttachedProperty<Magnifier?> MagnifierProperty =
        AvaloniaProperty.RegisterAttached<MagnifierManager, Control, Magnifier?>("Magnifier");

    public static void SetMagnifier(Control element, Magnifier? value)
        => element.SetValue(MagnifierProperty, value);

    public static Magnifier? GetMagnifier(Control element)
        => element.GetValue(MagnifierProperty);

    static MagnifierManager()
    {
        MagnifierProperty.Changed.Subscribe(OnMagnifierChanged);
    }

    private static void OnMagnifierChanged(AvaloniaPropertyChangedEventArgs<Magnifier?> e)
    {
        if (e.Sender is not Control target)
            throw new ArgumentException("Magnifier can only be attached to a Control.");

        // Detach any previously registered manager on this element before attaching a new one.
        if (_managers.TryGetValue(target, out var old))
        {
            old.Detach();
            _managers.Remove(target);
        }

        var magnifier = e.NewValue.GetValueOrDefault();
        if (magnifier != null)
        {
            var manager = new MagnifierManager();
            manager.AttachToMagnifier(target, magnifier);
            _managers.Add(target, manager);
        }
    }

    // -----------------------------------------------------------------------
    // Instance — manages one control/magnifier pair
    // -----------------------------------------------------------------------

    private void AttachToMagnifier(Control element, Magnifier magnifier)
    {
        _element = element;
        _element.PointerPressed += ElementOnPointerPressed;
        _element.PointerReleased += ElementOnPointerReleased;
        _element.PointerWheelChanged += ElementOnPointerWheelChanged;
        _element.DetachedFromVisualTree += OnElementDetached;

        magnifier.Target = _element;

        _adorner = new MagnifierAdorner(_element, magnifier);
    }

    private void Detach()
    {
        if (_element != null)
        {
            _element.PointerPressed -= ElementOnPointerPressed;
            _element.PointerReleased -= ElementOnPointerReleased;
            _element.PointerWheelChanged -= ElementOnPointerWheelChanged;
            _element.DetachedFromVisualTree -= OnElementDetached;

            // Remove the adorner control from the AdornerLayer so it is not left
            // as an invisible orphan child for the lifetime of the host window.
            // Use the static API to match the add path in VerifyAdornerLayer.
            AdornerLayer.SetAdornment(_element, null);
        }
        _adorner?.Detach();
        _adorner = null;
        _element = null;
    }

    private void OnElementDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Full teardown so that if the element is ever re-attached and SetMagnifier is
        // called again, a fresh manager/adorner pair is created from scratch.
        var element = _element;
        Detach();
        if (element != null)
            _managers.Remove(element);
    }

    private void ElementOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.GetCurrentPoint(null).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased)
            return;
        if (_element != null && GetMagnifier(_element) is { IsFrozen: true })
            return;

        HideAdorner();
    }

    private void ElementOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            ShowAdorner();
    }

    private void ElementOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_element == null)
            return;
        if (GetMagnifier(_element) is not { IsUsingZoomOnMouseWheel: true } magnifier)
            return;

        // WPF Delta < 0 = scroll down = zoom out (larger viewbox); Delta > 0 = scroll up = zoom in.
        // Avalonia Delta.Y > 0 = scroll up.
        if (e.Delta.Y < 0)
        {
            var newValue = magnifier.ZoomFactor + magnifier.ZoomFactorOnMouseWheel;
            magnifier.SetCurrentValue(Magnifier.ZoomFactorProperty, newValue);
        }
        else if (e.Delta.Y > 0)
        {
            var newValue = magnifier.ZoomFactor >= magnifier.ZoomFactorOnMouseWheel
                ? magnifier.ZoomFactor - magnifier.ZoomFactorOnMouseWheel
                : 0d;
            magnifier.SetCurrentValue(Magnifier.ZoomFactorProperty, newValue);
        }

        _adorner?.UpdateViewBox();
    }

    private void ShowAdorner()
    {
        if (_adorner == null || _element == null)
            return;
        VerifyAdornerLayer();
        _adorner.IsVisible = true;
    }

    private void VerifyAdornerLayer()
    {
        if (_adorner == null || _element == null)
            return;
        if (_adorner.Parent != null)
            return;

        var layer = AdornerLayer.GetAdornerLayer(_element);
        if (layer != null)
            AdornerLayer.SetAdornment(_element, _adorner);
    }

    private void HideAdorner()
    {
        if (_adorner is { IsVisible: true })
            _adorner.IsVisible = false;
    }
}
