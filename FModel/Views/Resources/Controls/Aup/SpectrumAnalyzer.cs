using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
// TODO(P3-013): CSCore.SoundIn not available on Linux — RecordingState will be replaced with cross-platform audio backend
// using CSCore.SoundIn;
// TODO(P3-013): CSCore.SoundOut not available on Linux — PlaybackState will be replaced with cross-platform audio backend
// using CSCore.SoundOut;

namespace FModel.Views.Resources.Controls.Aup;

public sealed class SpectrumAnalyzer : UserControl
{
    public enum ScalingStrategy
    {
        Decibel,
        Linear,
        Sqrt
    }

    private readonly Grid _spectrumGrid = new();
    private Border[] _bars = Array.Empty<Border>();

    // -----------------------------------------------------------------------
    // Styled properties
    // -----------------------------------------------------------------------

    public static readonly StyledProperty<ISource?> SourceProperty =
        AvaloniaProperty.Register<SpectrumAnalyzer, ISource?>(nameof(Source));
    public ISource? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly StyledProperty<ScalingStrategy> ScalingStrategyProperty =
        AvaloniaProperty.Register<SpectrumAnalyzer, ScalingStrategy>(nameof(SpectrumScalingStrategy),
            defaultValue: ScalingStrategy.Linear);
    public ScalingStrategy SpectrumScalingStrategy
    {
        get => GetValue(ScalingStrategyProperty);
        set => SetValue(ScalingStrategyProperty, value);
    }

    public static readonly StyledProperty<int> FrequencyBarCountProperty =
        AvaloniaProperty.Register<SpectrumAnalyzer, int>(nameof(FrequencyBarCount), defaultValue: 25);
    public int FrequencyBarCount
    {
        get => GetValue(FrequencyBarCountProperty);
        set => SetValue(FrequencyBarCountProperty, value);
    }

    public static readonly StyledProperty<int> FrequencyBarSpacingProperty =
        AvaloniaProperty.Register<SpectrumAnalyzer, int>(nameof(FrequencyBarSpacing), defaultValue: 2);
    public int FrequencyBarSpacing
    {
        get => GetValue(FrequencyBarSpacingProperty);
        set => SetValue(FrequencyBarSpacingProperty, value);
    }

    public static readonly StyledProperty<IBrush?> FrequencyBarBrushProperty =
        AvaloniaProperty.Register<SpectrumAnalyzer, IBrush?>(nameof(FrequencyBarBrush),
            defaultValue: Brushes.LightGreen);
    public IBrush? FrequencyBarBrush
    {
        get => GetValue(FrequencyBarBrushProperty);
        set => SetValue(FrequencyBarBrushProperty, value);
    }

    public static readonly StyledProperty<IBrush?> FrequencyBarBorderBrushProperty =
        AvaloniaProperty.Register<SpectrumAnalyzer, IBrush?>(nameof(FrequencyBarBorderBrush),
            defaultValue: Brushes.Transparent);
    public IBrush? FrequencyBarBorderBrush
    {
        get => GetValue(FrequencyBarBorderBrushProperty);
        set => SetValue(FrequencyBarBorderBrushProperty, value);
    }

    public static readonly StyledProperty<CornerRadius> FrequencyBarCornerRadiusProperty =
        AvaloniaProperty.Register<SpectrumAnalyzer, CornerRadius>(nameof(FrequencyBarCornerRadius),
            defaultValue: new CornerRadius(1, 1, 0, 0));
    public CornerRadius FrequencyBarCornerRadius
    {
        get => GetValue(FrequencyBarCornerRadiusProperty);
        set => SetValue(FrequencyBarCornerRadiusProperty, value);
    }

    public static readonly StyledProperty<Thickness> FrequencyBarBorderThicknessProperty =
        AvaloniaProperty.Register<SpectrumAnalyzer, Thickness>(nameof(FrequencyBarBorderThickness),
            defaultValue: new Thickness(0));
    public Thickness FrequencyBarBorderThickness
    {
        get => GetValue(FrequencyBarBorderThicknessProperty);
        set => SetValue(FrequencyBarBorderThicknessProperty, value);
    }

    // -----------------------------------------------------------------------
    // Static ctor — wire property-changed callbacks
    // -----------------------------------------------------------------------
    static SpectrumAnalyzer()
    {
        SourceProperty.Changed.AddClassHandler<SpectrumAnalyzer>((m, e) =>
            m.OnSourceChanged(e.GetOldValue<ISource?>(), e.GetNewValue<ISource?>()));

        FrequencyBarCountProperty.Changed.AddClassHandler<SpectrumAnalyzer>((m, e) =>
        {
            var nv = (int) e.NewValue!;
            if (nv < 1)
            { m.FrequencyBarCount = 1; return; }
            if (nv > 100)
            { m.FrequencyBarCount = 100; return; }
            m.CreateBars();
        });

        BoundsProperty.Changed.AddClassHandler<SpectrumAnalyzer>((m, e) =>
        {
            if (e.OldValue is Rect old && e.NewValue is Rect nw && old.Size != nw.Size)
                m.UpdateFrequencyMapping();
        });
        FrequencyBarBorderThicknessProperty.Changed.AddClassHandler<SpectrumAnalyzer>((m, _) => m.CreateBars());
        FrequencyBarBrushProperty.Changed.AddClassHandler<SpectrumAnalyzer>((m, _) => m.CreateBars());
        FrequencyBarBorderBrushProperty.Changed.AddClassHandler<SpectrumAnalyzer>((m, _) => m.CreateBars());
        FrequencyBarCornerRadiusProperty.Changed.AddClassHandler<SpectrumAnalyzer>((m, _) => m.CreateBars());
    }

