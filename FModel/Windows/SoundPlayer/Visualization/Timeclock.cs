using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FModel.Windows.SoundPlayer.Visualization
{
    [TemplatePart(Name = "PART_Timeclock", Type = typeof(Grid))]
    [TemplatePart(Name = "PART_Time", Type = typeof(TextBlock))]
    public class Timeclock : Control
    {
        private const string defaultFormat = @"hh\:mm\:ss\.ff";

        private ISource _activeSource;
        private readonly ObservableCollection<ISource> _sourceCollection = new ObservableCollection<ISource>();
        public ObservableCollection<ISource> SourceCollection { get { return _sourceCollection; } }

        private Grid timeclockGrid;
        private TextBlock timeText;

        private TimeSpan _elapsedTotal;
        private TimeSpan _remainingTotal;

        #region Dependency Properties

        #region Clock Type Property

        public static readonly DependencyProperty ClockTypeProperty = DependencyProperty.Register("ClockType", typeof(EClockType),
            typeof(Timeclock), new UIPropertyMetadata(EClockType.TimeElapsed, OnClockTypeChanged, OnCoerceClockType));

        private static object OnCoerceClockType(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceClockType((EClockType)value);
            else
                return value;
        }

        private static void OnClockTypeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnClockTypeChanged((EClockType)e.OldValue, (EClockType)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="ClockType"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="ClockType"/></param>
        /// <returns>The adjusted value of <see cref="ClockType"/></returns>
        protected virtual EClockType OnCoerceClockType(EClockType value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="ClockType"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="ClockType"/></param>
        /// <param name="newValue">The new value of <see cref="ClockType"/></param>
        protected virtual void OnClockTypeChanged(EClockType oldValue, EClockType newValue)
        {
            CalculateTime();
        }

        /// <summary>
        /// Gets or sets a ClockType for the Timeclock.
        /// </summary>        
        public EClockType ClockType
        {
            get
            {
                return (EClockType)GetValue(ClockTypeProperty);
            }
            set
            {
                SetValue(ClockTypeProperty, value);
            }
        }

        #endregion

        #region Label Font Property

        public static readonly DependencyProperty LabelFontProperty = DependencyProperty.Register("LabelFont", typeof(FontFamily),
            typeof(Timeclock), new UIPropertyMetadata(new FontFamily("Segoe UI"), OnLabelFontChanged, OnCoerceLabelFont));

        private static object OnCoerceLabelFont(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceLabelFont((FontFamily)value);
            else
                return value;
        }

        private static void OnLabelFontChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnLabelFontChanged((FontFamily)e.OldValue, (FontFamily)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="LabelFont"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="LabelFont"/></param>
        /// <returns>The adjusted value of <see cref="LabelFont"/></returns>
        protected virtual FontFamily OnCoerceLabelFont(FontFamily value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="LabelFont"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="LabelFont"/></param>
        /// <param name="newValue">The new value of <see cref="LabelFont"/></param>
        protected virtual void OnLabelFontChanged(FontFamily oldValue, FontFamily newValue)
        {

        }

        /// <summary>
        /// Gets or sets a LabelFont for the Timeclock.
        /// </summary>        
        public FontFamily LabelFont
        {
            get
            {
                return (FontFamily)GetValue(LabelFontProperty);
            }
            set
            {
                SetValue(LabelFontProperty, value);
            }
        }

        #endregion

        #region Label Foreground Property

        public static readonly DependencyProperty LabelForegroundProperty = DependencyProperty.Register("LabelForeground", typeof(Brush),
            typeof(Timeclock), new UIPropertyMetadata(Brushes.Silver, OnLabelForegroundChanged, OnCoerceLabelForeground));

        private static object OnCoerceLabelForeground(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceLabelForeground((Brush)value);
            else
                return value;
        }

        private static void OnLabelForegroundChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnLabelForegroundChanged((Brush)e.OldValue, (Brush)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="LabelForeground"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="LabelForeground"/></param>
        /// <returns>The adjusted value of <see cref="LabelForeground"/></returns>
        protected virtual Brush OnCoerceLabelForeground(Brush value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="LabelForeground"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="LabelForeground"/></param>
        /// <param name="newValue">The new value of <see cref="LabelForeground"/></param>
        protected virtual void OnLabelForegroundChanged(Brush oldValue, Brush newValue)
        {

        }

        /// <summary>
        /// Gets or sets a LabelForeground for the Timeclock.
        /// </summary>        
        public Brush LabelForeground
        {
            get
            {
                return (Brush)GetValue(LabelForegroundProperty);
            }
            set
            {
                SetValue(LabelForegroundProperty, value);
            }
        }

        #endregion

        #region Time Font Property

        public static readonly DependencyProperty TimeFontProperty = DependencyProperty.Register("TimeFont", typeof(FontFamily),
            typeof(Timeclock), new UIPropertyMetadata(new FontFamily("Ebrima"), OnTimeFontChanged, OnCoerceTimeFont));

        private static object OnCoerceTimeFont(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceTimeFont((FontFamily)value);
            else
                return value;
        }

        private static void OnTimeFontChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnTimeFontChanged((FontFamily)e.OldValue, (FontFamily)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="TimeFont"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="TimeFont"/></param>
        /// <returns>The adjusted value of <see cref="TimeFont"/></returns>
        protected virtual FontFamily OnCoerceTimeFont(FontFamily value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="TimeFont"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="TimeFont"/></param>
        /// <param name="newValue">The new value of <see cref="TimeFont"/></param>
        protected virtual void OnTimeFontChanged(FontFamily oldValue, FontFamily newValue)
        {

        }

        /// <summary>
        /// Gets or sets a TimeFont for the Timeclock.
        /// </summary>        
        public FontFamily TimeFont
        {
            get
            {
                return (FontFamily)GetValue(TimeFontProperty);
            }
            set
            {
                SetValue(TimeFontProperty, value);
            }
        }

        #endregion

        #region Time Foreground Property

        public static readonly DependencyProperty TimeForegroundProperty = DependencyProperty.Register("TimeForeground", typeof(Brush),
            typeof(Timeclock), new UIPropertyMetadata(Brushes.Silver, OnTimeForegroundChanged, OnCoerceTimeForeground));

        private static object OnCoerceTimeForeground(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceTimeForeground((Brush)value);
            else
                return value;
        }

        private static void OnTimeForegroundChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnTimeForegroundChanged((Brush)e.OldValue, (Brush)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="TimeForeground"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="TimeForeground"/></param>
        /// <returns>The adjusted value of <see cref="TimeForeground"/></returns>
        protected virtual Brush OnCoerceTimeForeground(Brush value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="TimeForeground"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="TimeForeground"/></param>
        /// <param name="newValue">The new value of <see cref="TimeForeground"/></param>
        protected virtual void OnTimeForegroundChanged(Brush oldValue, Brush newValue)
        {

        }

        /// <summary>
        /// Gets or sets a TimeForeground for the Timeclock.
        /// </summary>        
        public Brush TimeForeground
        {
            get
            {
                return (Brush)GetValue(TimeForegroundProperty);
            }
            set
            {
                SetValue(TimeForegroundProperty, value);
            }
        }

        #endregion

        #region Corner Radius Property

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius),
            typeof(Timeclock), new UIPropertyMetadata(new CornerRadius(3), OnCornerRadiusChanged, OnCoerceCornerRadius));

        private static object OnCoerceCornerRadius(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceCornerRadius((CornerRadius)value);
            else
                return value;
        }

        private static void OnCornerRadiusChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnCornerRadiusChanged((CornerRadius)e.OldValue, (CornerRadius)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="CornerRadius"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="CornerRadius"/></param>
        /// <returns>The adjusted value of <see cref="CornerRadius"/></returns>
        protected virtual CornerRadius OnCoerceCornerRadius(CornerRadius value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="CornerRadius"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="CornerRadius"/></param>
        /// <param name="newValue">The new value of <see cref="CornerRadius"/></param>
        protected virtual void OnCornerRadiusChanged(CornerRadius oldValue, CornerRadius newValue)
        {

        }

        /// <summary>
        /// Gets or sets a CornerRadius for the Timeclock.
        /// </summary>        
        public CornerRadius CornerRadius
        {
            get
            {
                return (CornerRadius)GetValue(CornerRadiusProperty);
            }
            set
            {
                SetValue(CornerRadiusProperty, value);
            }
        }

        #endregion

        #region Label Property

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string),
            typeof(Timeclock), new UIPropertyMetadata(string.Empty, OnLabelChanged, OnCoerceLabel));

        private static object OnCoerceLabel(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceLabel((string)value);
            else
                return value;
        }

        private static void OnLabelChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnLabelChanged((string)e.OldValue, (string)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="Label"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="Label"/></param>
        /// <returns>The adjusted value of <see cref="Label"/></returns>
        protected virtual string OnCoerceLabel(string value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="Label"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="Label"/></param>
        /// <param name="newValue">The new value of <see cref="Label"/></param>
        protected virtual void OnLabelChanged(string oldValue, string newValue)
        {

        }

        /// <summary>
        /// Gets or sets a Label for the Timeclock.
        /// </summary>        
        public string Label
        {
            get
            {
                return (string)GetValue(LabelProperty);
            }
            set
            {
                SetValue(LabelProperty, value);
            }
        }

        #endregion

        #region TimeFormat Property

        public static readonly DependencyProperty TimeFormatProperty = DependencyProperty.Register("TimeFormat", typeof(string),
            typeof(Timeclock), new UIPropertyMetadata(defaultFormat, OnTimeFormatChanged, OnCoerceTimeFormat));

        private static object OnCoerceTimeFormat(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceTimeFormat((string)value);
            else
                return value;
        }

        private static void OnTimeFormatChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnTimeFormatChanged((string)e.OldValue, (string)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="TimeFormat"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="TimeFormat"/></param>
        /// <returns>The adjusted value of <see cref="TimeFormat"/></returns>
        protected virtual string OnCoerceTimeFormat(string value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="TimeFormat"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="TimeFormat"/></param>
        /// <param name="newValue">The new value of <see cref="TimeFormat"/></param>
        protected virtual void OnTimeFormatChanged(string oldValue, string newValue)
        {
            try
            {
                TimeSpan.Zero.ToString(newValue);
            }
            catch
            {
                TimeFormat = defaultFormat;
            }

            CalculateTime();
        }

        /// <summary>
        /// Gets or sets a TimeFormat for the Timeclock.
        /// </summary>        
        public string TimeFormat
        {
            get
            {
                return (string)GetValue(TimeFormatProperty);
            }
            set
            {
                SetValue(TimeFormatProperty, value);
            }
        }

        #endregion

        #endregion

        static Timeclock()
        {
            Timeline.DefaultStyleKeyProperty.OverrideMetadata(typeof(Timeclock), new FrameworkPropertyMetadata(typeof(Timeclock)));

            App.SetFramerate();
        }

        public Timeclock()
        {
            SourceCollection.CollectionChanged += SourceCollection_CollectionChanged;
        }

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            timeclockGrid = GetTemplateChild("PART_Timeclock") as Grid;
            timeText = GetTemplateChild("PART_Time") as TextBlock;
            timeclockGrid.CacheMode = new BitmapCache();

            //_source_SourceEvent(this, null);
        }

        #endregion

        private void SourceCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var source in e.NewItems)
                {
                    var _source = (source as ISource);
                    _source.SourcePropertyChangedEvent += Source_SourcePropertyChangedEvent;
                    _activeSource = _source;
                }
            }
            if (e.OldItems != null)
            {
                foreach (var source in e.OldItems)
                {
                    (source as ISource).SourcePropertyChangedEvent -= Source_SourcePropertyChangedEvent;
                }
            }
        }

        private void CalculateTime()
        {
            if (_activeSource != null)
            {
                TimeSpan position = _activeSource.Position + _elapsedTotal;
                TimeSpan length = _activeSource.Length + _remainingTotal;
                bool isOutputSource = _activeSource is OutputSource;

                Dispatcher.BeginInvoke((Action)delegate
                {
                    if (ClockType == EClockType.TimeElapsed)
                    {
                        timeText.Text = position.ToString(TimeFormat);
                    }
                    else if (ClockType == EClockType.TimeRemaining && isOutputSource)
                    {
                        timeText.Text = (length - position).ToString(TimeFormat);
                    }
                });
            }
            else
            {
                Dispatcher.BeginInvoke((Action)delegate
                {
                    timeText.Text = TimeSpan.Zero.ToString(TimeFormat);
                });
            }
        }

        private void Source_SourcePropertyChangedEvent(object sender, SourcePropertyChangedEventArgs e)
        {
            if (timeText != null)
            {
                if (e.Property == ESourceProperty.PlaybackState)
                {
                    _activeSource = sender as ISource;
                    /*if ((CSCore.SoundOut.PlaybackState)e.Value == CSCore.SoundOut.PlaybackState.Playing)
                    {
                        _activeSource = sender as ISource;
                    }
                    else if ((CSCore.SoundOut.PlaybackState)e.Value == CSCore.SoundOut.PlaybackState.Paused)
                    {
                        _activeSource = sender as ISource;
                    }
                    else if ((CSCore.SoundOut.PlaybackState)e.Value == CSCore.SoundOut.PlaybackState.Stopped)
                    {
                        _activeSource = null;
                    }*/

                    // Calculate the time elapsed and remaining time of the queue.
                    var index = SourceCollection.IndexOf(_activeSource);

                    _elapsedTotal = TimeSpan.Zero;
                    _remainingTotal = TimeSpan.Zero;

                    if (index == -1)
                    {
                        // There is no active source, so the elapsed time will be 0 and the remaining
                        // will be the length of the entire queue.
                        _remainingTotal = TimeSpan.FromTicks(SourceCollection.Sum(x => x.Length.Ticks));
                    }
                    else
                    {
                        // There is an active source, so we need to sum all of the sources before
                        // and after the active source to get the elapsed and remaining times.
                        for (var i = 0; i < SourceCollection.Count; i++)
                        {
                            var source = SourceCollection[i];

                            if (i < index)
                            {
                                // add to elapsed time.
                                _elapsedTotal += source.Length;
                            }
                            else if (i > index)
                            {
                                // add to remaining time.
                                _remainingTotal += source.Length;
                            }
                        }
                    }
                }

                if (e.Property == ESourceProperty.Position)
                {
                    CalculateTime();
                }
            }
        }
    }
}
