using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace FModel.Views.Resources.Controls.Aup;

/// <summary>
/// Displays a running clock (elapsed or remaining) bound to an <see cref="ISource"/>.
/// Avalonia port of the WPF Timeclock UserControl — ControlTemplate replaced by a
/// visual tree built in the constructor.
/// </summary>
public sealed class Timeclock : UserControl
{
    public enum EClockType
    {
        TimeElapsed,
        TimeRemaining
    }

    private readonly TextBlock _labelText;
    private readonly TextBlock _timeText;
    private const string DefaultTimeFormat = "hh\\:mm\\:ss\\.ff";

    // -----------------------------------------------------------------------
    // Styled properties
    // -----------------------------------------------------------------------

    public static readonly StyledProperty<ISource?> SourceProperty =
        AvaloniaProperty.Register<Timeclock, ISource?>(nameof(Source));
    public ISource? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly StyledProperty<EClockType> ClockTypeProperty =
        AvaloniaProperty.Register<Timeclock, EClockType>(nameof(ClockType), defaultValue: EClockType.TimeElapsed);
    public EClockType ClockType
    {
        get => GetValue(ClockTypeProperty);
        set => SetValue(ClockTypeProperty, value);
    }

    public static readonly StyledProperty<FontFamily> LabelFontProperty =
        AvaloniaProperty.Register<Timeclock, FontFamily>(nameof(LabelFont), defaultValue: new FontFamily("Segoe UI"));
    public FontFamily LabelFont
    {
        get => GetValue(LabelFontProperty);
        set => SetValue(LabelFontProperty, value);
    }

    public static readonly StyledProperty<IBrush?> LabelForegroundProperty =
        AvaloniaProperty.Register<Timeclock, IBrush?>(nameof(LabelForeground), defaultValue: Brushes.Coral);
    public IBrush? LabelForeground
    {
        get => GetValue(LabelForegroundProperty);
        set => SetValue(LabelForegroundProperty, value);
    }

    public static readonly StyledProperty<FontFamily> TimeFontProperty =
        AvaloniaProperty.Register<Timeclock, FontFamily>(nameof(TimeFont), defaultValue: new FontFamily("Ebrima"));
    public FontFamily TimeFont
    {
        get => GetValue(TimeFontProperty);
        set => SetValue(TimeFontProperty, value);
    }

    public static readonly StyledProperty<IBrush?> TimeForegroundProperty =
        AvaloniaProperty.Register<Timeclock, IBrush?>(nameof(TimeForeground), defaultValue: Brushes.Silver);
    public IBrush? TimeForeground
    {
        get => GetValue(TimeForegroundProperty);
        set => SetValue(TimeForegroundProperty, value);
    }

    // CornerRadius is inherited from TemplatedControl via UserControl.
    // Default is set in the constructor via SetCurrentValue.

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<Timeclock, string>(nameof(Label), defaultValue: string.Empty);
    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly StyledProperty<string> TimeFormatProperty =
        AvaloniaProperty.Register<Timeclock, string>(nameof(TimeFormat), defaultValue: DefaultTimeFormat);
    public string TimeFormat
    {
        get => GetValue(TimeFormatProperty);
        set => SetValue(TimeFormatProperty, value);
    }

    // -----------------------------------------------------------------------
    // Constructor — build visual tree directly (no ControlTemplate needed)
    // -----------------------------------------------------------------------

    public Timeclock()
    {
        _labelText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _timeText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var border = new Border
        {
            Child = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { _labelText, _timeText }
            }
        };

        // Bind border CornerRadius to the inherited CornerRadius styled property.
        border.Bind(Border.CornerRadiusProperty, this.GetObservable(UserControl.CornerRadiusProperty));
        SetCurrentValue(UserControl.CornerRadiusProperty, new CornerRadius(3));

        // Bind textblock fonts / foregrounds to styled properties.
        _labelText.Bind(TextBlock.FontFamilyProperty, this.GetObservable(LabelFontProperty));
        _labelText.Bind(TextBlock.ForegroundProperty, this.GetObservable(LabelForegroundProperty));
        _labelText.Bind(TextBlock.TextProperty, this.GetObservable(LabelProperty));
        _timeText.Bind(TextBlock.FontFamilyProperty, this.GetObservable(TimeFontProperty));
        _timeText.Bind(TextBlock.ForegroundProperty, this.GetObservable(TimeForegroundProperty));

        Content = border;

        ZeroTime();
    }

    // -----------------------------------------------------------------------
    // Property-change reactions
    // -----------------------------------------------------------------------

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceProperty)
        {
            if (change.OldValue is ISource old)
            {
                old.SourceEvent -= OnSourceEvent;
                old.SourcePropertyChangedEvent -= OnSourcePropertyChangedEvent;
            }
            if (change.NewValue is ISource src)
            {
                src.SourceEvent += OnSourceEvent;
                src.SourcePropertyChangedEvent += OnSourcePropertyChangedEvent;
            }
            // Refresh display immediately.
            CalculateTime();
        }
        else if (change.Property == ClockTypeProperty
              || change.Property == TimeFormatProperty)
        {
            if (change.Property == TimeFormatProperty)
            {
                // Validate the new format string; revert to default on error.
                try
                { TimeSpan.Zero.ToString(TimeFormat); }
                catch { SetCurrentValue(TimeFormatProperty, DefaultTimeFormat); }
            }
            CalculateTime();
        }
    }

    private void OnSourceEvent(object? sender, SourceEventArgs? e)
    {
        if (Source == null)
            return;
        Dispatcher.UIThread.Post(() =>
        {
            SetCurrentValue(LabelProperty, Source.PlayedFile.FileName);
            CalculateTime();
        });
    }

    // NOTE: This handler may be called from a background (audio) thread.
    // CalculateTime() reads Source.PlayedFile.Position/Duration before marshalling
    // to the UI thread. This is safe because ISource properties are immutable value
    // types (TimeSpan) and the audio engine guarantees coherent reads.
    private void OnSourcePropertyChangedEvent(object? sender, SourcePropertyChangedEventArgs e)
    {
        if (e.Property != ESourceProperty.Position)
            return;
        CalculateTime();
    }

    private void CalculateTime()
    {
        if (Source != null)
        {
            var position = Source.PlayedFile.Position;
            var length = Source.PlayedFile.Duration;
            Dispatcher.UIThread.Post(() =>
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
            ZeroTime();
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (Source is { } src)
        {
            src.SourceEvent -= OnSourceEvent;
            src.SourcePropertyChangedEvent -= OnSourcePropertyChangedEvent;
        }
    }

    private void ZeroTime()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _timeText.Text = TimeSpan.Zero.ToString(TimeFormat);
        });
    }
}
