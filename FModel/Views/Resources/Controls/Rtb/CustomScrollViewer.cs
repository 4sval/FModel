using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// Provides a bindable <see cref="VerticalOffsetProperty"/> attached property for
/// <see cref="ScrollViewer"/>.
/// When set, scrolls the viewer to that offset immediately and keeps the property
/// in sync as the user scrolls (two-way).
/// Replaces the WPF <c>DependencyProperty</c>-based implementation.
/// </summary>
public static class CustomScrollViewer
{
    // Tracks which ScrollViewer instances already have a ScrollChanged subscription,
    // using a weak key so GC can reclaim viewers that leave the visual tree.
    private static readonly ConditionalWeakTable<ScrollViewer, object> _subscribed = new();
    // Non-null sentinel value required by ConditionalWeakTable; the value itself is never read.
    private static readonly object _marker = new();

    /// <summary>Bindable vertical-offset attached property.</summary>
    public static readonly AttachedProperty<double> VerticalOffsetProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, double>(
            "VerticalOffset",
            typeof(CustomScrollViewer),
            defaultValue: double.NaN,
            inherits: false,
            defaultBindingMode: BindingMode.TwoWay);

    public static double GetVerticalOffset(ScrollViewer viewer)
        => viewer.GetValue(VerticalOffsetProperty);

    public static void SetVerticalOffset(ScrollViewer viewer, double value)
        => viewer.SetValue(VerticalOffsetProperty, value);

    static CustomScrollViewer()
    {
        VerticalOffsetProperty.Changed.AddClassHandler<ScrollViewer>(OnVerticalOffsetChanged);
    }

    private static void OnVerticalOffsetChanged(ScrollViewer viewer, AvaloniaPropertyChangedEventArgs e)
    {
        var value = e.GetNewValue<double>();
        if (double.IsNaN(value))
            return;

        // Subscribe once per ScrollViewer instance to sync the property back when the user
        // scrolls.  This must happen before the epsilon guard below so that viewers starting at
        // offset 0.0 (the common case) still get a subscription on their first property set.
        // ConditionalWeakTable keeps the key weak so the viewer can be GC'd normally.
        if (!_subscribed.TryGetValue(viewer, out _))
        {
            _subscribed.Add(viewer, _marker);
            viewer.ScrollChanged += (_, se) =>
            {
                if (se.OffsetDelta.Y == 0)
                    return;
                // Update the attached property so two-way bindings stay in sync.
                // The re-entrancy guard below prevents an infinite update loop.
                viewer.SetCurrentValue(VerticalOffsetProperty, viewer.Offset.Y);
            };
        }

        // Short-circuit re-entrancy: the ScrollChanged handler above calls SetCurrentValue,
        // which re-triggers this callback.  If the viewer is already at the requested offset
        // there is nothing further to do.
        if (Math.Abs(viewer.Offset.Y - value) < 1e-6)
            return;

        viewer.Offset = viewer.Offset.WithY(value);
    }
}
