using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Options;
using CUE4Parse.Utils;
using FModel.Extensions;
using FModel.Framework;
using FModel.Settings;
using FModel.Views;
using FModel.Views.Resources.Controls;
using FModel.Views.Snooper;
using Serilog.Events;

namespace FModel.ViewModels;

public class ExportSessionViewModel : ViewModel
{
    public static ExportSessionViewModel Instance { get; } = new();

    private DispatcherTimer? _toastTimer;
    public bool ShowQueueToast
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            if (!value)
            {
                _toastTimer?.Stop();
                return;
            }

            if (_toastTimer == null)
            {
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                _toastTimer.Tick += (_, _) =>
                {
                    field = false;
                    RaisePropertyChanged(nameof(ShowQueueToast));
                    _toastTimer.Stop();
                };
            }

            _toastTimer.Stop();
            _toastTimer.Start();
        }
    }

    private ExportSession? _session;
    public ExportSession Session
    {
        get
        {
            if (_session != null) return _session;
            _session = new ExportSession((args, ct) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = new StreamingLevelFilterWindow(new StreamingLevelFilterViewModel(args));
                    _stopwatch.Stop();
                    window.ShowDialog();
                    _stopwatch.Start();
                }, DispatcherPriority.Normal, ct);
            });
            _session.PropertyChanged += OnSessionPropertyChanged;
            return _session;
        }
    }

    public ExportOptionsViewModel Options { get; } = new();

    public bool IsRunning
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value)) return;
            RaisePropertyChanged(nameof(CanExport));
        }
    }
    public bool IsFinished
    {
        get;
        private set => SetProperty(ref field, value);
    }
    public bool CanExport => !IsRunning && Session.TotalQueued > 0;

    public int CompletedCount
    {
        get;
        private set => SetProperty(ref field, value);
    }
    public int SucceededCount
    {
        get;
        private set => SetProperty(ref field, value);
    }
    public int FailedCount
    {
        get;
        private set => SetProperty(ref field, value);
    }
    public string? CurrentItemName
    {
        get;
        private set => SetProperty(ref field, value);
    }
    public TimeSpan ElapsedTime
    {
        get;
        private set => SetProperty(ref field, value);
    }
    public TimeSpan? EtaTime
    {
        get;
        private set => SetProperty(ref field, value);
    }
    public bool IsCanceled
    {
        get;
        private set => SetProperty(ref field, value);
    }
    public double ProgressValue
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public ObservableCollection<ClassGroupViewModel> ClassGroups { get; } = [];

    private CancellationTokenSource? _cts;
    private readonly Stopwatch _stopwatch = new();
    private readonly ConcurrentQueue<LogEvent> _pendingLogs = new();
    private DispatcherTimer? _uiTimer;

    private ExportSessionViewModel()
    {
        ImGuiSink.Instance.OnExporterLogEvent += OnLogEvent;
    }

    private void OnLogEvent(LogEvent log)
    {
        _pendingLogs.Enqueue(log);
        Application.Current?.Dispatcher.InvokeAsync(DrainLogs);
    }

    private int _previousCount;
    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ExportSession.TotalQueued)) return;

        var count = _session?.TotalQueued ?? 0;
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (count > 0 && _previousCount == 0)
            {
                ClearExportHistory();
            }

            ShowQueueToast = count switch
            {
                > 0 when _previousCount == 0 => true,
                0 => false,
                _ => ShowQueueToast
            };
            _previousCount = count;
            RaisePropertyChanged(nameof(CanExport));
        });
    }

    public async Task<IReadOnlyList<ExportResult>> ExportAsync()
    {
        if (IsRunning || Session.TotalQueued == 0) return null;

        IsRunning = true;
        IsFinished = false;
        IsCanceled = false;
        CompletedCount = 0;
        SucceededCount = 0;
        FailedCount = 0;
        _stopwatch.Restart();

        _cts = new CancellationTokenSource();
        StartUiTimer();

        string exportDirectory;
        ExportOptions exportOptions;
        if (Options.OverrideOptions)
        {
            exportDirectory = Options.OutputDirectory;
            exportOptions = Options.BuildOptions();
        }
        else
        {
            exportDirectory = UserSettings.Default.ModelDirectory;
            exportOptions = UserSettings.GetExportOptions();
        }

        var progress = new Progress<ExportProgress>(p =>
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                CompletedCount = p.Completed;
                CurrentItemName = p.LastResult?.ObjectPath;
                if (p.LastResult != null)
                {
                    if (p.LastResult.Success) SucceededCount++;
                    else FailedCount++;
                }
                ProgressValue = p.Total > 0 ? (double)p.Completed / p.Total : 0;
            });
        });

        IReadOnlyList<ExportResult> results = null;
        try
        {
            results = await Session.RunAsync(exportDirectory, exportOptions, progress, _cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Application.Current?.Dispatcher.InvokeAsync(() => IsCanceled = true);
        }
        finally
        {
            _stopwatch.Stop();
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                StopUiTimer();
                IsRunning = false;
                IsFinished = !IsCanceled;
                UpdateElapsedAndEta();
            });
        }
        return results;
    }

    public async Task ExportAutomaticallyAsync()
    {
        if (!UserSettings.Default.ExportImmediately) return;

        var results = await ExportAsync();
        if (results is { Count: > 0 }) LogSummary(results);
    }

    private static void LogSummary(IReadOnlyList<ExportResult> results)
    {
        if (results.Count == 1 && results[0].DiskFilePaths is not { Count: > 1 })
        {
            var result = results[0];
            switch (result.Success)
            {
                case true when result.DiskFilePaths is { Count: > 0 } files:
                    FLogger.Append(ELog.Information, () =>
                    {
                        FLogger.Text("Successfully exported ", Constants.WHITE);
                        FLogger.Link(Path.GetFileName(files[0]), files[0], true);
                    });
                    break;
                case false when result.Error is { } exception:
                    FLogger.Append(exception);
                    break;
            }
            return;
        }

        var failed = 0;
        var groups = new Dictionary<string, (int Count, string[] Source, string[] Directory)>(StringComparer.OrdinalIgnoreCase);

        foreach (var result in results)
        {
            if (!result.Success)
            {
                failed++;
                continue;
            }

            if (result.DiskFilePaths == null) continue;

            foreach (var file in result.DiskFilePaths)
            {
                var extension = Path.GetExtension(file).TrimStart('.');
                var source = SplitDirectory(result.ObjectPath.TrimStart('/', '\\'));
                var directory = SplitDirectory(file);
                groups[extension] = groups.TryGetValue(extension, out var group)
                    ? (group.Count + 1, CommonPrefix(group.Source, source), CommonPrefix(group.Directory, directory))
                    : (1, source, directory);
            }
        }

        foreach (var (extension, group) in groups)
        {
            var source = string.Join('/', group.Source);
            var directory = string.Join(Path.DirectorySeparatorChar, group.Directory);
            if (source.Length == 0)
                source = "multiple folders";

            FLogger.Append(ELog.Information, () =>
            {
                FLogger.Text($"Successfully exported {group.Count} {extension} from ", Constants.WHITE);
                if (directory.Length > 0) FLogger.Link(source, directory, true);
                else FLogger.Text(source, Constants.WHITE, true);
            });
        }

        if (failed > 0)
        {
            FLogger.Append(ELog.Error, () => FLogger.Text($"Failed to export {failed} asset{(failed == 1 ? "" : "s")}, open the Export Session window for more details.", Constants.WHITE, true));
        }
    }

    private static string[] SplitDirectory(string path) => Path.GetDirectoryName(path)?.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) ?? [];
    private static string[] CommonPrefix(string[] a, string[] b)
    {
        var i = 0;
        while (i < a.Length && i < b.Length && string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase)) i++;
        return i == a.Length ? a : a[..i];
    }

    public void CancelExport()
    {
        _cts?.Cancel();
    }

    public void ClearQueue()
    {
        _session?.Clear();
        ClearExportHistory();
    }

    public void RemoveFromQueue(ObjectGroupViewModel item)
    {
        if (IsRunning || _session?.Remove(item.Path) != true)
            return;

        var group = ClassGroups.FirstOrDefault(x => x.Objects.Contains(item));
        if (group == null)
            return;

        group.Objects.Remove(item);
        if (group.Objects.Count == 0)
        {
            ClassGroups.Remove(group);
        }
    }

    private void StartUiTimer()
    {
        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _uiTimer.Tick += (_, _) => UpdateElapsedAndEta();
        _uiTimer.Start();
    }

    private void StopUiTimer()
    {
        _uiTimer?.Stop();
        _uiTimer = null;
    }

    private void UpdateElapsedAndEta()
    {
        ElapsedTime = _stopwatch.Elapsed;

        var remaining = Session.TotalQueued;
        if (IsRunning && remaining > 0 && CompletedCount > 1 && ElapsedTime.TotalSeconds > 0)
        {
            var rate = CompletedCount / ElapsedTime.TotalSeconds;
            if (rate > 0)
            {
                EtaTime = TimeSpan.FromSeconds(remaining / rate);
                return;
            }
        }
        EtaTime = null;
    }

    private void ClearExportHistory()
    {
        CompletedCount = 0;
        SucceededCount = 0;
        FailedCount = 0;
        ProgressValue = 0;
        ElapsedTime = TimeSpan.Zero;
        EtaTime = null;
        CurrentItemName = null;
        IsFinished = false;
        IsCanceled = false;
        ClassGroups.Clear();
    }

    private void DrainLogs()
    {
        while (_pendingLogs.TryDequeue(out var log))
        {
            var className = log.GetContext("ClassName");
            var objectPath = log.GetContext("ObjectPath");
            var filePath = log.GetContext("FilePath");

            var cg = FindOrCreateClass(className);
            var og = FindOrCreateObject(cg, objectPath);
            if (log.Level >= LogEventLevel.Error)
            {
                og.ErrorCount++;
                cg.ErrorCount++;
            }
            if (og.FirstFilePath == null && !string.IsNullOrEmpty(filePath))
                og.FirstFilePath = filePath;
            og.Entries.Add(new LogEntryViewModel(log));
        }
    }

    private ClassGroupViewModel FindOrCreateClass(string name)
    {
        var cg = ClassGroups.FirstOrDefault(c => c.Name == name);
        if (cg != null) return cg;
        cg = new ClassGroupViewModel(name);
        ClassGroups.Add(cg);
        return cg;
    }

    private static ObjectGroupViewModel FindOrCreateObject(ClassGroupViewModel cg, string path)
    {
        var og = cg.Objects.FirstOrDefault(o => o.Path == path);
        if (og != null) return og;
        og = new ObjectGroupViewModel(path);
        cg.Objects.Add(og);
        return og;
    }
}

