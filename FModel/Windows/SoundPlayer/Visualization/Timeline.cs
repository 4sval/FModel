using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FModel.Windows.SoundPlayer.Visualization
{
    [TemplatePart(Name = "PART_Timeline", Type = typeof(Grid))]
    [TemplatePart(Name = "PART_Length", Type = typeof(Canvas))]
    [TemplatePart(Name = "PART_ControlContainer")]
    [TemplatePart(Name = "PART_ProgressLine", Type = typeof(Border))]
    public class Timeline : Control
    {
        private ISource _source;
        private Grid timelineGrid;
        private Grid controlContainer;
        private Grid lengthGrid;
        private Border progressLine;

        #region Dependency Properties

        #region Source Property

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(ISource),
            typeof(Timeline), new UIPropertyMetadata(null, OnSourceChanged, OnCoerceSource));

        private static object OnCoerceSource(DependencyObject o, object value)
        {
            if (o is Timeline timeline)
                return timeline.OnCoerceSource((ISource)value);
            else
                return value;
        }

        private static void OnSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeline timeline)
                timeline.OnSourceChanged((ISource)e.OldValue, (ISource)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="Source"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="Source"/></param>
        /// <returns>The adjusted value of <see cref="Source"/></returns>
        protected virtual ISource OnCoerceSource(ISource value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="Source"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="Source"/></param>
        /// <param name="newValue">The new value of <see cref="Source"/></param>
        protected virtual void OnSourceChanged(ISource oldValue, ISource newValue)
        {
            _source = Source;
            _source.SourceEvent += Source_SourceEvent;
            _source.SourcePropertyChangedEvent += Source_SourcePropertyChangedEvent;

            Source_SourceEvent(this, null);
        }

        private void Source_SourceEvent(object sender, SourceEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                if (Source.Length > TimeSpan.Zero)
                {
                    if (progressLine != null)
                    {
                        // The source has data. Show various UI.
                        progressLine.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    if (progressLine != null)
                    {
                        // The source does not have data. Hide various UI.
                        progressLine.Visibility = Visibility.Collapsed;
                    }
                }

                UpdateTimeline();
            });
        }

        private void Source_SourcePropertyChangedEvent(object sender, SourcePropertyChangedEventArgs e)
        {
            if (e.Property == ESourceProperty.Position)
            {
                TimeSpan position = (TimeSpan)e.Value;
                Dispatcher.BeginInvoke((Action)delegate
                {
                    Position = position;

                    if (lengthGrid != null && progressLine != null)
                    {
                        var x = 0d;

                        if (_source.Length.TotalMilliseconds != 0)
                        {
                            double progressPercent = position.TotalMilliseconds / _source.Length.TotalMilliseconds;
                            x = progressPercent * lengthGrid.RenderSize.Width;
                        }

                        progressLine.Width = x;
                    }
                });
            }
        }

        /// <summary>
        /// Gets or sets a Source for the Timeline.
        /// </summary>        
        public ISource Source
        {
            get
            {
                return (ISource)GetValue(SourceProperty);
            }
            set
            {
                SetValue(SourceProperty, value);
            }
        }

        #endregion

        #region Position Property

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position", typeof(TimeSpan),
            typeof(Timeline), new UIPropertyMetadata(TimeSpan.Zero, OnPositionChanged, OnCoercePosition));

        private static object OnCoercePosition(DependencyObject o, object value)
        {
            if (o is Timeline timeline)
                return timeline.OnCoercePosition((TimeSpan)value);
            else
                return value;
        }

        private static void OnPositionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeline timeline)
                timeline.OnPositionChanged((TimeSpan)e.OldValue, (TimeSpan)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="Position"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="Position"/></param>
        /// <returns>The adjusted value of <see cref="Position"/></returns>
        protected virtual TimeSpan OnCoercePosition(TimeSpan value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="Position"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="Position"/></param>
        /// <param name="newValue">The new value of <see cref="Position"/></param>
        protected virtual void OnPositionChanged(TimeSpan oldValue, TimeSpan newValue)
        {

        }

        /// <summary>
        /// Gets or sets a Position for the Timeline.
        /// </summary>        
        public TimeSpan Position
        {
            get
            {
                return (TimeSpan)GetValue(PositionProperty);
            }
            set
            {
                SetValue(PositionProperty, value);
            }
        }

        #endregion

        #region TickBrush Property

        public static readonly DependencyProperty TickBrushProperty = DependencyProperty.Register("TickBrush", typeof(Brush),
            typeof(Timeline), new UIPropertyMetadata(Brushes.Silver, OnTickBrushChanged, OnCoerceTickBrush));

        private static object OnCoerceTickBrush(DependencyObject o, object value)
        {
            if (o is Timeline timeline)
                return timeline.OnCoerceTickBrush((Brush)value);
            else
                return value;
        }

        private static void OnTickBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeline timeline)
                timeline.OnTickBrushChanged((Brush)e.OldValue, (Brush)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="TickBrush"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="TickBrush"/></param>
        /// <returns>The adjusted value of <see cref="TickBrush"/></returns>
        protected virtual Brush OnCoerceTickBrush(Brush value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="TickBrush"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="TickBrush"/></param>
        /// <param name="newValue">The new value of <see cref="TickBrush"/></param>
        protected virtual void OnTickBrushChanged(Brush oldValue, Brush newValue)
        {
            UpdateTimeline();
        }

        /// <summary>
        /// Gets or sets a TickBrush for the Timeline.
        /// </summary>        
        public Brush TickBrush
        {
            get
            {
                return (Brush)GetValue(TickBrushProperty);
            }
            set
            {
                SetValue(TickBrushProperty, value);
            }
        }

        #endregion

        #region TimeBrush Property

        public static readonly DependencyProperty TimeBrushProperty = DependencyProperty.Register("TimeBrush", typeof(Brush),
            typeof(Timeline), new UIPropertyMetadata(Brushes.Silver, OnTimeBrushChanged, OnCoerceTimeBrush));

        private static object OnCoerceTimeBrush(DependencyObject o, object value)
        {
            if (o is Timeline timeline)
                return timeline.OnCoerceTimeBrush((Brush)value);
            else
                return value;
        }

        private static void OnTimeBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeline timeline)
                timeline.OnTimeBrushChanged((Brush)e.OldValue, (Brush)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="TimeBrush"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="TimeBrush"/></param>
        /// <returns>The adjusted value of <see cref="TimeBrush"/></returns>
        protected virtual Brush OnCoerceTimeBrush(Brush value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="TimeBrush"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="TimeBrush"/></param>
        /// <param name="newValue">The new value of <see cref="TimeBrush"/></param>
        protected virtual void OnTimeBrushChanged(Brush oldValue, Brush newValue)
        {
            UpdateTimeline();
        }

        /// <summary>
        /// Gets or sets a TimeBrush for the Timeline.
        /// </summary>        
        public Brush TimeBrush
        {
            get
            {
                return (Brush)GetValue(TimeBrushProperty);
            }
            set
            {
                SetValue(TimeBrushProperty, value);
            }
        }

        #endregion

        #region ProgressLineBrush Property

        public static readonly DependencyProperty ProgressLineBrushProperty = DependencyProperty.Register("ProgressLineBrush", typeof(Brush),
            typeof(Timeline), new UIPropertyMetadata(Brushes.Silver, OnProgressLineBrushChanged, OnCoerceProgressLineBrush));

        private static object OnCoerceProgressLineBrush(DependencyObject o, object value)
        {
            if (o is Timeline timeline)
                return timeline.OnCoerceProgressLineBrush((Brush)value);
            else
                return value;
        }

        private static void OnProgressLineBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeline timeline)
                timeline.OnProgressLineBrushChanged((Brush)e.OldValue, (Brush)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="ProgressLineBrush"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="ProgressLineBrush"/></param>
        /// <returns>The adjusted value of <see cref="ProgressLineBrush"/></returns>
        protected virtual Brush OnCoerceProgressLineBrush(Brush value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="ProgressLineBrush"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="ProgressLineBrush"/></param>
        /// <param name="newValue">The new value of <see cref="ProgressLineBrush"/></param>
        protected virtual void OnProgressLineBrushChanged(Brush oldValue, Brush newValue)
        {

        }

        /// <summary>
        /// Gets or sets a ProgressLineBrush for the Timeline.
        /// </summary>        
        public Brush ProgressLineBrush
        {
            get
            {
                return (Brush)GetValue(ProgressLineBrushProperty);
            }
            set
            {
                SetValue(ProgressLineBrushProperty, value);
            }
        }

        #endregion

        #region ProgressBrush Property

        public static readonly DependencyProperty ProgressBrushProperty = DependencyProperty.Register("ProgressBrush", typeof(Brush),
            typeof(Timeline), new UIPropertyMetadata(Brushes.LightBlue, OnProgressBrushChanged, OnCoerceProgressBrush));

        private static object OnCoerceProgressBrush(DependencyObject o, object value)
        {
            if (o is Timeline timeline)
                return timeline.OnCoerceProgressBrush((Brush)value);
            else
                return value;
        }

        private static void OnProgressBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeline timeline)
                timeline.OnProgressBrushChanged((Brush)e.OldValue, (Brush)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="ProgressBrush"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="ProgressBrush"/></param>
        /// <returns>The adjusted value of <see cref="ProgressBrush"/></returns>
        protected virtual Brush OnCoerceProgressBrush(Brush value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="ProgressBrush"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="ProgressBrush"/></param>
        /// <param name="newValue">The new value of <see cref="ProgressBrush"/></param>
        protected virtual void OnProgressBrushChanged(Brush oldValue, Brush newValue)
        {

        }

        /// <summary>
        /// Gets or sets a ProgressBrush for the Timeline.
        /// </summary>        
        public Brush ProgressBrush
        {
            get
            {
                return (Brush)GetValue(ProgressBrushProperty);
            }
            set
            {
                SetValue(ProgressBrushProperty, value);
            }
        }

        #endregion

        #endregion

        static Timeline()
        {
            Timeline.DefaultStyleKeyProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata(typeof(Timeline)));

            App.SetFramerate();
        }

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            timelineGrid = GetTemplateChild("PART_Timeline") as Grid;
            controlContainer = GetTemplateChild("PART_ControlContainer") as Grid;
            lengthGrid = GetTemplateChild("PART_Length") as Grid;
            progressLine = GetTemplateChild("PART_ProgressLine") as Border;
            timelineGrid.CacheMode = new BitmapCache();

            Source_SourceEvent(this, null);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateTimeline();
        }

        #endregion

        private void UpdateTimeline()
        {
            if (_source == null || Source.Length == TimeSpan.Zero || lengthGrid == null || lengthGrid.RenderSize.Width < 1 ||
                lengthGrid.RenderSize.Height < 1)
            {
                return;
            }

            lengthGrid.Children.Clear();

            // freeze brushes
            var tickBrush = TickBrush.Clone();
            var timeBrush = TimeBrush.Clone();
            tickBrush.Freeze();
            timeBrush.Freeze();

            // Draw the bottom border
            var bottomBorder = new Border
            {
                Background = tickBrush,
                Height = 1,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            lengthGrid.Children.Add(bottomBorder);

            // Determine the number of major ticks that we should display.
            // This depends on the width of the timeline.
            var width = lengthGrid.RenderSize.Width;
            var majorTickCount = Math.Floor(width / 100);
            var totalSeconds = _source.Length.TotalSeconds;
            var majorTickSecondInterval = Math.Floor(totalSeconds / majorTickCount);
            majorTickSecondInterval = Math.Ceiling(majorTickSecondInterval / 10) * 10;
            var minorTickInterval = majorTickSecondInterval / 5 == 0 ? 1 : majorTickSecondInterval / 5;
            var minorTickCount = totalSeconds / minorTickInterval;

            for (var i = 0; i < minorTickCount; i++)
            {
                var interval = i * minorTickInterval;
                double positionPercent = interval / totalSeconds;
                double x = positionPercent * width;

                if (interval % majorTickSecondInterval != 0)
                {
                    // Minor tick
                    var tick = new Border
                    {
                        Width = 1,
                        Height = 7,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Background = tickBrush,
                        Margin = new Thickness(x, 0, 0, 0)
                    };
                    lengthGrid.Children.Add(tick);
                }
                else
                {
                    // Major tick
                    var tick = new Border
                    {
                        Width = 1,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Background = tickBrush,
                        Margin = new Thickness(x, 0, 0, 0)
                    };
                    lengthGrid.Children.Add(tick);

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
                    lengthGrid.Children.Add(time);
                }
            }
        }
    }
}