    public SpectrumAnalyzer()
    {
        Content = _spectrumGrid;
    }

    // -----------------------------------------------------------------------
    // Source wiring
    // -----------------------------------------------------------------------

    private ISource? _source;
    private SpectrumProvider? _spectrumProvider;

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
    }

    private void OnSourceEvent(object? sender, SourceEventArgs e)
    {
        if (e.Event != ESourceEventType.Loading)
            return;
        _spectrumProvider = Source?.Spectrum;
        UpdateFrequencyMapping();
        CreateBars();
    }

    private void OnSourcePropertyChangedEvent(object? sender, SourcePropertyChangedEventArgs e)
    {
        switch (e.Property)
        {
            case ESourceProperty.FftData:
                UpdateSpectrum(SpectrumResolution, _source!.FftData);
                break;
            case ESourceProperty.HideToggle when e.Value is false:
                CreateBars();
                break;
            // TODO(P3-013): PlaybackState / RecordingState come from CSCore which is not available on Linux.
            // Re-enable these case arms when the cross-platform audio backend is integrated (P3-013).
            // case ESourceProperty.PlaybackState when (PlaybackState) e.Value == PlaybackState.Playing:
            //     CreateBars();
            //     break;
            // case ESourceProperty.RecordingState when (RecordingState) e.Value == RecordingState.Recording:
            //     CreateBars();
            //     break;
            case ESourceProperty.HideToggle when e.Value is true:
                SilenceBars();
                break;
                // case ESourceProperty.RecordingState when (RecordingState) e.Value == RecordingState.Stopped:
                //     SilenceBars();
                //     break;
        }
    }

    // -----------------------------------------------------------------------
    // Internal rendering constants and state
    // -----------------------------------------------------------------------

    private const int ScaleFactorLinear = 50;
    private const int ScaleFactorSqr = 2;
    private const int MaxFftIndex = 4096 / 2 - 1;
    private const int MaximumFrequency = 48000;
    private const int MinimumFrequency = 20;
    private const int SpectrumResolution = 100;
    private const double MinDbValue = -90;
    private const double DbScale = 0 - MinDbValue;
    private int[] _spectrumIndexMax = new int[SpectrumResolution];
    private int _maximumFrequencyIndex;
    private int _minimumFrequencyIndex;

    private void CreateBars()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _spectrumGrid.Children.Clear();
            _bars = new Border[FrequencyBarCount];
            for (var i = 0; i < _bars.Length; i++)
            {
                _bars[i] = new Border
                {
                    CornerRadius = FrequencyBarCornerRadius,
                    BorderThickness = FrequencyBarBorderThickness,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Height = 0,
                    BorderBrush = FrequencyBarBorderBrush,
                    Background = FrequencyBarBrush,
                };
                _spectrumGrid.Children.Add(_bars[i]);
            }
        }, DispatcherPriority.Render);
    }

    private void SilenceBars()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _spectrumGrid.Children.Clear();
            _bars = Array.Empty<Border>();
        }, DispatcherPriority.Render);
    }

    private void UpdateFrequencyMapping()
    {
        if (_spectrumProvider == null)
            return;

        _maximumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MaximumFrequency) + 1, MaxFftIndex);
        _minimumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MinimumFrequency), MaxFftIndex);

        // Inline replacement for CSCore's array.CheckBuffer(size, clear):
        if (_spectrumIndexMax.Length != SpectrumResolution)
            _spectrumIndexMax = new int[SpectrumResolution];
        else
            Array.Clear(_spectrumIndexMax, 0, SpectrumResolution);

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
        // Snapshot the array reference — SilenceBars() may replace _bars on the UI thread
        // concurrently; the snapshot keeps the closure and the loop bound in sync.
        var bars = _bars;
        if (bars.Length == 0)
            return;
        // SpectrumScalingStrategy is a value-type StyledProperty — safe to read from any thread.
        var spectrumScalingStrategy = SpectrumScalingStrategy;

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

        Dispatcher.UIThread.Post(() =>
        {
            // Use the snapshotted array as the authoritative bar count so that
            // FrequencyBarCount changes or a concurrent SilenceBars() call cannot
            // cause the loop to index past the end of the array.
            var barCount = Math.Min(bars.Length, dataPoints.Count);
            for (var i = 0; i < barCount; i++)
            {
                var barSpacing = Bounds.Width / bars.Length;
                var barWidth = barSpacing - FrequencyBarSpacing;
                if (barWidth < .5)
                    barWidth = .5;

                var b = bars[i];
                b.Height = dataPoints[i] / 100 * Bounds.Height;
                b.Width = barWidth;
                b.Margin = new Thickness(i * barSpacing, 0, 0, 0);
            }
        }, DispatcherPriority.Background);
    }
}
