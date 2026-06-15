using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Options;
using CUE4Parse.Utils;
using FModel.Extensions;
using FModel.Views.Snooper.Models;
using ImGuiNET;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace FModel.Views.Snooper;

public sealed class ExportModal
{
    public static ExportModal Instance { get; } = new();

    private const string Title = "Export Progress";
    private const string IconXMark = "\uf057";
    private const string IconFolder = "\uf08e";

    private readonly Vector4[] _pieColors =
    [
        new(0.22f, 0.52f, 0.90f, 1f),
        new(0.28f, 0.78f, 0.44f, 1f),
        new(0.90f, 0.62f, 0.22f, 1f),
        new(0.75f, 0.32f, 0.75f, 1f),
        new(0.32f, 0.75f, 0.85f, 1f),
        new(0.85f, 0.32f, 0.45f, 1f),
        new(0.90f, 0.90f, 0.22f, 1f),
        new(0.55f, 0.75f, 0.32f, 1f),
    ];

    private static readonly Vector4 _redColor = new(1f, 0.4f, 0.4f, 1f);
    private static readonly Vector4 _orangeColor = new(1f, 0.5f, 0f, 1f);
    private static readonly Vector4 _yellowColor = new(1f, 1f, 0.4f, 1f);
    private static readonly Vector4 _greenColor = new(0.4f, 1f, 0.4f, 1f);

    private bool _openPopup;
    private bool _modalOpen;
    private bool _inProgress;
    private CancellationTokenSource? _cts;
    private IReadOnlyList<ExportResult>? _exportResults;

    private ExportProgress _currentProgress;
    private readonly IProgress<ExportProgress> _progress;
    private readonly Stopwatch _stopwatch = new();
    private readonly ConcurrentQueue<LogEvent> _pendingLogs = new();
    private readonly List<ClassGroup> _classGroups = [];

    private const int MaxGraphSamples = 4096;
    private const float GraphSampleIntervalSec = 0.25f;
    private readonly List<float> _graphSamples = [];
    private float _graphNextSampleAt;
    private int _graphLastCompleted;

    private ExportModal()
    {
        ImGuiSink.Instance.OnExporterLogEvent += _pendingLogs.Enqueue;
        _progress = new Progress<ExportProgress>(p => _currentProgress = p);
    }

