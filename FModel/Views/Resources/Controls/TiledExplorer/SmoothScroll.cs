using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace FModel.Views.Resources.Controls.TiledExplorer;

/// <summary>
/// Attached behavior to reduce mouse-wheel scroll sensitivity for elements containing a ScrollViewer.
/// Attach to the ListBox (or its Style) with IsEnabled="True" and optionally set Factor to control strength.
/// Smaller Factor -> smaller scroll per notch.
/// </summary>
public static class SmoothScroll
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<SmoothScroll, Control, bool>("IsEnabled");

    public static readonly AttachedProperty<double> FactorProperty =
        AvaloniaProperty.RegisterAttached<SmoothScroll, Control, double>("Factor", 0.25);

    public static void SetIsEnabled(Control obj, bool value) => obj.SetValue(IsEnabledProperty, value);
    public static bool GetIsEnabled(Control obj) => obj.GetValue(IsEnabledProperty);
    public static void SetFactor(Control obj, double value) => obj.SetValue(FactorProperty, value);
    public static double GetFactor(Control obj) => obj.GetValue(FactorProperty);

    private static readonly ConditionalWeakTable<Control, ScrollViewer> _scrollViewerCache = new();

    static SmoothScroll()
    {
        IsEnabledProperty.Changed.Subscribe(OnIsEnabledChanged);
    }

    private static void OnIsEnabledChanged(AvaloniaPropertyChangedEventArgs<bool> e)
    {
        if (e.Sender is not Control element)
            return;

        if (e.NewValue.GetValueOrDefault())
            element.PointerWheelChanged += Element_PointerWheelChanged;
        else
        {
            element.PointerWheelChanged -= Element_PointerWheelChanged;
            _scrollViewerCache.Remove(element);
        }
    }

    private static void Element_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not Control control)
            return;

        var sv = FindScrollViewer(control);
        if (sv == null)
            return;

        double factor = GetFactor(control);
        if (double.IsNaN(factor) || factor <= 0)
            factor = 0.25;

        double notches = e.Delta.Y;
        const double basePixelsPerNotch = 50.0;
        double adjustedPixels = notches * basePixelsPerNotch * factor;

        var scrollableHeight = Math.Max(0, sv.Extent.Height - sv.Viewport.Height);
        var scrollableWidth = Math.Max(0, sv.Extent.Width - sv.Viewport.Width);

        if (scrollableHeight > 0)
        {
            double newOffset = sv.Offset.Y - adjustedPixels;
            if (newOffset < 0)
                newOffset = 0;
            if (newOffset > scrollableHeight)
                newOffset = scrollableHeight;
            sv.Offset = sv.Offset.WithY(newOffset);
            e.Handled = true;
            return;
        }

        if (scrollableWidth > 0)
        {
            double newOffset = sv.Offset.X - adjustedPixels;
            if (newOffset < 0)
                newOffset = 0;
            if (newOffset > scrollableWidth)
                newOffset = scrollableWidth;
            sv.Offset = sv.Offset.WithX(newOffset);
            e.Handled = true;
        }
    }

    private static ScrollViewer? FindScrollViewer(Control control)
    {
        if (control is ScrollViewer sv)
            return sv;

        if (_scrollViewerCache.TryGetValue(control, out var cached))
            return cached;

        var found = control.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (found != null)
            _scrollViewerCache.Add(control, found);

        return found;
    }
}