public class ClassGroupViewModel(string name) : ViewModel
{
    public string Name { get; } = name;
    public ObservableCollection<ObjectGroupViewModel> Objects { get; } = [];

    public bool IsExpanded
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int ErrorCount
    {
        get;
        set
        {
            SetProperty(ref field, value);
            RaisePropertyChanged(nameof(HasErrors));
        }
    }
    public override bool HasErrors => ErrorCount > 0;
}

public class ObjectGroupViewModel(string path) : ViewModel
{
    public string Path { get; } = path;
    public string Directory { get; } = path.SubstringBeforeLast('/');
    public string Name { get; } = path.SubstringAfterLast('.');
    public ObservableCollection<LogEntryViewModel> Entries { get; } = [];

    public bool IsExpanded
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int ErrorCount
    {
        get;
        set
        {
            SetProperty(ref field, value);
            RaisePropertyChanged(nameof(HasErrors));
        }
    }
    public override bool HasErrors => ErrorCount > 0;

    public string? FirstFilePath
    {
        get;
        set
        {
            SetProperty(ref field, value);
            RaisePropertyChanged(nameof(HasFilePath));
        }
    }
    public bool HasFilePath => FirstFilePath != null;
}

public class LogEntryViewModel(LogEvent log)
{
    public LogEventLevel Level { get; } = log.Level;
    public DateTimeOffset Timestamp { get; } = log.Timestamp;
    public string Message { get; } = log.Exception switch
    {
        NullReferenceException or ArgumentException => log.RenderMessage(),
        _ => log.Exception?.Message ?? log.RenderMessage()
    };
    public Exception? Exception { get; } = log.Exception;
    public IReadOnlyList<ExceptionDetailsViewModel> ExceptionDetails { get; } =
        log.Exception is { } exception ? [new ExceptionDetailsViewModel(exception)] : [];
}

public class ExceptionDetailsViewModel
{
    public string Header { get; }
    public string Details { get; }

    public ExceptionDetailsViewModel(Exception exception)
    {
        var text = exception.ToString().ReplaceLineEndings("\n");
        var newline = text.IndexOf('\n');

        if (newline < 0)
        {
            Header = text;
            Details = string.Empty;
        }
        else
        {
            Header = text[..newline];
            Details = text[(newline + 1)..];
        }
    }
}
