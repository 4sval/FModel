using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FModel.Views.Resources.Controls.TiledExplorer;

/// <summary>
/// Attached behavior to reduce mouse-wheel scroll sensitivity for elements containing a ScrollViewer.
/// Attach to the ListBox (or its Style) with IsEnabled="True" and optionally set Factor to control strength.
/// Smaller Factor -> smaller scroll per notch.
/// </summary>
public static class SmoothScroll
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(SmoothScroll), new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty FactorProperty = DependencyProperty.RegisterAttached(
        "Factor", typeof(double), typeof(SmoothScroll), new PropertyMetadata(0.25));

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);
    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetFactor(DependencyObject obj, double value) => obj.SetValue(FactorProperty, value);
    public static double GetFactor(DependencyObject obj) => (double)obj.GetValue(FactorProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
                element.PreviewMouseWheel += Element_PreviewMouseWheel;
            else
                element.PreviewMouseWheel -= Element_PreviewMouseWheel;
        }
    }

    private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject dep) return;

        var sv = FindScrollViewer(dep);
        if (sv == null) return;

        double factor = GetFactor(dep);
        if (double.IsNaN(factor) || factor <= 0) factor = 0.25;

        // e.Delta is typically +/-120 per notch
        double notches = e.Delta / 120.0;

        // Base pixels per notch (tweakable); smaller value gives smoother/less jumpy scroll
        const double basePixelsPerNotch = 50.0;

        double adjustedPixels = notches * basePixelsPerNotch * factor;

        // Prefer vertical scrolling when possible
        if (sv.ScrollableHeight > 0)
        {
            double newOffset = sv.VerticalOffset - adjustedPixels;
            if (newOffset < 0) newOffset = 0;
            if (newOffset > sv.ScrollableHeight) newOffset = sv.ScrollableHeight;
            sv.ScrollToVerticalOffset(newOffset);
            e.Handled = true;
            return;
        }

        if (sv.ScrollableWidth > 0)
        {
            double newOffset = sv.HorizontalOffset - adjustedPixels;
            if (newOffset < 0) newOffset = 0;
            if (newOffset > sv.ScrollableWidth) newOffset = sv.ScrollableWidth;
            sv.ScrollToHorizontalOffset(newOffset);
            e.Handled = true;
        }
    }

    private static ScrollViewer FindScrollViewer(DependencyObject d)
    {
        if (d == null) return null;
        if (d is ScrollViewer sv) return sv;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
        {
            var child = VisualTreeHelper.GetChild(d, i);
            var result = FindScrollViewer(child);
            if (result != null) return result;
        }

        return null;
    }
}
