using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CSCore;
using CSCore.SoundIn;
using CSCore.SoundOut;

namespace FModel.Views.Resources.Controls.Aup
{
    [TemplatePart(Name = "PART_Spectrum", Type = typeof(Grid))]
    public sealed class SpectrumAnalyzer : UserControl
    {
        public enum ScalingStrategy
        {
            Decibel,
            Linear,
            Sqrt
        }

        private Grid _spectrumGrid;
        private Border[] _bars;

        static SpectrumAnalyzer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SpectrumAnalyzer), new FrameworkPropertyMetadata(typeof(SpectrumAnalyzer)));
        }

        private ISource _source;
        public ISource Source
        {
            get => (ISource) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ISource), typeof(SpectrumAnalyzer),
                new UIPropertyMetadata(null, OnSourceChanged, OnCoerceSource));

        public ScalingStrategy SpectrumScalingStrategy
        {
            get => (ScalingStrategy) GetValue(ScalingStrategyProperty);
            set => SetValue(ScalingStrategyProperty, value);
        }
        public static readonly DependencyProperty ScalingStrategyProperty =
            DependencyProperty.Register("ScalingStrategy", typeof(ScalingStrategy), typeof(SpectrumAnalyzer),
                new UIPropertyMetadata(ScalingStrategy.Linear, OnScalingStrategyChanged, OnCoerceScalingStrategy));

        public int FrequencyBarCount
        {
            get => (int) GetValue(FrequencyBarCountProperty);
            set => SetValue(FrequencyBarCountProperty, value);
        }
        public static readonly DependencyProperty FrequencyBarCountProperty =
            DependencyProperty.Register("FrequencyBarCount", typeof(int), typeof(SpectrumAnalyzer),
                new UIPropertyMetadata(25, OnFrequencyBarCountChanged, OnCoerceFrequencyBarCount));

        public int FrequencyBarSpacing
        {
            get => (int) GetValue(FrequencyBarSpacingProperty);
            set => SetValue(FrequencyBarSpacingProperty, value);
        }
        public static readonly DependencyProperty FrequencyBarSpacingProperty =
            DependencyProperty.Register("FrequencyBarSpacing", typeof(int), typeof(SpectrumAnalyzer),
                new UIPropertyMetadata(2, OnFrequencyBarSpacingChanged, OnCoerceFrequencyBarSpacing));

        public Brush FrequencyBarBrush
        {
            get => (Brush) GetValue(FrequencyBarBrushProperty);
            set => SetValue(FrequencyBarBrushProperty, value);
        }
        public static readonly DependencyProperty FrequencyBarBrushProperty =
            DependencyProperty.Register("FrequencyBarBrush", typeof(Brush), typeof(SpectrumAnalyzer),
                new UIPropertyMetadata(Brushes.LightGreen, OnFrequencyBarBrushChanged, OnCoerceFrequencyBarBrush));

        public Brush FrequencyBarBorderBrush
        {
            get => (Brush) GetValue(FrequencyBarBorderBrushProperty);
            set => SetValue(FrequencyBarBorderBrushProperty, value);
        }
        public static readonly DependencyProperty FrequencyBarBorderBrushProperty =
            DependencyProperty.Register("FrequencyBarBorderBrush", typeof(Brush), typeof(SpectrumAnalyzer),
                new UIPropertyMetadata(Brushes.Transparent, OnFrequencyBarBorderBrushChanged, OnCoerceFrequencyBarBorderBrush));

        public CornerRadius FrequencyBarCornerRadius
        {
            get => (CornerRadius) GetValue(FrequencyBarCornerRadiusProperty);
            set => SetValue(FrequencyBarCornerRadiusProperty, value);
        }
        public static readonly DependencyProperty FrequencyBarCornerRadiusProperty =
            DependencyProperty.Register("FrequencyBarCornerRadius", typeof(CornerRadius), typeof(SpectrumAnalyzer),
                new UIPropertyMetadata(new CornerRadius(1, 1, 0, 0), OnFrequencyBarCornerRadiusChanged, OnCoerceFrequencyBarCornerRadius));

        public Thickness FrequencyBarBorderThickness
        {
            get => (Thickness) GetValue(FrequencyBarBorderThicknessProperty);
            set => SetValue(FrequencyBarBorderThicknessProperty, value);
        }
        public static readonly DependencyProperty FrequencyBarBorderThicknessProperty =
            DependencyProperty.Register("FrequencyBarBorderThickness", typeof(Thickness), typeof(SpectrumAnalyzer),
                new UIPropertyMetadata(new Thickness(0), OnFrequencyBarBorderThicknessChanged, OnCoerceFrequencyBarBorderThickness));

        private void OnSourceChanged(ISource oldValue, ISource newValue)
        {
            _source = Source;
            _source.SourceEvent += OnSourceEvent;
            _source.SourcePropertyChangedEvent += OnSourcePropertyChangedEvent;
        }

        private SpectrumProvider _spectrumProvider;
        private void OnSourceEvent(object sender, SourceEventArgs e)
        {
            if (e.Event != ESourceEventType.Loading) return;
            _spectrumProvider = Source.Spectrum;
            UpdateFrequencyMapping();
        }

        private void OnSourcePropertyChangedEvent(object sender, SourcePropertyChangedEventArgs e)
        {
            switch (e.Property)
            {
                case ESourceProperty.FftData:
                    UpdateSpectrum(SpectrumResolution, _source.FftData);
                    break;
                case ESourceProperty.PlaybackState when (PlaybackState) e.Value == PlaybackState.Playing:
                case ESourceProperty.RecordingState when (RecordingState) e.Value == RecordingState.Recording:
                    CreateBars();
                    break;
                case ESourceProperty.RecordingState when (RecordingState) e.Value == RecordingState.Stopped:
                    SilenceBars();
                    break;
            }
        }

        private ISource OnCoerceSource(ISource value) => value;
        private static object OnCoerceSource(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceSource((ISource) value);
            return value;
        }

        private static void OnSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnSourceChanged((ISource) e.OldValue, (ISource) e.NewValue);
        }

        private ScalingStrategy OnCoerceScalingStrategy(ScalingStrategy value) => value;
        private static object OnCoerceScalingStrategy(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceScalingStrategy((ScalingStrategy) value);
            return value;
        }

        private void OnScalingStrategyChanged(ScalingStrategy oldValue, ScalingStrategy newValue)
        {
        }

        private static void OnScalingStrategyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnScalingStrategyChanged((ScalingStrategy) e.OldValue, (ScalingStrategy) e.NewValue);
        }

        private int OnCoerceFrequencyBarCount(int value) => value;
        private static object OnCoerceFrequencyBarCount(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarCount((int) value);
            return value;
        }

        private static void OnFrequencyBarCountChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarCountChanged((int) e.OldValue, (int) e.NewValue);
        }

        private void OnFrequencyBarCountChanged(int oldValue, int newValue)
        {
            FrequencyBarCount = newValue switch
            {
                < 1 => 1,
                > 100 => 100,
                _ => FrequencyBarCount
            };

            CreateBars();
        }

        private int OnCoerceFrequencyBarSpacing(int value) => value;
        private static object OnCoerceFrequencyBarSpacing(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarSpacing((int) value);
            return value;
        }

        private void OnFrequencyBarSpacingChanged(int oldValue, int newValue)
        {
        }

        private static void OnFrequencyBarSpacingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarSpacingChanged((int) e.OldValue, (int) e.NewValue);
        }

        private Brush OnCoerceFrequencyBarBrush(Brush value) => value;
        private static object OnCoerceFrequencyBarBrush(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarBrush((Brush) value);
            return value;
        }

        private void OnFrequencyBarBrushChanged(Brush oldValue, Brush newValue)
        {
        }

        private static void OnFrequencyBarBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarBrushChanged((Brush) e.OldValue, (Brush) e.NewValue);
        }

        private Brush OnCoerceFrequencyBarBorderBrush(Brush value) => value;
        private static object OnCoerceFrequencyBarBorderBrush(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarBorderBrush((Brush) value);
            return value;
        }

        private void OnFrequencyBarBorderBrushChanged(Brush oldValue, Brush newValue)
        {
        }

        private static void OnFrequencyBarBorderBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarBorderBrushChanged((Brush) e.OldValue, (Brush) e.NewValue);
        }

        private CornerRadius OnCoerceFrequencyBarCornerRadius(CornerRadius value) => value;
        private static object OnCoerceFrequencyBarCornerRadius(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarCornerRadius((CornerRadius) value);
            return value;
        }

        private void OnFrequencyBarCornerRadiusChanged(CornerRadius oldValue, CornerRadius newValue)
        {
        }

        private static void OnFrequencyBarCornerRadiusChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarCornerRadiusChanged((CornerRadius) e.OldValue, (CornerRadius) e.NewValue);
        }

        private Thickness OnCoerceFrequencyBarBorderThickness(Thickness value) => value;
        private static object OnCoerceFrequencyBarBorderThickness(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer.OnCoerceFrequencyBarBorderThickness((Thickness) value);
            return value;
        }

        private void OnFrequencyBarBorderThicknessChanged(Thickness oldValue, Thickness newValue)
        {
        }

        private static void OnFrequencyBarBorderThicknessChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                spectrumAnalyzer.OnFrequencyBarBorderThicknessChanged((Thickness) e.OldValue, (Thickness) e.NewValue);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _spectrumGrid = GetTemplateChild("PART_Spectrum") as Grid;

            CreateBars();
            UpdateFrequencyMapping();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            UpdateFrequencyMapping();
        }

        private const int ScaleFactorLinear = 50;
        private const int ScaleFactorSqr = 2;
        private const int MaxFftIndex = 4096 / 2 - 1;
        private const int MaximumFrequency = 48000;
        private const int MinimumFrequency = 20;
        private const int SpectrumResolution = 100;
        private const double MinDbValue = -90;
        private const double DbScale = 0 - MinDbValue;
        private int[] _spectrumIndexMax = new int[100];
        private int _maximumFrequencyIndex;
        private int _minimumFrequencyIndex;

        private void CreateBars()
        {
            Dispatcher.BeginInvoke((Action) delegate
            {
                if (_spectrumGrid == null) return;

                _spectrumGrid.Children.Clear();
                _bars = new Border[FrequencyBarCount];
                for (var i = 0; i < _bars.Length; i++)
                {
                    var borderBrush = FrequencyBarBorderBrush.Clone();
                    var barBrush = FrequencyBarBrush.Clone();
                    borderBrush.Freeze();
                    barBrush.Freeze();

                    _bars[i] = new Border
                    {
                        CornerRadius = FrequencyBarCornerRadius,
                        BorderThickness = FrequencyBarBorderThickness,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Height = 0,
                        BorderBrush = borderBrush,
                        Background = barBrush
                    };
                    _spectrumGrid.Children.Add(_bars[i]);
                }
            }, DispatcherPriority.Render);
        }

        private void SilenceBars()
        {
            Dispatcher.BeginInvoke((Action) delegate
            {
                if (_spectrumGrid == null) return;

                _spectrumGrid.Children.Clear();
                _spectrumGrid.CacheMode = new BitmapCache();
            }, DispatcherPriority.Render);
        }

        private void UpdateFrequencyMapping()
        {
            if (_spectrumProvider == null) return;

            _maximumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MaximumFrequency) + 1, MaxFftIndex);
            _minimumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MinimumFrequency), MaxFftIndex);
            _spectrumIndexMax = _spectrumIndexMax.CheckBuffer(SpectrumResolution, true);

            var indexCount = _maximumFrequencyIndex - _minimumFrequencyIndex;
            var linearIndexBucketSize = Math.Round(indexCount / (double) SpectrumResolution, 3);
            for (var i = 1; i < SpectrumResolution; i++)
            {
                _spectrumIndexMax[i - 1] = _minimumFrequencyIndex + (int) (i * linearIndexBucketSize);
            }

            _spectrumIndexMax[^1] = _maximumFrequencyIndex;
        }

        private void UpdateSpectrum(double maxValue, IReadOnlyList<float> fftBuffer)
        {
            var spectrumScalingStrategy = ScalingStrategy.Decibel;
            Dispatcher.Invoke(delegate { spectrumScalingStrategy = SpectrumScalingStrategy; });

            var lastValue = 0D;
            var spectrumPointIndex = 0;
            var dataPoints = new List<double>();
            for (var i = _minimumFrequencyIndex; i <= _maximumFrequencyIndex; i++)
            {
                var tempVal = spectrumScalingStrategy switch
                {
                    ScalingStrategy.Decibel => (20 * Math.Log10(fftBuffer[i]) - MinDbValue) / DbScale * maxValue,
                    ScalingStrategy.Linear => fftBuffer[i] * ScaleFactorLinear * maxValue,
                    ScalingStrategy.Sqrt => Math.Sqrt(fftBuffer[i]) * ScaleFactorSqr * maxValue,
                    _ => 0D
                };

                var bAgain = true;
                var value = Math.Max(0, Math.Max(tempVal, 0));
                while (spectrumPointIndex <= _spectrumIndexMax.Length - 1 && i == _spectrumIndexMax[spectrumPointIndex])
                {
                    if (!bAgain)
                        value = lastValue;

                    if (value > maxValue)
                        value = maxValue;

                    if (spectrumPointIndex > 0)
                        value = (lastValue + value) / 1.5;

                    dataPoints.Add(value);
                    lastValue = value;
                    value = 0.0;
                    spectrumPointIndex++;
                    bAgain = false;
                }
            }

            Dispatcher.Invoke(delegate
            {
                for (var i = 0; i < FrequencyBarCount; i++)
                {
                    var barSpacing = RenderSize.Width / FrequencyBarCount;
                    var barWidth = barSpacing - FrequencyBarSpacing;
                    if (barWidth < .5)
                        barWidth = .5;

                    var b = _bars[i];
                    b.Height = dataPoints[i] / 100 * RenderSize.Height;
                    b.Width = barWidth;
                    b.Margin = new Thickness(i * barSpacing, 0, 0, 0);
                }
            }, DispatcherPriority.ApplicationIdle);
        }
    }
}