    public void Export(IEnumerable<IExportableThing> nodes, string exportDirectory, ExportOptions options)
    {
        Reset();
        _openPopup = true;
        _inProgress = true;
        _cts = new CancellationTokenSource();
        _stopwatch.Restart();

        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                var session = new ExportSession();
                foreach (var node in nodes) node.AddToExportSession(session);
                _exportResults = await session.RunAsync(exportDirectory, options, _progress, token);
            }
            catch (OperationCanceledException)
            {
                Log.Error("Export cancelled by user");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Export failed");
            }
            finally
            {
                _stopwatch.Stop();
                _inProgress = false;
            }
        }, token);
    }

    public void Draw()
    {
        if (_openPopup)
        {
            ImGui.OpenPopup(Title);
            _modalOpen = true;
            _openPopup = false;
        }

        if (!_modalOpen) return;

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowSize(viewport.WorkSize * 0.75f, ImGuiCond.Always);
        ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Always, new Vector2(0.5f, 0.5f));

        var open = true;
        if (ImGui.BeginPopupModal(Title, ref open, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
        {
            if (ImGui.BeginChild("##ModalInfoBody", Vector2.Zero, ImGuiChildFlags.FrameStyle))
            {
                DrawProgressBar();

                ImGui.Spacing();
                ImGui.SeparatorText("Throughput");
                DrawThroughputGraph();

                ImGui.Spacing();
                ImGui.SeparatorText("Export Log");
                DrawExportLog();
            }
            ImGui.EndChild();
            ImGui.EndPopup();
        }

        if (!open)
        {
            _modalOpen = false;
            Reset();
        }
    }

    private void Reset()
    {
        _pendingLogs.Clear();
        _classGroups.Clear();
        _exportResults = null;
        _currentProgress = new ExportProgress(0, 0);
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _graphSamples.Clear();
        _graphNextSampleAt = 0f;
        _graphLastCompleted = 0;
    }

    private void DrawProgressBar()
    {
        var e = _stopwatch.Elapsed;
        ImGui.TextDisabled("\uf2f2");
        ImGui.SameLine();
        ImGui.TextUnformatted($"{e.Minutes:D2}:{e.Seconds:D2}.{e.Milliseconds / 10:D2}");

        if (_inProgress && _currentProgress is { Total: > 0, Completed: > 1 })
        {
            var rate = _currentProgress.Completed / e.TotalSeconds;
            if (rate > 0)
            {
                var remaining = (_currentProgress.Total - _currentProgress.Completed) / rate;
                var eta = TimeSpan.FromSeconds(remaining);
                ImGui.SameLine();
                ImGui.TextDisabled("\uf017");
                ImGui.SameLine();
                ImGui.TextUnformatted($"ETA {eta.Minutes:D2}:{eta.Seconds:D2}");
            }
        }
        else if (!_inProgress && _exportResults is { Count: > 0 })
        {
            ImGui.SameLine();
            ImGui.TextColored(_greenColor, "\uf058");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{_exportResults?.Count(r => r.Success) ?? 0} succeeded");

            ImGui.SameLine();
            ImGui.TextColored(_redColor, IconXMark);
            ImGui.SameLine();
            ImGui.TextUnformatted($"{_exportResults?.Count(r => !r.Success) ?? 0} failed");
        }

        ImGui.Spacing();

        var barColor = _classGroups.Any(cg => cg.ErrorCount > 0) ? new Vector4(0.75f, 0.32f, 0.32f, 1f) : _inProgress ? new Vector4(0.22f, 0.52f, 0.90f, 1f) : new Vector4(0.28f, 0.78f, 0.44f, 1f);
        var label = _inProgress && _currentProgress.Total > 0 ? _currentProgress.DisplayText : _inProgress ? "Preparing..." : "Done";

        var barPos = ImGui.GetCursorScreenPos();
        var barSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        var hovered = ImGui.IsMouseHoveringRect(barPos, barPos + barSize);
        if (hovered && _inProgress)
        {
            barColor = new Vector4(0.75f, 0.32f, 0.32f, 1f);
            label = "\uf05e  Cancel";
        }

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
        ImGui.ProgressBar(_currentProgress.Percentage, barSize, label);
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);

        if (_inProgress)
        {
            ImGui.SetCursorScreenPos(barPos);
            if (ImGui.InvisibleButton("##BarAction", barSize))
            {
                _cts?.Cancel();
            }
        }
    }

    private void DrawThroughputGraph()
    {
        var elapsedSec = (float)_stopwatch.Elapsed.TotalSeconds;
        if (_inProgress && _currentProgress.Completed > 0 && elapsedSec >= _graphNextSampleAt && _graphSamples.Count < MaxGraphSamples)
        {
            var completed = _currentProgress.Completed;
            var rate = (completed - _graphLastCompleted) / GraphSampleIntervalSec;
            _graphSamples.Add(MathF.Max(rate, 0f));
            _graphLastCompleted = completed;
            _graphNextSampleAt = elapsedSec + GraphSampleIntervalSec;
        }

        var graphPos = ImGui.GetCursorScreenPos();
        var graphW = ImGui.GetContentRegionAvail().X;
        var graphH = ImGui.GetFrameHeight() * 3f;
        var graphSize = new Vector2(graphW, graphH);
        ImGui.InvisibleButton("##ThroughputGraph", graphSize);
        var dl = ImGui.GetWindowDrawList();

        dl.AddRectFilled(graphPos, graphPos + graphSize, 0xFF_14_14_14);

        var count = _graphSamples.Count;
        switch (count)
        {
            case 0 when !_inProgress:
            {
                dl.AddRect(graphPos, graphPos + graphSize, 0xFF_2A_2A_2A);
                return;
            }
            case < 2:
            {
                if (_inProgress)
                {
                    const string msg = "Collecting data...";
                    var msgSz = ImGui.CalcTextSize(msg);
                    dl.AddText(graphPos + (graphSize - msgSz) * 0.5f, 0x44_FF_FF_FF, msg);
                }
                dl.AddRect(graphPos, graphPos + graphSize, 0xFF_2A_2A_2A);
                return;
            }
        }

        // Y scale
        var maxVal = 0f;
        for (var i = 0; i < count; i++) maxVal = MathF.Max(maxVal, _graphSamples[i]);
        if (maxVal < 0.01f) maxVal = 1f;
        maxVal *= 1.15f; // top headroom

        dl.PushClipRect(graphPos, graphPos + graphSize, true);

        // Horizontal grid lines at 25 / 50 / 75 %
        for (var g = 1; g < 4; g++)
        {
            var gy = graphPos.Y + graphH * g / 4f;
            dl.AddLine(new Vector2(graphPos.X, gy), new Vector2(graphPos.X + graphW, gy), 0x18_FF_FF_FF, 0.5f);
        }

        var lineColor = ImGui.GetColorU32(new Vector4(0.22f, 0.52f, 0.90f, 1f));
        var fillColor = ImGui.GetColorU32(new Vector4(0.22f, 0.52f, 0.90f, 0.15f));

        // xStep shrinks as samples accumulate → graph zooms out naturally.
        // Oldest sample is always at x=0, newest at x=graphW.
        var xStep = graphW / (count - 1f);

        // Filled area (series of convex quads)
        for (var i = 0; i < count - 1; i++)
        {
            var t0 = Math.Clamp(_graphSamples[i] / maxVal, 0f, 1f);
            var t1 = Math.Clamp(_graphSamples[i + 1] / maxVal, 0f, 1f);
            var x0 = graphPos.X + xStep * i;
            var x1 = graphPos.X + xStep * (i + 1);
            var y0 = graphPos.Y + graphH - graphH * t0;
            var y1 = graphPos.Y + graphH - graphH * t1;
            var bot = graphPos.Y + graphH;
            dl.AddQuadFilled(new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(x1, bot), new Vector2(x0, bot), fillColor);
        }

        // Line
        for (var i = 0; i < count - 1; i++)
        {
            var t0 = Math.Clamp(_graphSamples[i] / maxVal, 0f, 1f);
            var t1 = Math.Clamp(_graphSamples[i + 1] / maxVal, 0f, 1f);
            var x0 = graphPos.X + xStep * i;
            var x1 = graphPos.X + xStep * (i + 1);
            var y0 = graphPos.Y + graphH - graphH * t0;
            var y1 = graphPos.Y + graphH - graphH * t1;
            dl.AddLine(new Vector2(x0, y0), new Vector2(x1, y1), lineColor, 1.5f);
        }

        // Dot on the newest sample (always at the right edge)
        var newestT = Math.Clamp(_graphSamples[count - 1] / maxVal, 0f, 1f);
        var newestY = graphPos.Y + graphH - graphH * newestT;
        dl.AddCircleFilled(new Vector2(graphPos.X + graphW, newestY), 3f, lineColor);

        var rateStr = $"{_graphSamples[count - 1]:F1} items/s";
        dl.AddText(new Vector2(graphPos.X + 4, graphPos.Y + 3), 0xCC_FF_FF_FF, rateStr);

        dl.PopClipRect();

        dl.AddRect(graphPos, graphPos + graphSize, 0xFF_2A_2A_2A);
    }

    private void DrawExportLog()
    {
        var avail = ImGui.GetContentRegionAvail();
        var rowH = ImGui.GetTextLineHeightWithSpacing();
        var canvasSize = 15 * rowH + ImGui.GetFrameHeightWithSpacing();
        var treeW = avail.X - canvasSize - ImGui.GetStyle().ItemSpacing.X;

        DrainPendingLogs();

        if (ImGui.BeginChild("##ExportLogTree", avail with { X = treeW }, ImGuiChildFlags.FrameStyle))
        {
            if (_classGroups.Count == 0)
            {
                ImGui.TextDisabled(_inProgress ? "Waiting for export data..." : "No export log.");
            }
            else for (var i = 0; i < _classGroups.Count; i++)
            {
                DrawClassGroup(i, _classGroups[i]);
            }
        }
        ImGui.EndChild();

        ImGui.SameLine();
        if (ImGui.BeginChild("##RightPanel", new Vector2(canvasSize, -1), ImGuiChildFlags.FrameStyle))
        {
            DrawPieCanvas();

            if (_classGroups.Count == 0)
            {
                ImGui.TextDisabled(_inProgress ? "Waiting for data..." : "No export data.");
            }
            else
            {
                var total = _classGroups.Sum(cg => cg.Objects.Count);
                for (var i = 0; i < _classGroups.Count; i++)
                {
                    var cg = _classGroups[i];
                    ImGui.PushStyleColor(ImGuiCol.Text, _pieColors[i % _pieColors.Length]);
                    ImGui.TextUnformatted("\uf111");
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.TextUnformatted(cg.Name);
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
                    ImGui.TextUnformatted($"({(float) cg.Objects.Count / total * 100f:F1}%)");
                    ImGui.PopStyleColor();
                }
            }
        }

        ImGui.EndChild();
    }

    private void DrawPieCanvas()
    {
        const int segments = 64;

        var canvasPos = ImGui.GetCursorScreenPos();
        var size = ImGui.GetContentRegionAvail().X;
        var canvasVec = new Vector2(size);
        ImGui.InvisibleButton("##PieCanvas", canvasVec);
        var isHovered = ImGui.IsItemHovered();
        var mousePos = ImGui.GetMousePos();
        var dl = ImGui.GetWindowDrawList();

        dl.AddRectFilled(canvasPos, canvasPos + canvasVec, 0xFF_14_14_14);
        dl.AddRect(canvasPos, canvasPos + canvasVec, 0xFF_32_32_32);

        var total = _classGroups.Sum(cg => cg.Objects.Count);
        var padding = ImGui.GetFrameHeight() * 0.5f;
        var radius = size * 0.5f - padding;
        var center = canvasPos + new Vector2(size * 0.5f);

        if (total == 0 || radius <= 0)
        {
            dl.AddCircleFilled(center, MathF.Max(radius, 1f), 0xFF_1F_1F_1F, segments);
            return;
        }

        // Determine hovered slice by angle
        var hoveredSlice = -1;
        if (isHovered)
        {
            var dx = mousePos.X - center.X;
            var dy = mousePos.Y - center.Y;
            if (dx * dx + dy * dy <= radius * radius)
            {
                var angle = MathF.Atan2(dy, dx);
                while (angle < -MathF.PI / 2f) angle += MathF.PI * 2f;
                var cur = -MathF.PI / 2f;
                for (var i = 0; i < _classGroups.Count; i++)
                {
                    var sweep = (float)_classGroups[i].Objects.Count / total * MathF.PI * 2f;
                    if (angle >= cur && angle < cur + sweep) { hoveredSlice = i; break; }
                    cur += sweep;
                }
            }
        }

        float startAngle = -MathF.PI / 2f;
        for (var i = 0; i < _classGroups.Count; i++)
        {
            var cg = _classGroups[i];
            var ratio = (float) cg.Objects.Count / total;
            var sliceAngle = ratio * MathF.PI * 2f;
            var col = ImGui.GetColorU32(_pieColors[i % _pieColors.Length]);
            var r = i == hoveredSlice ? radius + padding * 0.25f : radius;
            dl.PathLineTo(center);
            dl.PathArcTo(center, r, startAngle, startAngle + sliceAngle);
            dl.PathFillConvex(col);

            var midAngle = startAngle + sliceAngle * 0.5f;
            var labelPos = center + new Vector2(MathF.Cos(midAngle), MathF.Sin(midAngle)) * (radius * 0.62f);
            var pctStr = $"{ratio * 100f:F0}%";
            dl.AddText(labelPos - ImGui.CalcTextSize(pctStr) * 0.5f, 0xFF_FF_FF_FF, pctStr);

            startAngle += sliceAngle;
        }

        dl.AddCircle(center, radius, 0xAA_00_00_00, segments, 1.5f);

        if (hoveredSlice >= 0)
        {
            ImGui.BeginTooltip();

            var cg = _classGroups[hoveredSlice];
            ImGui.PushStyleColor(ImGuiCol.Text, _pieColors[hoveredSlice % _pieColors.Length]);
            ImGui.TextUnformatted("\uf111");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.TextUnformatted(cg.Name);

            ImGui.EndTooltip();
        }
    }

    private void DrawClassGroup(int index, ClassGroup cg)
    {
        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.20f, 0.20f, 0.20f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.69f, 0.69f, 1.00f, 0.20f));
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.69f, 0.69f, 1.00f, 0.20f));
        var open = ImGui.CollapsingHeader($"{cg.Name} ({cg.Objects.Count})##class_{cg.Name}");
        ImGui.PopStyleColor(3);
        var headerMin = ImGui.GetItemRectMin();
        var headerMax = ImGui.GetItemRectMax();

        var labelW = MathF.Floor(ImGui.GetStyle().ItemSpacing.X * 0.5f);
        var col = ImGui.GetColorU32(_pieColors[index % _pieColors.Length]);
        ImGui.GetWindowDrawList().AddRectFilled(headerMin, headerMax with { X = headerMin.X + labelW }, col);

        if (cg.ErrorCount > 0 && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{cg.ErrorCount} error{(cg.ErrorCount > 1 ? "s" : "")} in this class");
        }

        if (!open) return;
        foreach (var og in cg.Objects)
        {
            DrawObjectGroup(cg.Name, og);
        }
    }

    private void DrawObjectGroup(string className, ObjectGroup og)
    {
        var rightEdge = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;

        var hasErr = og.ErrorCount > 0;
        if (hasErr) ImGui.PushStyleColor(ImGuiCol.Text, _redColor);
        var flags = ImGuiTreeNodeFlags.AllowOverlap | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.FramePadding;
        var open = ImGui.TreeNodeEx($"{og.Name}##obj_{className}_{og.Name}", flags);
        if (hasErr) ImGui.PopStyleColor();

        if (og.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.FilePath)) is { } first)
        {
            var style = ImGui.GetStyle();
            var btnW = ImGui.CalcTextSize(IconFolder).X + style.FramePadding.X * 2;
            ImGui.SameLine(rightEdge - btnW);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, style.ItemSpacing with { X = 0 });
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            if (ImGui.Button($"{IconFolder}##obj_{className}_{og.Name}"))
            {
                OpenInExplorer(first.FilePath!);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Open In Explorer");
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }

        if (!open) return;
        foreach (var entry in og.Entries)
        {
            DrawLogEntry(entry);
        }
        ImGui.TreePop();
    }

    private void DrawLogEntry(LogEntry entry)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, entry.Color);
        ImGui.TextUnformatted(entry.Icon);
        ImGui.PopStyleColor();
        ImGui.SameLine();
        ImGui.TextUnformatted(entry.Message);
        if (ImGui.IsItemHovered() && entry.Exception != null)
        {
            DrawExceptionTooltip(entry.Exception);
        }
    }

    private static void DrawExceptionTooltip(Exception ex)
    {
        ImGui.BeginTooltip();
        ImGui.PushStyleColor(ImGuiCol.Text, _redColor);
        ImGui.TextUnformatted(ex.GetType().ToString());
        ImGui.PopStyleColor();
        ImGui.SameLine(0, 0);
        ImGui.TextDisabled(":");
        ImGui.SameLine();
        ImGui.TextUnformatted(ex.Message);
        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            ImGui.SetWindowFontScale(0.85f);
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
            ImGui.TextUnformatted(ex.StackTrace);
            ImGui.PopStyleColor();
            ImGui.SetWindowFontScale(1.0f);
        }
        ImGui.EndTooltip();
    }

    private void OpenInExplorer(string path)
    {
        try
        {
            if (File.Exists(path) || Directory.Exists(path)) Process.Start("explorer.exe", $"/select, \"{path}\"");
            else Log.Warning("File or directory does not exist: {Path}", path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open in explorer: {Path}", path);
        }
    }

    private void DrainPendingLogs()
    {
        while (_pendingLogs.TryDequeue(out var log))
        {
            var className = log.GetContext("ClassName");
            var objectPath = log.GetContext("ObjectPath");
            var filePath = log.GetContext("FilePath");

            var cg = FindOrCreateClass(className);
            var og = FindOrCreateObject(cg, objectPath);
            var entry = new LogEntry(log, filePath);
            if (entry.Icon == IconXMark)
            {
                og.ErrorCount++;
                cg.ErrorCount++;
            }
            og.Entries.Add(entry);
        }
    }

    private ClassGroup FindOrCreateClass(string name)
    {
        foreach (var cg in _classGroups)
            if (cg.Name == name) return cg;

        var n = new ClassGroup(name);
        _classGroups.Add(n);
        return n;
    }

    private static ObjectGroup FindOrCreateObject(ClassGroup cg, string path)
    {
        foreach (var og in cg.Objects)
            if (og.Path == path) return og;

        var n = new ObjectGroup(path);
        cg.Objects.Add(n);
        return n;
    }

    private sealed class LogEntry(LogEvent log, string? filePath)
    {
        public string Icon { get; } = log.Level switch
        {
            LogEventLevel.Error or LogEventLevel.Fatal => IconXMark,
            LogEventLevel.Warning => "\uf071",
            LogEventLevel.Information => "\uf05a",
            LogEventLevel.Debug => "\uf188",
            _ => "\uf5dc"
        };
        public Vector4 Color { get; } = log.Level switch
        {
            LogEventLevel.Error or LogEventLevel.Fatal => _redColor,
            LogEventLevel.Warning => _yellowColor,
            _ => new Vector4(0.5f, 0.5f, 0.5f, 1f)
        };
        public string Message { get; } = $"[{log.Timestamp:HH:mm:ss.fff}] {log.RenderMessage()}";
        public string? FilePath { get; } = filePath;
        public Exception? Exception { get; } = log.Exception;
    }

    private sealed class ObjectGroup(string path)
    {
        public string Path { get; } = path;
        public string Name { get; } = path.SubstringAfterLast('.');
        public List<LogEntry> Entries { get; } = [];
        public int ErrorCount { get; set; }
    }

    private sealed class ClassGroup(string name)
    {
        public string Name { get; } = name;
        public List<ObjectGroup> Objects { get; } = [];
        public int ErrorCount { get; set; }
    }
}

public class ImGuiSink : ILogEventSink
{
    public static ImGuiSink Instance { get; } = new();

    private ImGuiSink()
    {

    }

    public event Action<LogEvent>? OnExporterLogEvent;

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("ExporterV2", out var state) && state is ScalarValue { Value: true })
        {
            OnExporterLogEvent?.Invoke(logEvent);
        }
    }
}
