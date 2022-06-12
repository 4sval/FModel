using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FModel.Views.Resources.Controls.Aup;

[TemplatePart(Name = "PART_ControlContainer")]
[TemplatePart(Name = "PART_TimelineArea", Type = typeof(Border))]
[TemplatePart(Name = "PART_MousePosition", Type = typeof(Rectangle))]
[TemplatePart(Name = "PART_Timeline", Type = typeof(Grid))]
[TemplatePart(Name = "PART_Length", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_ProgressLine", Type = typeof(Border))]
public sealed class Timeline : UserControl
{
    private Grid _controlContainer;
    private Border _timelineArea;
    private Rectangle _position;
    private Grid _timelineGrid;
    private Grid _lengthGrid;
    private Border _progressLine;

    static Timeline()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata(typeof(Timeline)));
    }

    private ISource _source;
    public ISource Source
    {
        get => (ISource) GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(ISource), typeof(Timeline),
            new UIPropertyMetadata(null, OnSourceChanged, OnCoerceSource));

    public TimeSpan Position
    {
        get => (TimeSpan) GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }
    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.Register("Position", typeof(TimeSpan), typeof(Timeline),
            new UIPropertyMetadata(TimeSpan.Zero, OnPositionChanged, OnCoercePosition));

    public Brush TickBrush
    {
        get => (Brush) GetValue(TickBrushProperty);
        set => SetValue(TickBrushProperty, value);
    }
    public static readonly DependencyProperty TickBrushProperty =
        DependencyProperty.Register("TickBrush", typeof(Brush), typeof(Timeline),
            new UIPropertyMetadata(Brushes.Red, OnTickBrushChanged, OnCoerceTickBrush));

    public Brush TimeBrush
    {
        get => (Brush) GetValue(TimeBrushProperty);
        set => SetValue(TimeBrushProperty, value);
    }
    public static readonly DependencyProperty TimeBrushProperty =
        DependencyProperty.Register("TimeBrush", typeof(Brush), typeof(Timeline),
            new UIPropertyMetadata(Brushes.Blue, OnTimeBrushChanged, OnCoerceTimeBrush));

    public Brush ProgressLineBrush
    {
        get => (Brush) GetValue(ProgressLineBrushProperty);
        set => SetValue(ProgressLineBrushProperty, value);
    }
    public static readonly DependencyProperty ProgressLineBrushProperty =
        DependencyProperty.Register("ProgressLineBrush", typeof(Brush), typeof(Timeline),
            new UIPropertyMetadata(Brushes.Violet, OnProgressLineBrushChanged, OnCoerceProgressLineBrush));

    public Brush ProgressBrush
    {
        get => (Brush) GetValue(ProgressBrushProperty);
        set => SetValue(ProgressBrushProperty, value);
    }
    public static readonly DependencyProperty ProgressBrushProperty =
        DependencyProperty.Register("ProgressBrush", typeof(Brush), typeof(Timeline),
            new UIPropertyMetadata(Brushes.DarkGreen, OnProgressBrushChanged, OnCoerceProgressBrush));

    public Brush MousePositionBrush
    {
        get => (Brush) GetValue(MousePositionBrushProperty);
        set => SetValue(MousePositionBrushProperty, value);
    }
    public static readonly DependencyProperty MousePositionBrushProperty =
        DependencyProperty.Register("MousePositionBrush", typeof(Brush), typeof(Timeline),
            new UIPropertyMetadata(Brushes.Brown, OnMousePositionBrushChanged, OnCoerceMousePositionBrush));

    private void OnSourceChanged(ISource oldValue, ISource newValue)
    {
        _source = Source;
        _source.SourceEvent += OnSourceEvent;
        _source.SourcePropertyChangedEvent += OnSourcePropertyChangedEvent;
        OnSourceEvent(this, null);
    }

    private void OnSourceEvent(object sender, SourceEventArgs e)
    {
        if (Source == null) return;
        Dispatcher.BeginInvoke((Action) UpdateTimeline);
    }

    private void OnSourcePropertyChangedEvent(object sender, SourcePropertyChangedEventArgs e)
    {
        if (e.Property != ESourceProperty.Position) return;

        var position = (TimeSpan) e.Value;
        Dispatcher.BeginInvoke((Action) delegate
        {
            Position = position;
            if (_lengthGrid == null) return;

            var x = 0d;
            if (_source.PlayedFile.Duration.TotalMilliseconds != 0)
            {
                x = position.TotalMilliseconds / _source.PlayedFile.Duration.TotalMilliseconds * _lengthGrid.RenderSize.Width;
            }

            _progressLine.Width = x;
        });
    }

    private ISource OnCoerceSource(ISource value) => value;
    private static object OnCoerceSource(DependencyObject o, object value)
    {
        if (o is Timeline timeline)
            return timeline.OnCoerceSource((ISource) value);
        return value;
    }

    private static void OnSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is Timeline timeline)
            timeline.OnSourceChanged((ISource) e.OldValue, (ISource) e.NewValue);
    }

    private TimeSpan OnCoercePosition(TimeSpan value) => value;
    private static object OnCoercePosition(DependencyObject o, object value)
    {
        if (o is Timeline timeline)
            return timeline.OnCoercePosition((TimeSpan) value);
        return value;
    }

    private void OnPositionChanged(TimeSpan oldValue, TimeSpan newValue)
    {
    }

    private static void OnPositionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is Timeline timeline)
            timeline.OnPositionChanged((TimeSpan) e.OldValue, (TimeSpan) e.NewValue);
    }

    private Brush OnCoerceTickBrush(Brush value) => value;
    private static object OnCoerceTickBrush(DependencyObject o, object value)
    {
        if (o is Timeline timeline)
            return timeline.OnCoerceTickBrush((Brush) value);
        return value;
    }

    private void OnTickBrushChanged(Brush oldValue, Brush newValue) => UpdateTimeline();
    private static void OnTickBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is Timeline timeline)
            timeline.OnTickBrushChanged((Brush) e.OldValue, (Brush) e.NewValue);
    }

    private Brush OnCoerceTimeBrush(Brush value) => value;
    private static object OnCoerceTimeBrush(DependencyObject o, object value)
    {
        if (o is Timeline timeline)
            return timeline.OnCoerceTimeBrush((Brush) value);
        return value;
    }

    private void OnTimeBrushChanged(Brush oldValue, Brush newValue) => UpdateTimeline();
    private static void OnTimeBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is Timeline timeline)
            timeline.OnTimeBrushChanged((Brush) e.OldValue, (Brush) e.NewValue);
    }

    private Brush OnCoerceProgressLineBrush(Brush value) => value;
    private static object OnCoerceProgressLineBrush(DependencyObject o, object value)
    {
        if (o is Timeline timeline)
            return timeline.OnCoerceProgressLineBrush((Brush) value);
        return value;
    }

    private void OnProgressLineBrushChanged(Brush oldValue, Brush newValue)
    {
    }

    private static void OnProgressLineBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is Timeline timeline)
            timeline.OnProgressLineBrushChanged((Brush) e.OldValue, (Brush) e.NewValue);
    }

    private Brush OnCoerceProgressBrush(Brush value) => value;
    private static object OnCoerceProgressBrush(DependencyObject o, object value)
    {
        if (o is Timeline timeline)
            return timeline.OnCoerceProgressBrush((Brush) value);
        return value;
    }

    private void OnProgressBrushChanged(Brush oldValue, Brush newValue)
    {
    }

    private static void OnProgressBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is Timeline timeline)
            timeline.OnProgressBrushChanged((Brush) e.OldValue, (Brush) e.NewValue);
    }

    private Brush OnCoerceMousePositionBrush(Brush value) => value;
    private static object OnCoerceMousePositionBrush(DependencyObject o, object value)
    {
        if (o is Timeline timeline)
            return timeline.OnCoerceMousePositionBrush((Brush) value);
        return value;
    }

    private void OnMousePositionBrushChanged(Brush oldValue, Brush newValue)
    {
    }

    private static void OnMousePositionBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is Timeline timeline)
            timeline.OnMousePositionBrushChanged((Brush) e.OldValue, (Brush) e.NewValue);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _controlContainer = GetTemplateChild("PART_ControlContainer") as Grid;
        _timelineGrid = GetTemplateChild("PART_Timeline") as Grid;
        _lengthGrid = GetTemplateChild("PART_Length") as Grid;
        _progressLine = GetTemplateChild("PART_ProgressLine") as Border;
        _timelineGrid.CacheMode = new BitmapCache();

        _timelineArea = GetTemplateChild("PART_TimelineArea") as Border;
        _position = GetTemplateChild("PART_MousePosition") as Rectangle;
        _timelineArea.MouseEnter += (_, _) => _position.Visibility = Visibility.Visible;
        _timelineArea.MouseLeave += (_, _) => _position.Visibility = Visibility.Collapsed;
        _timelineArea.MouseMove += (_, args)
            => _position.Margin = new Thickness(args.GetPosition(_timelineArea).X, 0, _position.ActualWidth, 0);
        _timelineArea.MouseLeftButtonDown += (_, args)
            => Source.SkipTo(args.GetPosition(_timelineArea).X / _timelineArea.ActualWidth);

        OnSourceEvent(this, null);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateTimeline();
    }

    private readonly Border _bottomBorder = new()
    {
        Height = 1,
        VerticalAlignment = VerticalAlignment.Bottom,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private void UpdateTimeline()
    {
        if (_source == null || _source.PlayedFile.Duration == TimeSpan.Zero || _lengthGrid == null ||
            _lengthGrid.RenderSize.Width < 1 || _lengthGrid.RenderSize.Height < 1) return;

        _lengthGrid.Children.Clear();

        // freeze brushes
        var tickBrush = TickBrush.Clone();
        var timeBrush = TimeBrush.Clone();
        tickBrush.Freeze();
        timeBrush.Freeze();

        // Draw the bottom border
        _bottomBorder.Background = tickBrush;
        _lengthGrid.Children.Add(_bottomBorder);

        // Determine the number of major ticks that we should display.
        // This depends on the width of the timeline.
        var width = _lengthGrid.RenderSize.Width;
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
                // Major tick
                _lengthGrid.Children.Add(new Border
                {
                    Width = 1,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Background = tickBrush,
                    Margin = new Thickness(x, 0, 0, 0)
                });

                // Add time label
                var time = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = timeBrush
                };
                var ts = TimeSpan.FromSeconds(interval);
                time.Text = ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
                time.Margin = new Thickness(x + 5, 0, 0, 7);
                _lengthGrid.Children.Add(time);
            }
        }
    }
}