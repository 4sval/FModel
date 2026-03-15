using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FModel.Views.Resources.Controls;

public class Magnifier : Control
{
    private const double DEFAULT_SIZE = 100d;

    // -----------------------------------------------------------------------
    // Styled properties
    // -----------------------------------------------------------------------

    public static readonly StyledProperty<EFrameType> FrameTypeProperty =
        AvaloniaProperty.Register<Magnifier, EFrameType>(nameof(FrameType), defaultValue: EFrameType.Circle);
    public EFrameType FrameType
    {
        get => GetValue(FrameTypeProperty);
        set => SetValue(FrameTypeProperty, value);
    }

    public static readonly StyledProperty<bool> IsUsingZoomOnMouseWheelProperty =
        AvaloniaProperty.Register<Magnifier, bool>(nameof(IsUsingZoomOnMouseWheel), defaultValue: true);
    public bool IsUsingZoomOnMouseWheel
    {
        get => GetValue(IsUsingZoomOnMouseWheelProperty);
        set => SetValue(IsUsingZoomOnMouseWheelProperty, value);
    }

    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<Magnifier, double>(nameof(Radius), defaultValue: DEFAULT_SIZE / 2);
    public double Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public static readonly StyledProperty<Control?> TargetProperty =
        AvaloniaProperty.Register<Magnifier, Control?>(nameof(Target));
    public Control? Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public static readonly StyledProperty<double> ZoomFactorProperty =
        AvaloniaProperty.Register<Magnifier, double>(nameof(ZoomFactor), defaultValue: 0.5,
            validate: v => v >= 0);
    public double ZoomFactor
    {
        get => GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    public static readonly StyledProperty<double> ZoomFactorOnMouseWheelProperty =
        AvaloniaProperty.Register<Magnifier, double>(nameof(ZoomFactorOnMouseWheel), defaultValue: 0.1d,
            validate: v => v >= 0);
    public double ZoomFactorOnMouseWheel
    {
        get => GetValue(ZoomFactorOnMouseWheelProperty);
        set => SetValue(ZoomFactorOnMouseWheelProperty, value);
    }

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<Magnifier, IBrush?>(nameof(Background));
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<Magnifier, IBrush?>(nameof(BorderBrush));
    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        AvaloniaProperty.Register<Magnifier, Thickness>(nameof(BorderThickness));
    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    // ViewBox is not a styled property — it is driven programmatically by the adorner.
    public Rect ViewBox { get; set; }

    public bool IsFrozen { get; private set; }

    // VisualBrush paints the magnified region; rebuilt whenever Target changes.
    private VisualBrush? _visualBrush;

    // -----------------------------------------------------------------------
    // Static ctor — wire property-changed callbacks
    // -----------------------------------------------------------------------
    static Magnifier()
    {
        FrameTypeProperty.Changed.AddClassHandler<Magnifier>((m, _) => m.OnFrameTypeChanged());
        RadiusProperty.Changed.AddClassHandler<Magnifier>((m, _) => m.OnRadiusChanged());
        ZoomFactorProperty.Changed.AddClassHandler<Magnifier>((m, _) => m.UpdateViewBox());
        // Rebuild the VisualBrush whenever the Target control changes.
        TargetProperty.Changed.AddClassHandler<Magnifier>((m, _) => m.RebuildBrush());

        // Default Width / Height
        WidthProperty.OverrideDefaultValue<Magnifier>(DEFAULT_SIZE);
        HeightProperty.OverrideDefaultValue<Magnifier>(DEFAULT_SIZE);

        // Class-level handler — must live in static ctor, not the instance ctor, to avoid
        // registering N handlers for N instances (AddClassHandler is class-wide).
        BoundsProperty.Changed.AddClassHandler<Magnifier>((m, e) =>
        {
            if (e.OldValue is Rect oldBounds && e.NewValue is Rect newBounds && oldBounds.Size == newBounds.Size)
                return;
            m.UpdateViewBox(); // UpdateViewBox already calls InvalidateVisual().
        });
    }

    public Magnifier() { }

    // -----------------------------------------------------------------------
    // Overrides
    // -----------------------------------------------------------------------
    public override void Render(DrawingContext context)
    {
        if (_visualBrush == null || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        var strokeThickness = (BorderThickness.Left + BorderThickness.Top +
                               BorderThickness.Right + BorderThickness.Bottom) / 4;
        var pen = BorderBrush != null ? new Pen(BorderBrush, strokeThickness) : null;

        if (FrameType == EFrameType.Circle)
        {
            var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
            var rx = Bounds.Width / 2;
            var ry = Bounds.Height / 2;
            if (Background != null)
                context.DrawEllipse(Background, null, center, rx, ry);
            context.DrawEllipse(_visualBrush, pen, center, rx, ry);
        }
        else
        {
            var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
            if (Background != null)
                context.DrawRectangle(Background, null, rect);
            context.DrawRectangle(_visualBrush, pen, rect);
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    public void Freeze(bool freeze)
    {
        IsFrozen = freeze;
    }

    private void OnFrameTypeChanged()
    {
        UpdateSizeFromRadius();
        InvalidateVisual();
    }

    private void OnRadiusChanged()
    {
        UpdateSizeFromRadius();
    }

    private void UpdateSizeFromRadius()
    {
        if (FrameType != EFrameType.Circle)
            return;

        var newSize = Radius * 2;
        if (!Helper.AreVirtuallyEqual(Width, newSize))
            Width = newSize;
        if (!Helper.AreVirtuallyEqual(Height, newSize))
            Height = newSize;
    }

    internal void UpdateViewBox()
    {
        // Prefer actual layout size; fall back to the Width/Height styled properties
        // (default 100) so that ViewBox.Size is non-zero even before the first layout
        // pass. This ensures CalculateViewBoxLocation() can center the viewport under
        // the cursor on the very first PointerPressed before Bounds are measured.
        var w = Bounds.Width > 0 ? Bounds.Width : Width;
        var h = Bounds.Height > 0 ? Bounds.Height : Height;
        if (w <= 0 || h <= 0)
            return;

        ViewBox = new Rect(ViewBox.X, ViewBox.Y, w * ZoomFactor, h * ZoomFactor);
        UpdateBrushViewBox();
        InvalidateVisual();
    }

    private void RebuildBrush()
    {
        if (Target == null)
        {
            _visualBrush = null;
            InvalidateVisual();
            return;
        }

        _visualBrush = new VisualBrush
        {
            SourceControl = Target,
            Stretch = Stretch.Fill,
            TileMode = TileMode.None,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            DestinationRect = new RelativeRect(0, 0, 1, 1, RelativeUnit.Relative),
        };
        UpdateBrushViewBox();
        InvalidateVisual();
    }

    private void UpdateBrushViewBox()
    {
        if (_visualBrush == null)
            return;
        // SourceRect maps which region of Target is painted — the magnified "window".
        _visualBrush.SourceRect = new RelativeRect(ViewBox, RelativeUnit.Absolute);
    }
}
