using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace FModel.Views.Resources.Controls.Aup;

public sealed class Timeline : UserControl
{
    // Visual structure built directly in the constructor — no ControlTemplate needed.
    private static readonly IBrush s_defaultProgressBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops = new GradientStops
        {
            new GradientStop(Color.FromRgb(0x45, 0xB6, 0x49), 0.0),
            new GradientStop(Color.FromRgb(0xDC, 0xE3, 0x5B), 1.0)
        }
    };

    private readonly Grid _lengthGrid = new();
    private readonly Border _progressLine = new()
    {
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Stretch,
        BorderThickness = new Thickness(0, 0, 1, 0),
        Width = 0
    };
    private readonly Border _positionLine = new()
    {
        Width = 1,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Stretch,
        IsVisible = false
    };
    // -----------------------------------------------------------------------
    // Styled properties
    // -----------------------------------------------------------------------

    public static readonly StyledProperty<ISource?> SourceProperty =
        AvaloniaProperty.Register<Timeline, ISource?>(nameof(Source));
    public ISource? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly StyledProperty<TimeSpan> PositionProperty =
        AvaloniaProperty.Register<Timeline, TimeSpan>(nameof(Position), defaultValue: TimeSpan.Zero);
    public TimeSpan Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public static readonly StyledProperty<IBrush?> TickBrushProperty =
        AvaloniaProperty.Register<Timeline, IBrush?>(nameof(TickBrush),
            defaultValue: new SolidColorBrush(Color.FromRgb(0x7F, 0x84, 0x8E)));
    public IBrush? TickBrush
    {
        get => GetValue(TickBrushProperty);
        set => SetValue(TickBrushProperty, value);
    }

    public static readonly StyledProperty<IBrush?> TimeBrushProperty =
        AvaloniaProperty.Register<Timeline, IBrush?>(nameof(TimeBrush),
            defaultValue: new SolidColorBrush(Color.FromRgb(0xDA, 0xE5, 0xF2)));
    public IBrush? TimeBrush
    {
        get => GetValue(TimeBrushProperty);
        set => SetValue(TimeBrushProperty, value);
    }

    public static readonly StyledProperty<IBrush?> ProgressLineBrushProperty =
        AvaloniaProperty.Register<Timeline, IBrush?>(nameof(ProgressLineBrush), defaultValue: Brushes.Brown);
    public IBrush? ProgressLineBrush
    {
        get => GetValue(ProgressLineBrushProperty);
        set => SetValue(ProgressLineBrushProperty, value);
    }

    public static readonly StyledProperty<IBrush?> ProgressBrushProperty =
        AvaloniaProperty.Register<Timeline, IBrush?>(nameof(ProgressBrush), defaultValue: s_defaultProgressBrush);
    public IBrush? ProgressBrush
    {
        get => GetValue(ProgressBrushProperty);
        set => SetValue(ProgressBrushProperty, value);
    }

    public static readonly StyledProperty<IBrush?> MousePositionBrushProperty =
        AvaloniaProperty.Register<Timeline, IBrush?>(nameof(MousePositionBrush), defaultValue: Brushes.Brown);
    public IBrush? MousePositionBrush
    {
        get => GetValue(MousePositionBrushProperty);
        set => SetValue(MousePositionBrushProperty, value);
    }

    // -----------------------------------------------------------------------
    // Static ctor — wire property-changed callbacks
    // -----------------------------------------------------------------------
    static Timeline()
    {
        SourceProperty.Changed.AddClassHandler<Timeline>((m, e) =>
            m.OnSourceChanged(e.GetOldValue<ISource?>(), e.GetNewValue<ISource?>()));

        TickBrushProperty.Changed.AddClassHandler<Timeline>((m, _) => Dispatcher.UIThread.Post(m.UpdateTimeline));
        TimeBrushProperty.Changed.AddClassHandler<Timeline>((m, _) => Dispatcher.UIThread.Post(m.UpdateTimeline));

        ProgressBrushProperty.Changed.AddClassHandler<Timeline>((m, e) =>
            m._progressLine.Background = e.GetNewValue<IBrush?>());
        MousePositionBrushProperty.Changed.AddClassHandler<Timeline>((m, e) =>
            m._positionLine.Background = e.GetNewValue<IBrush?>());
        ProgressLineBrushProperty.Changed.AddClassHandler<Timeline>((m, e) =>
            m._progressLine.BorderBrush = e.GetNewValue<IBrush?>());
        PositionProperty.Changed.AddClassHandler<Timeline>((m, e) =>
        {
            var position = e.GetNewValue<TimeSpan>();
            var totalMs = m._source?.PlayedFile?.Duration.TotalMilliseconds ?? 0;
            m._progressLine.Width = totalMs > 0
                ? position.TotalMilliseconds / totalMs * m._lengthGrid.Bounds.Width
                : 0;
        });

        // Replaces WPF OnRenderSizeChanged — re-draw ticks and recompute progress width when size changes.
        BoundsProperty.Changed.AddClassHandler<Timeline>((m, e) =>
        {
            if (e.OldValue is Rect old && e.NewValue is Rect nw && old.Size != nw.Size)
            {
                m.UpdateTimeline();
                var totalMs = m._source?.PlayedFile?.Duration.TotalMilliseconds ?? 0;
                if (totalMs > 0)
                    m._progressLine.Width = m.Position.TotalMilliseconds / totalMs * nw.Width;
            }
        });
    }

    public Timeline()
    {
        // Initialise visual brushes from default property values.
        _progressLine.Background = ProgressBrush;
        _progressLine.BorderBrush = ProgressLineBrush;
        _positionLine.Background = MousePositionBrush;

        // Two-row layout faithful to the WPF PART_Timeline template:
        //   Row 0 (20px): _lengthGrid (tick marks + time labels)
        //   Row 1 (*):    _progressLine (playback progress fill + right-edge cursor border)
        //   _positionLine (mouse cursor indicator) spans both rows.
        var root = new Grid { ClipToBounds = true };
        root.RowDefinitions.Add(new RowDefinition(20, GridUnitType.Pixel));
        root.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));

        _lengthGrid.ClipToBounds = true;
        Grid.SetRow(_lengthGrid, 0);
        Grid.SetRow(_positionLine, 0);
        Grid.SetRowSpan(_positionLine, 2);
        Grid.SetRow(_progressLine, 1);

        root.Children.Add(_lengthGrid);
        root.Children.Add(_progressLine);
        root.Children.Add(_positionLine); // on top — must remain visible over the advancing progress fill

        root.PointerEntered += (_, _) => _positionLine.IsVisible = true;
        root.PointerExited += (_, _) => _positionLine.IsVisible = false;
        root.PointerMoved += OnOverlayPointerMoved;
        root.PointerPressed += OnOverlayPointerPressed;

        Content = root;
    }

    // -----------------------------------------------------------------------
    // Pointer handlers (replaces WPF MouseEnter/Leave/Move/LeftButtonDown)
    // -----------------------------------------------------------------------

    private void OnOverlayPointerMoved(object? sender, PointerEventArgs e)
    {
        var x = e.GetPosition((Visual?) sender).X;
        _positionLine.Margin = new Thickness(x, 0, 0, 0);
    }

    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            return;
        if (Source == null)
            return;
        var overlayWidth = ((Control?) sender)?.Bounds.Width ?? 0;
        if (overlayWidth <= 0)
            return;
        var ratio = e.GetPosition((Visual?) sender).X / overlayWidth;
        Source.SkipTo(Math.Clamp(ratio, 0.0, 1.0));
    }

    // -----------------------------------------------------------------------
    // Source wiring
    // -----------------------------------------------------------------------

    private ISource? _source;

    private void OnSourceChanged(ISource? oldValue, ISource? newValue)
    {
        if (oldValue != null)
        {
            oldValue.SourceEvent -= OnSourceEvent;
            oldValue.SourcePropertyChangedEvent -= OnSourcePropertyChangedEvent;
        }

        _source = newValue;
        if (_source == null)
            return;

        _source.SourceEvent += OnSourceEvent;
        _source.SourcePropertyChangedEvent += OnSourcePropertyChangedEvent;
        Dispatcher.UIThread.Post(UpdateTimeline);
    }

    private void OnSourceEvent(object? sender, SourceEventArgs e)
    {
        if (Source == null)
            return;
        Dispatcher.UIThread.Post(UpdateTimeline);
    }

    private void OnSourcePropertyChangedEvent(object? sender, SourcePropertyChangedEventArgs e)
    {
        if (e.Property != ESourceProperty.Position)
            return;

        // Setting Position triggers PositionProperty.Changed which updates _progressLine.Width.
        var position = (TimeSpan) e.Value;
        Dispatcher.UIThread.Post(() => Position = position);
    }

    // -----------------------------------------------------------------------
    // Timeline rendering (replaces WPF UpdateTimeline)
    // -----------------------------------------------------------------------

    private void UpdateTimeline()
    {
        if (_source == null || _source.PlayedFile.Duration == TimeSpan.Zero)
            return;

        var width = _lengthGrid.Bounds.Width;
        var height = _lengthGrid.Bounds.Height;
        if (width < 1 || height < 1)
            return;

        _lengthGrid.Children.Clear();

        var tickBrush = TickBrush;
        var timeBrush = TimeBrush;

        // Bottom border line
        _lengthGrid.Children.Add(new Border
        {
            Height = 1,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = tickBrush
        });

        var majorTickCount = Math.Floor(width / 100);
        var totalSeconds = _source.PlayedFile.Duration.TotalSeconds;
        var majorTickSecondInterval = Math.Floor(totalSeconds / majorTickCount);
        majorTickSecondInterval = Math.Ceiling(majorTickSecondInterval / 10) * 10;
        var minorTickInterval = majorTickSecondInterval / 5 == 0 ? 1 : majorTickSecondInterval / 5;
        var minorTickCount = totalSeconds / minorTickInterval;

        for (var i = 0; i < minorTickCount; i++)
        {
            var interval = i * minorTickInterval;
            var positionPercent = interval / totalSeconds;
            var x = positionPercent * width;

            if (interval % majorTickSecondInterval != 0)
            {
                // Minor tick
                _lengthGrid.Children.Add(new Border
                {
                    Width = 1,
                    Height = 7,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Background = tickBrush,
                    Margin = new Thickness(x, 0, 0, 0)
                });
            }
            else
            {
                // Major tick — full height
                _lengthGrid.Children.Add(new Border
                {
                    Width = 1,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Background = tickBrush,
                    Margin = new Thickness(x, 0, 0, 0)
                });

                // Time label
                var ts = TimeSpan.FromSeconds(interval);
                _lengthGrid.Children.Add(new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = timeBrush,
                    Text = ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss"),
                    Margin = new Thickness(x + 5, 0, 0, 7)
                });
            }
        }
    }
}
