using CSCore;
using CSCore.SoundIn;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FModel.Windows.SoundPlayer.Visualization
{
    [TemplatePart(Name = "PART_Spectrum", Type = typeof(Grid))]
    public class SpectrumAnalyzer : Control
    {
        private ISource _source;
        internal ISpectrumProvider _spectrumProvider;

        // visual elements
        private Grid spectrumGrid;
        private Border[] bars;

        private const int ScaleFactorLinear = 9;
        protected const int ScaleFactorSqr = 2;
        protected const double MinDbValue = -90;
        protected const double MaxDbValue = 0;
        protected const double DbScale = (MaxDbValue - MinDbValue);
        private int[] _spectrumIndexMax = new int[100];
        private readonly int _maxFftIndex = 4096 / 2 - 1;
        private readonly int _maximumFrequency = 20000;
        public int _maximumFrequencyIndex;
        private readonly int _minimumFrequency = 20;
        public int _minimumFrequencyIndex;
        private readonly int _spectrumResolution = 100;

        #region Dependency Properties

        #region Source Property

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(ISource),
            typeof(SpectrumAnalyzer), new UIPropertyMetadata(null, OnSourceChanged, OnCoerceSource));

        private static object OnCoerceSource(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceSource((ISource)value);
            else
                return value;
        }

        private static void OnSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnSourceChanged((ISource)e.OldValue, (ISource)e.NewValue);
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
        }

        private void Source_SourceEvent(object sender, SourceEventArgs e)
        {
            if (e.Event == ESourceEventType.Loaded)
            {
                _spectrumProvider = Source.SpectrumProvider;
                UpdateFrequencyMapping();
            }
        }

        private void Source_SourcePropertyChangedEvent(object sender, SourcePropertyChangedEventArgs e)
        {
            if (e.Property == ESourceProperty.FftData)
            {
                UpdateSpectrum(_spectrumResolution, _source.FftData);
            }
            else if (e.Property == ESourceProperty.PlaybackState && (PlaybackState)e.Value == PlaybackState.Playing)
            {
                CreateBars();
            }
            else if (e.Property == ESourceProperty.RecordingState && (RecordingState)e.Value == RecordingState.Recording)
            {
                CreateBars();
            }
            else if (e.Property == ESourceProperty.PlaybackState && (PlaybackState)e.Value != PlaybackState.Playing)
            {
                SilenceBars();
            }
            else if (e.Property == ESourceProperty.RecordingState && (RecordingState)e.Value == RecordingState.Stopped)
            {
                SilenceBars();
            }
        }

        /// <summary>
        /// Gets or sets a Source for the SpectrumAnalyzer.
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

        #region ScalingStrategy Property

        public static readonly DependencyProperty ScalingStrategyProperty = DependencyProperty.Register("ScalingStrategy", typeof(ScalingStrategy),
            typeof(SpectrumAnalyzer), new UIPropertyMetadata(ScalingStrategy.Decibel, OnScalingStrategyChanged, OnCoerceScalingStrategy));

        private static object OnCoerceScalingStrategy(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceScalingStrategy((ScalingStrategy)value);
            else
                return value;
        }

        private static void OnScalingStrategyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnScalingStrategyChanged((ScalingStrategy)e.OldValue, (ScalingStrategy)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="ScalingStrategy"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="ScalingStrategy"/></param>
        /// <returns>The adjusted value of <see cref="ScalingStrategy"/></returns>
        protected virtual ScalingStrategy OnCoerceScalingStrategy(ScalingStrategy value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="ScalingStrategy"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="ScalingStrategy"/></param>
        /// <param name="newValue">The new value of <see cref="ScalingStrategy"/></param>
        protected virtual void OnScalingStrategyChanged(ScalingStrategy oldValue, ScalingStrategy newValue)
        {

        }

        /// <summary>
        /// Gets or sets a ScalingStrategy for the SpectrumAnalyzer.
        /// </summary>        
        public ScalingStrategy SpectrumScalingStrategy
        {
            get
            {
                return (ScalingStrategy)GetValue(ScalingStrategyProperty);
            }
            set
            {
                SetValue(ScalingStrategyProperty, value);
            }
        }

        #endregion

        #region FrequencyBarCount Property

        public static readonly DependencyProperty FrequencyBarCountProperty = DependencyProperty.Register("FrequencyBarCount", typeof(int),
            typeof(SpectrumAnalyzer), new UIPropertyMetadata(25, OnFrequencyBarCountChanged, OnCoerceFrequencyBarCount));

        private static object OnCoerceFrequencyBarCount(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarCount((int)value);
            else
                return value;
        }

        private static void OnFrequencyBarCountChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarCountChanged((int)e.OldValue, (int)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="FrequencyBarCount"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="FrequencyBarCount"/></param>
        /// <returns>The adjusted value of <see cref="FrequencyBarCount"/></returns>
        protected virtual int OnCoerceFrequencyBarCount(int value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="FrequencyBarCount"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="FrequencyBarCount"/></param>
        /// <param name="newValue">The new value of <see cref="FrequencyBarCount"/></param>
        protected virtual void OnFrequencyBarCountChanged(int oldValue, int newValue)
        {
            if (newValue < 1)
            {
                FrequencyBarCount = 1;
            }
            else if (newValue > 100)
            {
                FrequencyBarCount = 100;
            }

            CreateBars();
        }

        /// <summary>
        /// Gets or sets a FrequencyBarCount for the SpectrumAnalyzer.
        /// </summary>        
        public int FrequencyBarCount
        {
            get
            {
                return (int)GetValue(FrequencyBarCountProperty);
            }
            set
            {
                SetValue(FrequencyBarCountProperty, value);
            }
        }

        #endregion

        #region FrequencyBarSpacing Property

        public static readonly DependencyProperty FrequencyBarSpacingProperty = DependencyProperty.Register("FrequencyBarSpacing", typeof(int),
            typeof(SpectrumAnalyzer), new UIPropertyMetadata(2, OnFrequencyBarSpacingChanged, OnCoerceFrequencyBarSpacing));

        private static object OnCoerceFrequencyBarSpacing(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarSpacing((int)value);
            else
                return value;
        }

        private static void OnFrequencyBarSpacingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarSpacingChanged((int)e.OldValue, (int)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="FrequencyBarSpacing"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="FrequencyBarSpacing"/></param>
        /// <returns>The adjusted value of <see cref="FrequencyBarSpacing"/></returns>
        protected virtual int OnCoerceFrequencyBarSpacing(int value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="FrequencyBarSpacing"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="FrequencyBarSpacing"/></param>
        /// <param name="newValue">The new value of <see cref="FrequencyBarSpacing"/></param>
        protected virtual void OnFrequencyBarSpacingChanged(int oldValue, int newValue)
        {

        }

        /// <summary>
        /// Gets or sets a FrequencyBarSpacing for the SpectrumAnalyzer.
        /// </summary>        
        public int FrequencyBarSpacing
        {
            get
            {
                return (int)GetValue(FrequencyBarSpacingProperty);
            }
            set
            {
                SetValue(FrequencyBarSpacingProperty, value);
            }
        }

        #endregion

        #region FrequencyBarBrush Property

        public static readonly DependencyProperty FrequencyBarBrushProperty = DependencyProperty.Register("FrequencyBarBrush", typeof(Brush),
            typeof(SpectrumAnalyzer), new UIPropertyMetadata(Brushes.LightGreen, OnFrequencyBarBrushChanged, OnCoerceFrequencyBarBrush));

        private static object OnCoerceFrequencyBarBrush(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarBrush((Brush)value);
            else
                return value;
        }

        private static void OnFrequencyBarBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarBrushChanged((Brush)e.OldValue, (Brush)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="FrequencyBarBrush"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="FrequencyBarBrush"/></param>
        /// <returns>The adjusted value of <see cref="FrequencyBarBrush"/></returns>
        protected virtual Brush OnCoerceFrequencyBarBrush(Brush value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="FrequencyBarBrush"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="FrequencyBarBrush"/></param>
        /// <param name="newValue">The new value of <see cref="FrequencyBarBrush"/></param>
        protected virtual void OnFrequencyBarBrushChanged(Brush oldValue, Brush newValue)
        {

        }

        /// <summary>
        /// Gets or sets a FrequencyBarBrush for the SpectrumAnalyzer.
        /// </summary>        
        public Brush FrequencyBarBrush
        {
            get
            {
                return (Brush)GetValue(FrequencyBarBrushProperty);
            }
            set
            {
                SetValue(FrequencyBarBrushProperty, value);
            }
        }

        #endregion

        #region FrequencyBarBorderBrush Property

        public static readonly DependencyProperty FrequencyBarBorderBrushProperty = DependencyProperty.Register("FrequencyBarBorderBrush", typeof(Brush),
            typeof(SpectrumAnalyzer), new UIPropertyMetadata(Brushes.Transparent, OnFrequencyBarBorderBrushChanged, OnCoerceFrequencyBarBorderBrush));

        private static object OnCoerceFrequencyBarBorderBrush(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarBorderBrush((Brush)value);
            else
                return value;
        }

        private static void OnFrequencyBarBorderBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarBorderBrushChanged((Brush)e.OldValue, (Brush)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="FrequencyBarBorderBrush"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="FrequencyBarBorderBrush"/></param>
        /// <returns>The adjusted value of <see cref="FrequencyBarBorderBrush"/></returns>
        protected virtual Brush OnCoerceFrequencyBarBorderBrush(Brush value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="FrequencyBarBorderBrush"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="FrequencyBarBorderBrush"/></param>
        /// <param name="newValue">The new value of <see cref="FrequencyBarBorderBrush"/></param>
        protected virtual void OnFrequencyBarBorderBrushChanged(Brush oldValue, Brush newValue)
        {

        }

        /// <summary>
        /// Gets or sets a FrequencyBarBorderBrush for the SpectrumAnalyzer.
        /// </summary>        
        public Brush FrequencyBarBorderBrush
        {
            get
            {
                return (Brush)GetValue(FrequencyBarBorderBrushProperty);
            }
            set
            {
                SetValue(FrequencyBarBorderBrushProperty, value);
            }
        }

        #endregion

        #region FrequencyBarCornerRadius Property

        public static readonly DependencyProperty FrequencyBarCornerRadiusProperty = DependencyProperty.Register("FrequencyBarCornerRadius", typeof(CornerRadius),
            typeof(SpectrumAnalyzer), new UIPropertyMetadata(new CornerRadius(1, 1, 0, 0), OnFrequencyBarCornerRadiusChanged, OnCoerceFrequencyBarCornerRadius));

        private static object OnCoerceFrequencyBarCornerRadius(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarCornerRadius((CornerRadius)value);
            else
                return value;
        }

        private static void OnFrequencyBarCornerRadiusChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarCornerRadiusChanged((CornerRadius)e.OldValue, (CornerRadius)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="FrequencyBarCornerRadius"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="FrequencyBarCornerRadius"/></param>
        /// <returns>The adjusted value of <see cref="FrequencyBarCornerRadius"/></returns>
        protected virtual CornerRadius OnCoerceFrequencyBarCornerRadius(CornerRadius value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="FrequencyBarCornerRadius"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="FrequencyBarCornerRadius"/></param>
        /// <param name="newValue">The new value of <see cref="FrequencyBarCornerRadius"/></param>
        protected virtual void OnFrequencyBarCornerRadiusChanged(CornerRadius oldValue, CornerRadius newValue)
        {

        }

        /// <summary>
        /// Gets or sets a FrequencyBarCornerRadius for the SpectrumAnalyzer.
        /// </summary>        
        public CornerRadius FrequencyBarCornerRadius
        {
            get
            {
                return (CornerRadius)GetValue(FrequencyBarCornerRadiusProperty);
            }
            set
            {
                SetValue(FrequencyBarCornerRadiusProperty, value);
            }
        }

        #endregion

        #region FrequencyBarBorderThickness Property

        public static readonly DependencyProperty FrequencyBarBorderThicknessProperty = DependencyProperty.Register("FrequencyBarBorderThickness", typeof(Thickness),
            typeof(SpectrumAnalyzer), new UIPropertyMetadata(new Thickness(0), OnFrequencyBarBorderThicknessChanged, OnCoerceFrequencyBarBorderThickness));

        private static object OnCoerceFrequencyBarBorderThickness(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarBorderThickness((Thickness)value);
            else
                return value;
        }

        private static void OnFrequencyBarBorderThicknessChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarBorderThicknessChanged((Thickness)e.OldValue, (Thickness)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="FrequencyBarBorderThickness"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="FrequencyBarBorderThickness"/></param>
        /// <returns>The adjusted value of <see cref="FrequencyBarBorderThickness"/></returns>
        protected virtual Thickness OnCoerceFrequencyBarBorderThickness(Thickness value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="FrequencyBarBorderThickness"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="FrequencyBarBorderThickness"/></param>
        /// <param name="newValue">The new value of <see cref="FrequencyBarBorderThickness"/></param>
        protected virtual void OnFrequencyBarBorderThicknessChanged(Thickness oldValue, Thickness newValue)
        {

        }

        /// <summary>
        /// Gets or sets a FrequencyBarBorderThickness for the SpectrumAnalyzer.
        /// </summary>        
        public Thickness FrequencyBarBorderThickness
        {
            get
            {
                return (Thickness)GetValue(FrequencyBarBorderThicknessProperty);
            }
            set
            {
                SetValue(FrequencyBarBorderThicknessProperty, value);
            }
        }

        #endregion

        #endregion

        static SpectrumAnalyzer()
        {
            SpectrumAnalyzer.DefaultStyleKeyProperty
                .OverrideMetadata(typeof(SpectrumAnalyzer), new FrameworkPropertyMetadata(typeof(SpectrumAnalyzer)));

            App.SetFramerate();
        }

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            spectrumGrid = GetTemplateChild("PART_Spectrum") as Grid;
            spectrumGrid.CacheMode = new BitmapCache();

            CreateBars();
            UpdateFrequencyMapping();
        }

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            UpdateFrequencyMapping();
        }

        #endregion

        private void CreateBars()
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                if (spectrumGrid != null)
                {
                    // Let's try to make this use less CPU by reusing the spectrum bars!
                    bars = new Border[FrequencyBarCount];

                    for (var i = 0; i < bars.Length; i++)
                    {
                        bars[i] = new Border();
                    }

                    spectrumGrid.Children.Clear();

                    for (var i = 0; i < bars.Length; i++)
                    {
                        spectrumGrid.Children.Add(bars[i]);
                    }
                }
            }, DispatcherPriority.Render);
        }

        private void SilenceBars()
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                if (spectrumGrid != null)
                {
                    spectrumGrid.Children.Clear();
                    spectrumGrid.CacheMode = new BitmapCache();
                }

            }, DispatcherPriority.Render);
        }

        private void UpdateFrequencyMapping()
        {
            if (_spectrumProvider == null)
                return;

            _maximumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(_maximumFrequency) + 1, _maxFftIndex);
            _minimumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(_minimumFrequency), _maxFftIndex);

            int actualResolution = _spectrumResolution;

            int indexCount = _maximumFrequencyIndex - _minimumFrequencyIndex;
            double linearIndexBucketSize = Math.Round(indexCount / (double)actualResolution, 3);

            _spectrumIndexMax = _spectrumIndexMax.CheckBuffer(actualResolution, true);

            double maxLog = Math.Log(actualResolution, actualResolution);
            for (int i = 1; i < actualResolution; i++)
            {
                _ =
                    (int)((maxLog - Math.Log((actualResolution + 1) - i, (actualResolution + 1))) * indexCount) +
                    _minimumFrequencyIndex;

                _spectrumIndexMax[i - 1] = _minimumFrequencyIndex + (int)(i * linearIndexBucketSize);
            }

            if (actualResolution > 0)
            {
                _spectrumIndexMax[^1] = _maximumFrequencyIndex;
            }
        }

        private void UpdateSpectrum(double maxValue, float[] fftBuffer)
        {
            ScalingStrategy spectrumScalingStrategy = ScalingStrategy.Decibel;
            Dispatcher.Invoke((Action)delegate { spectrumScalingStrategy = SpectrumScalingStrategy; });

            var dataPoints = new List<double>();
            double value0 = 0, value = 0;
            double lastValue = 0;
            double actualMaxValue = maxValue;
            int spectrumPointIndex = 0;

            for (int i = _minimumFrequencyIndex; i <= _maximumFrequencyIndex; i++)
            {
                switch (spectrumScalingStrategy)
                {
                    case ScalingStrategy.Decibel:
                        value0 = (((20 * Math.Log10(fftBuffer[i])) - MinDbValue) / DbScale) * actualMaxValue;
                        break;
                    case ScalingStrategy.Linear:
                        value0 = (fftBuffer[i] * ScaleFactorLinear) * actualMaxValue;
                        break;
                    case ScalingStrategy.Sqrt:
                        value0 = ((Math.Sqrt(fftBuffer[i])) * ScaleFactorSqr) * actualMaxValue;
                        break;
                }

                bool recalc = true;

                value = Math.Max(0, Math.Max(value0, value));

                while (spectrumPointIndex <= _spectrumIndexMax.Length - 1 &&
                        i == _spectrumIndexMax[spectrumPointIndex])
                {
                    if (!recalc)
                        value = lastValue;

                    if (value > maxValue)
                        value = maxValue;

                    /*TODO: Add Average property to the spectrum analyzer*/
                    if (/*_useAverage*/true && spectrumPointIndex > 0)
                        value = (lastValue + value) / 2.0;

                    dataPoints.Add(value);

                    lastValue = value;
                    value = 0.0;
                    spectrumPointIndex++;
                    recalc = false;
                }
            }

            Dispatcher.Invoke((Action)delegate
            {
                var width = this.RenderSize.Width;
                var height = this.RenderSize.Height;
                var barSpacing = width / FrequencyBarCount;
                var barWidth = barSpacing - FrequencyBarSpacing;

                // freeze brushes
                var borderBrush = FrequencyBarBorderBrush.Clone();
                var barBrush = FrequencyBarBrush.Clone();
                borderBrush.Freeze();
                barBrush.Freeze();

                if (barWidth < .5)
                    barWidth = .5;

                for (var i = 0; i < FrequencyBarCount; i++)
                {
                    //var b = new Border();
                    var b = bars[i];
                    b.Width = barWidth;
                    b.BorderBrush = borderBrush;
                    b.CornerRadius = FrequencyBarCornerRadius;
                    b.BorderThickness = FrequencyBarBorderThickness;
                    b.Height = (dataPoints[i] / 100) * height;
                    b.HorizontalAlignment = HorizontalAlignment.Left;
                    b.VerticalAlignment = VerticalAlignment.Bottom;
                    b.Margin = new Thickness(i * barSpacing, 0, 0, 0);
                    b.Background = barBrush;
                }
            }, DispatcherPriority.ApplicationIdle);
        }

        public enum ScalingStrategy
        {
            Decibel,
            Linear,
            Sqrt
        }
    }
}
