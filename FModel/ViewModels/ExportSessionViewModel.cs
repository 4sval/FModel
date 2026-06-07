using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using CUE4Parse_Conversion;
using FModel.Framework;
using FModel.Settings;

namespace FModel.ViewModels;

public class ExportSessionViewModel : ViewModel
{
    public static ExportSessionViewModel Instance { get; } = new();

    private int _previousCount;
    private DispatcherTimer? _toastTimer;
    public bool ShowQueueToast
    {
        get;
        set => SetProperty(ref field, value);
    }

    private ExportSession? _session;
    public ExportSession Session
    {
        get
        {
            if (_session != null) return _session;
            _session = new ExportSession(UserSettings.Default.OutputDirectory, UserSettings.GetExportOptions());
            _session.PropertyChanged += OnSessionPropertyChanged;
            return _session;
        }
    }

    private ExportSessionViewModel()
    {

    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ExportSession.TotalQueued)) return;

        var count = _session?.TotalQueued ?? 0;
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            switch (count)
            {
                case 1 when _previousCount == 0:
                    ShowToast();
                    break;
                case 0:
                    HideToast();
                    break;
            }

            _previousCount = count;
        });
    }

    private void ShowToast()
    {
        ShowQueueToast = true;
        if (_toastTimer == null)
        {
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7.5) };
            _toastTimer.Tick += (_, _) => HideToast();
        }
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void HideToast()
    {
        ShowQueueToast = false;
        _toastTimer?.Stop();
    }

    public void Invalidate()
    {
        _session?.PropertyChanged -= OnSessionPropertyChanged;
        _session = null;
        Application.Current?.Dispatcher.InvokeAsync(HideToast);
    }
}
