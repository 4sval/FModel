using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FModel.Views.Resources.Controls;

[TemplatePart(Name = PART_VisualBrush, Type = typeof(VisualBrush))]
public class Magnifier : Control
{
    private const double DEFAULT_SIZE = 100d;
    private const string PART_VisualBrush = "PART_VisualBrush";
    private VisualBrush _visualBrush = new();

    public static readonly DependencyProperty FrameTypeProperty =
        DependencyProperty.Register("FrameType", typeof(EFrameType), typeof(Magnifier), new UIPropertyMetadata(EFrameType.Circle, OnFrameTypeChanged));
    public EFrameType FrameType
    {
        get => (EFrameType) GetValue(FrameTypeProperty);
        set => SetValue(FrameTypeProperty, value);
    }

    private static void OnFrameTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var m = (Magnifier) d;
        m.OnFrameTypeChanged((EFrameType) e.OldValue, (EFrameType) e.NewValue);
    }

    protected virtual void OnFrameTypeChanged(EFrameType oldValue, EFrameType newValue)
    {
        UpdateSizeFromRadius();
    }

    public static readonly DependencyProperty IsUsingZoomOnMouseWheelProperty =
        DependencyProperty.Register("IsUsingZoomOnMouseWheel", typeof(bool), typeof(Magnifier), new UIPropertyMetadata(true));
    public bool IsUsingZoomOnMouseWheel
    {
        get => (bool) GetValue(IsUsingZoomOnMouseWheelProperty);
        set => SetValue(IsUsingZoomOnMouseWheelProperty, value);
    }

    public bool IsFrozen { get; private set; }

    public static readonly DependencyProperty RadiusProperty =
        DependencyProperty.Register("Radius", typeof(double), typeof(Magnifier), new FrameworkPropertyMetadata(DEFAULT_SIZE / 2, OnRadiusPropertyChanged));
    public double Radius
    {
        get => (double) GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    private static void OnRadiusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var m = (Magnifier) d;
        m.OnRadiusChanged(e);
    }

    protected virtual void OnRadiusChanged(DependencyPropertyChangedEventArgs e)
    {
        UpdateSizeFromRadius();
    }

    public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(UIElement), typeof(Magnifier));
    public UIElement Target
    {
        get => (UIElement) GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public Rect ViewBox
    {
        get => _visualBrush.Viewbox;
        set => _visualBrush.Viewbox = value;
    }

    public static readonly DependencyProperty ZoomFactorProperty =
        DependencyProperty.Register("ZoomFactor", typeof(double), typeof(Magnifier), new FrameworkPropertyMetadata(0.5, OnZoomFactorPropertyChanged), OnValidationCallback);
    public double ZoomFactor
    {
        get => (double) GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    private static bool OnValidationCallback(object baseValue)
    {
        return (double) baseValue >= 0;
    }

    private static void OnZoomFactorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var m = (Magnifier) d;
        m.OnZoomFactorChanged(e);
    }

    protected virtual void OnZoomFactorChanged(DependencyPropertyChangedEventArgs e)
    {
        UpdateViewBox();
    }

    public static readonly DependencyProperty ZoomFactorOnMouseWheelProperty =
        DependencyProperty.Register("ZoomFactorOnMouseWheel", typeof(double), typeof(Magnifier), new FrameworkPropertyMetadata(0.1d, OnZoomFactorOnMouseWheelPropertyChanged), OnZoomFactorOnMouseWheelValidationCallback);
    public double ZoomFactorOnMouseWheel
    {
        get => (double) GetValue(ZoomFactorOnMouseWheelProperty);
        set => SetValue(ZoomFactorOnMouseWheelProperty, value);
    }

    private static bool OnZoomFactorOnMouseWheelValidationCallback(object baseValue)
    {
        return (double) baseValue >= 0;
    }

    private static void OnZoomFactorOnMouseWheelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var m = (Magnifier) d;
        m.OnZoomFactorOnMouseWheelChanged(e);
    }

    protected virtual void OnZoomFactorOnMouseWheelChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    static Magnifier()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Magnifier), new FrameworkPropertyMetadata(typeof(Magnifier)));
        HeightProperty.OverrideMetadata(typeof(Magnifier), new FrameworkPropertyMetadata(DEFAULT_SIZE));
        WidthProperty.OverrideMetadata(typeof(Magnifier), new FrameworkPropertyMetadata(DEFAULT_SIZE));
    }

    public Magnifier()
    {
        SizeChanged += OnSizeChangedEvent;
    }

    private void OnSizeChangedEvent(object sender, SizeChangedEventArgs e)
    {
        UpdateViewBox();
    }

    private void UpdateSizeFromRadius()
    {
        if (FrameType != EFrameType.Circle) return;

        var newSize = Radius * 2;
        if (!Helper.AreVirtuallyEqual(Width, newSize))
        {
            Width = newSize;
        }

        if (!Helper.AreVirtuallyEqual(Height, newSize))
        {
            Height = newSize;
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        var newBrush = GetTemplateChild(PART_VisualBrush) as VisualBrush ?? new VisualBrush();
        newBrush.Viewbox = _visualBrush.Viewbox;
        _visualBrush = newBrush;
    }

    public void Freeze(bool freeze)
    {
        IsFrozen = freeze;
    }

    private void UpdateViewBox()
    {
        if (!IsInitialized)
            return;

        ViewBox = new Rect(ViewBox.Location, new Size(ActualWidth * ZoomFactor, ActualHeight * ZoomFactor));
    }
}