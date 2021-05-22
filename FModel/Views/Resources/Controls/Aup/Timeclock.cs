using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FModel.Views.Resources.Controls.Aup
{
    [TemplatePart(Name = "PART_Timeclock", Type = typeof(Grid))]
    [TemplatePart(Name = "PART_Time", Type = typeof(TextBlock))]
    public sealed class Timeclock : UserControl
    {
        public enum EClockType
        {
            TimeElapsed,
            TimeRemaining
        }

        private Grid _timeclockGrid;
        private TextBlock _timeText;
        private const string DefaultTimeFormat = "hh\\:mm\\:ss\\.ff";

        static Timeclock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Timeclock), new FrameworkPropertyMetadata(typeof(Timeclock)));
        }

        private ISource _source;
        public ISource Source
        {
            get => (ISource) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ISource), typeof(Timeclock),
                new UIPropertyMetadata(null, OnSourceChanged, OnCoerceSource));

        public EClockType ClockType
        {
            get => (EClockType) GetValue(ClockTypeProperty);
            set => SetValue(ClockTypeProperty, value);
        }
        public static readonly DependencyProperty ClockTypeProperty =
            DependencyProperty.Register("ClockType", typeof(EClockType), typeof(Timeclock),
                new UIPropertyMetadata(EClockType.TimeElapsed, OnClockTypeChanged, OnCoerceClockType));

        public FontFamily LabelFont
        {
            get => (FontFamily) GetValue(LabelFontProperty);
            set => SetValue(LabelFontProperty, value);
        }
        public static readonly DependencyProperty LabelFontProperty =
            DependencyProperty.Register("LabelFont", typeof(FontFamily), typeof(Timeclock),
                new UIPropertyMetadata(new FontFamily("Segoe UI"), OnLabelFontChanged, OnCoerceLabelFont));

        public Brush LabelForeground
        {
            get => (Brush) GetValue(LabelForegroundProperty);
            set => SetValue(LabelForegroundProperty, value);
        }
        public static readonly DependencyProperty LabelForegroundProperty =
            DependencyProperty.Register("LabelForeground", typeof(Brush), typeof(Timeclock),
                new UIPropertyMetadata(Brushes.Coral, OnLabelForegroundChanged, OnCoerceLabelForeground));

        public FontFamily TimeFont
        {
            get => (FontFamily) GetValue(TimeFontProperty);
            set => SetValue(TimeFontProperty, value);
        }
        public static readonly DependencyProperty TimeFontProperty =
            DependencyProperty.Register("TimeFont", typeof(FontFamily), typeof(Timeclock),
                new UIPropertyMetadata(new FontFamily("Ebrima"), OnTimeFontChanged, OnCoerceTimeFont));

        public Brush TimeForeground
        {
            get => (Brush) GetValue(TimeForegroundProperty);
            set => SetValue(TimeForegroundProperty, value);
        }
        public static readonly DependencyProperty TimeForegroundProperty =
            DependencyProperty.Register("TimeForeground", typeof(Brush), typeof(Timeclock),
                new UIPropertyMetadata(Brushes.Silver, OnTimeForegroundChanged, OnCoerceTimeForeground));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius) GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(Timeclock),
                new UIPropertyMetadata(new CornerRadius(3), OnCornerRadiusChanged, OnCoerceCornerRadius));

        public string Label
        {
            get => (string) GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(Timeclock),
                new UIPropertyMetadata(string.Empty, OnLabelChanged, OnCoerceLabel));

        public string TimeFormat
        {
            get => (string) GetValue(TimeFormatProperty);
            set => SetValue(TimeFormatProperty, value);
        }
        public static readonly DependencyProperty TimeFormatProperty =
            DependencyProperty.Register("TimeFormat", typeof(string), typeof(Timeclock),
                new UIPropertyMetadata(DefaultTimeFormat, OnTimeFormatChanged, OnCoerceTimeFormat));

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
            Label = Source.PlayedFile.FileName;
            Dispatcher.BeginInvoke((Action) CalculateTime);
        }

        private void OnSourcePropertyChangedEvent(object sender, SourcePropertyChangedEventArgs e)
        {
            if (_timeText == null || e.Property != ESourceProperty.Position) return;

            CalculateTime();
        }

        private ISource OnCoerceSource(ISource value) => value;
        private static object OnCoerceSource(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceSource((ISource) value);
            return value;
        }

        private static void OnSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnSourceChanged((ISource) e.OldValue, (ISource) e.NewValue);
        }

        private EClockType OnCoerceClockType(EClockType value) => value;
        private static object OnCoerceClockType(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceClockType((EClockType) value);
            return value;
        }

        private void OnClockTypeChanged(EClockType oldValue, EClockType newValue) => CalculateTime();
        private static void OnClockTypeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnClockTypeChanged((EClockType) e.OldValue, (EClockType) e.NewValue);
        }

        private FontFamily OnCoerceLabelFont(FontFamily value) => value;
        private static object OnCoerceLabelFont(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceLabelFont((FontFamily) value);
            return value;
        }

        private void OnLabelFontChanged(FontFamily oldValue, FontFamily newValue)
        {
        }

        private static void OnLabelFontChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnLabelFontChanged((FontFamily) e.OldValue, (FontFamily) e.NewValue);
        }

        private Brush OnCoerceLabelForeground(Brush value) => value;
        private static object OnCoerceLabelForeground(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceLabelForeground((Brush) value);
            return value;
        }

        private void OnLabelForegroundChanged(Brush oldValue, Brush newValue)
        {
        }

        private static void OnLabelForegroundChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnLabelForegroundChanged((Brush) e.OldValue, (Brush) e.NewValue);
        }

        private FontFamily OnCoerceTimeFont(FontFamily value) => value;
        private static object OnCoerceTimeFont(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceTimeFont((FontFamily) value);
            return value;
        }

        private void OnTimeFontChanged(FontFamily oldValue, FontFamily newValue)
        {
        }

        private static void OnTimeFontChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnTimeFontChanged((FontFamily) e.OldValue, (FontFamily) e.NewValue);
        }

        private Brush OnCoerceTimeForeground(Brush value) => value;
        private static object OnCoerceTimeForeground(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceTimeForeground((Brush) value);
            return value;
        }

        private void OnTimeForegroundChanged(Brush oldValue, Brush newValue)
        {
        }

        private static void OnTimeForegroundChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnTimeForegroundChanged((Brush) e.OldValue, (Brush) e.NewValue);
        }

        private CornerRadius OnCoerceCornerRadius(CornerRadius value) => value;
        private static object OnCoerceCornerRadius(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceCornerRadius((CornerRadius) value);
            return value;
        }

        private void OnCornerRadiusChanged(CornerRadius oldValue, CornerRadius newValue)
        {
        }

        private static void OnCornerRadiusChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnCornerRadiusChanged((CornerRadius) e.OldValue, (CornerRadius) e.NewValue);
        }

        private string OnCoerceLabel(string value) => value;
        private static object OnCoerceLabel(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceLabel((string) value);
            return value;
        }

        private void OnLabelChanged(string oldValue, string newValue)
        {
        }

        private static void OnLabelChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnLabelChanged((string) e.OldValue, (string) e.NewValue);
        }

        private string OnCoerceTimeFormat(string value) => value;
        private static object OnCoerceTimeFormat(DependencyObject o, object value)
        {
            if (o is Timeclock timeclock)
                return timeclock.OnCoerceTimeFormat((string) value);
            return value;
        }

        private void OnTimeFormatChanged(string oldValue, string newValue)
        {
            try
            {
                TimeSpan.Zero.ToString(newValue);
            }
            catch
            {
                TimeFormat = DefaultTimeFormat;
            }

            CalculateTime();
        }
        private static void OnTimeFormatChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is Timeclock timeclock)
                timeclock.OnTimeFormatChanged((string) e.OldValue, (string) e.NewValue);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _timeclockGrid = GetTemplateChild("PART_Timeclock") as Grid;
            _timeText = GetTemplateChild("PART_Time") as TextBlock;
            _timeclockGrid.CacheMode = new BitmapCache();

            OnSourceEvent(this, null);
        }

        private void CalculateTime()
        {
            if (_source != null)
            {
                var position = _source.PlayedFile.Position;
                var length = _source.PlayedFile.Duration;
                Dispatcher.BeginInvoke((Action) delegate
                {
                    _timeText.Text = ClockType switch
                    {
                        EClockType.TimeElapsed => position.ToString(TimeFormat),
                        EClockType.TimeRemaining => (length - position).ToString(TimeFormat),
                        _ => _timeText.Text
                    };
                });
            }
            else
            {
                Dispatcher.BeginInvoke((Action) delegate { _timeText.Text = TimeSpan.Zero.ToString(TimeFormat); });
            }
        }
    }
}