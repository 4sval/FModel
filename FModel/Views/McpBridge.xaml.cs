using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AdonisUI.Controls;
using FModel.Settings;

namespace FModel.Views;

public partial class McpBridge : AdonisWindow
{
    private Process? _bridgeProcess;
    private bool _isRunning;

    // File-tail state — we read bridge-events.log written by CUE4Parse-MCP
    // This works regardless of whether FModel or Claude Desktop spawned the bridge
    private Timer? _tailTimer;
    private long _tailPosition;
    private string _eventLogPath = "";

    // All log entries kept in memory for re-filtering
    private readonly List<LogEntry> _logEntries = new();
    private const int MaxLogEntries = 2000;

    // Resolve repo root: FModel exe lives at <repo>/FModel/bin/Release/net8.0-windows/win-x64/
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string BridgeExePath = Path.Combine(
        RepoRoot, "CUE4Parse-MCP", "bin", "Release", "net8.0", "CUE4Parse-MCP.exe");

    private static readonly string BridgeProjectDir = Path.Combine(
        RepoRoot, "CUE4Parse-MCP");

    // Tag → color mapping (IDA-style)
    private static readonly Dictionary<string, Color> TagColors = new()
    {
        ["REQ"]    = Color.FromRgb(0x6B, 0xA3, 0xF7), // blue
        ["RESP"]   = Color.FromRgb(0x4E, 0xC9, 0x4E), // green
        ["TOOL"]   = Color.FromRgb(0xC8, 0x9B, 0xF7), // purple
        ["ERROR"]  = Color.FromRgb(0xF7, 0x6B, 0x6B), // red
        ["FATAL"]  = Color.FromRgb(0xF7, 0x6B, 0x6B), // red
        ["SERVER"] = Color.FromRgb(0x88, 0x88, 0x88), // gray
    };

    public McpBridge()
    {
        InitializeComponent();

        try { UpdateStatus(false); } catch { /* ignore during init */ }
        try { UpdateConfigSnippet(); } catch { /* ignore during init */ }

        // Tail bridge-events.log — catches all traffic from the bridge regardless of who started it
        StartFileTailing();

        // Kill the bridge when FModel exits (not just when this window closes)
        Application.Current.Exit += (_, _) =>
        {
            StopBridge();
            StopFileTailing();
        };
    }

    // ---- Bridge Process Management ----

    private void OnToggleClick(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
            StopBridge();
        else
            StartBridge();
    }

    private void StartBridge()
    {
        var exePath = Path.GetFullPath(BridgeExePath);

        if (!File.Exists(exePath))
        {
            AppendLog("SERVER", "Bridge exe not found. Build CUE4Parse-MCP first.");
            return;
        }

        try
        {
            AppendLog("SERVER", $"Starting: {exePath}");

            // Auto-detect the .usmap mappings file from FModel's own .data cache
            // FModel downloads/caches usmap files as *_oo.usmap in {OutputDirectory}\.data\
            string? mappingsPath = null;
            var dataDir = Path.Combine(UserSettings.Default.OutputDirectory, ".data");
            if (Directory.Exists(dataDir))
            {
                var latestUsmap = new DirectoryInfo(dataDir)
                    .GetFiles("*_oo.usmap")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
                if (latestUsmap != null)
                    mappingsPath = latestUsmap.FullName;
            }

            if (mappingsPath != null)
                AppendLog("SERVER", $"Auto-detected mappings: {mappingsPath}");
            else
                AppendLog("SERVER", "WARNING: No *_oo.usmap found in .data dir — unversioned properties won't deserialize");

            _bridgeProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = mappingsPath != null ? $"--mappings \"{mappingsPath}\"" : "",
                    UseShellExecute = false,
                    // Stdin/stdout are needed by MCP; we do NOT redirect stderr here
                    // because the bridge writes structured logs to bridge-events.log
                    // and that file is already being tailed by StartFileTailing().
                    RedirectStandardInput  = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError  = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            _bridgeProcess.Exited += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    AppendLog("SERVER", "Bridge process exited.");
                    UpdateStatus(false);
                });
            };

            _bridgeProcess.Start();
            UpdateStatus(true);
            AppendLog("SERVER", $"Bridge started (PID {_bridgeProcess.Id}) — watching bridge-events.log");
        }
        catch (Exception ex)
        {
            AppendLog("ERROR", $"Failed to start: {ex.Message}");
            UpdateStatus(false);
        }
    }

    // ---- File Tailing ----

    private void StartFileTailing()
    {
        _eventLogPath = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(BridgeExePath)) ?? "",
            "bridge-events.log");

        // Reset tail position — if the file already exists show from the start,
        // but a new bridge run will truncate the file so _tailPosition > fileLen
        // resets automatically in TailLogFile().
        _tailPosition = 0;

        // Poll every 150 ms — snappy enough for live logs, low enough CPU
        _tailTimer = new Timer(_ => TailLogFile(), null, 0, 150);
    }

    private void TailLogFile()
    {
        try
        {
            if (!File.Exists(_eventLogPath)) return;

            using var fs = new FileStream(_eventLogPath,
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Bridge restarted and truncated the file
            if (_tailPosition > fs.Length) _tailPosition = 0;
            if (_tailPosition >= fs.Length) return;

            fs.Seek(_tailPosition, SeekOrigin.Begin);
            using var reader = new StreamReader(fs, Encoding.UTF8, leaveOpen: true);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var captured = line;
                Dispatcher.BeginInvoke(() => ParseAndAppendBridgeLine(captured));
            }

            _tailPosition = fs.Position;
        }
        catch { /* ignore transient file-access errors */ }
    }

    private void StopFileTailing()
    {
        _tailTimer?.Dispose();
        _tailTimer = null;
    }

    private void StopBridge()
    {
        var proc = _bridgeProcess;
        _bridgeProcess = null;
        UpdateStatus(false);

        if (proc == null) return;

        // Kill on a background thread so the UI thread never blocks
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                if (!proc.HasExited) proc.Kill(true);
                proc.Dispose();
            }
            catch { }
        }).ContinueWith(_ =>
            Dispatcher.BeginInvoke(() => AppendLog("SERVER", "Bridge stopped.")));
    }

    // ---- Structured Log Parsing ----

    /// <summary>
    /// Parses a structured log line from the bridge process stderr.
    /// Expected format: [HH:mm:ss.fff] TAG      | message
    /// Falls back to raw display if the format doesn't match.
    /// </summary>
    private void ParseAndAppendBridgeLine(string line)
    {
        // Try to parse the structured format
        // [12:34:56.789] REQ      | tools/call (id=1) tool=load_game
        if (line.Length > 16 && line[0] == '[' && line[13] == ']')
        {
            var afterTimestamp = line[15..]; // skip "] "
            var pipeIdx = afterTimestamp.IndexOf('|');
            if (pipeIdx > 0)
            {
                var tag = afterTimestamp[..pipeIdx].Trim();
                var message = afterTimestamp[(pipeIdx + 1)..].TrimStart();
                AppendLog(tag, message);
                return;
            }
        }

        // Fallback: treat as a plain server message
        AppendLog("SERVER", line);
    }

    // ---- Log Display ----

    private void AppendLog(string tag, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Tag = tag,
            Message = message
        };

        _logEntries.Add(entry);

        // Trim old entries
        while (_logEntries.Count > MaxLogEntries)
            _logEntries.RemoveAt(0);

        if (IsTagVisible(tag))
        {
            AddLogParagraph(entry);
            ScrollToBottom();
        }
    }

    private void AddLogParagraph(LogEntry entry)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 1) };

        // Timestamp in dim gray
        var tsRun = new Run($"[{entry.Timestamp:HH:mm:ss.fff}] ")
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
        };
        paragraph.Inlines.Add(tsRun);

        // Tag in its color, bold
        var tagColor = TagColors.GetValueOrDefault(entry.Tag, Color.FromRgb(0x88, 0x88, 0x88));
        var tagRun = new Run($"{entry.Tag,-8}")
        {
            Foreground = new SolidColorBrush(tagColor),
            FontWeight = FontWeights.Bold
        };
        paragraph.Inlines.Add(tagRun);

        // Separator
        paragraph.Inlines.Add(new Run("│ ") { Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)) });

        // Message — highlight arrows for TOOL lines
        if (entry.Tag == "TOOL" && entry.Message.Length > 1)
        {
            var arrow = entry.Message[0];
            if (arrow == '→' || arrow == '←')
            {
                var arrowColor = arrow == '→'
                    ? Color.FromRgb(0x6B, 0xA3, 0xF7)  // blue for outgoing
                    : Color.FromRgb(0x4E, 0xC9, 0x4E);  // green for incoming
                paragraph.Inlines.Add(new Run($"{arrow} ") { Foreground = new SolidColorBrush(arrowColor), FontWeight = FontWeights.Bold });
                paragraph.Inlines.Add(new Run(entry.Message[2..]) { Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)) });
            }
            else
            {
                paragraph.Inlines.Add(new Run(entry.Message) { Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)) });
            }
        }
        else if (entry.Tag == "ERROR" || entry.Tag == "FATAL")
        {
            paragraph.Inlines.Add(new Run(entry.Message) { Foreground = new SolidColorBrush(Color.FromRgb(0xF7, 0x6B, 0x6B)) });
        }
        else
        {
            paragraph.Inlines.Add(new Run(entry.Message) { Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)) });
        }

        LogOutput.Document.Blocks.Add(paragraph);
    }

    private void ScrollToBottom()
    {
        LogScrollViewer.ScrollToEnd();
    }

    // ---- Filtering ----

    private bool IsTagVisible(string tag)
    {
        return tag switch
        {
            "REQ" => FilterReq.IsChecked == true,
            "RESP" => FilterResp.IsChecked == true,
            "TOOL" => FilterTool.IsChecked == true,
            "ERROR" or "FATAL" => FilterError.IsChecked == true,
            "SERVER" => FilterServer.IsChecked == true,
            _ => true
        };
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        // Guard: CheckBox events fire during InitializeComponent before LogOutput is ready
        if (LogOutput == null) return;
        RebuildLogDisplay();
    }

    private void RebuildLogDisplay()
    {
        LogOutput.Document.Blocks.Clear();
        foreach (var entry in _logEntries)
        {
            if (IsTagVisible(entry.Tag))
                AddLogParagraph(entry);
        }
        ScrollToBottom();
    }

    private void OnClearLogClick(object sender, RoutedEventArgs e)
    {
        _logEntries.Clear();
        LogOutput.Document.Blocks.Clear();
    }

    // ---- Status / Config ----

    private void UpdateStatus(bool running)
    {
        _isRunning = running;
        StatusDot.Fill = running
            ? new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x4E))
            : new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
        StatusText.Text = running ? "Running" : "Stopped";
        ToggleButton.Content = running ? "Stop Bridge" : "Start Bridge";
    }

    // Proxy exe is what claude_desktop_config.json points to — it relays stdio to the bridge's named pipe
    private static readonly string ProxyExePath = Path.Combine(
        RepoRoot, "CUE4Parse-MCP-Proxy", "bin", "Release", "net8.0", "CUE4Parse-MCP-Proxy.exe");

    private string McpConfig => "{\n  \"mcpServers\": {\n    \"cue4parse\": {\n      \"command\": \""
        + Path.GetFullPath(ProxyExePath).Replace("\\", "\\\\")
        + "\"\n    }\n  }\n}";

    private void UpdateConfigSnippet()
    {
        ConfigSnippet.Text = McpConfig;
    }

    private void OnCopyConfigClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(McpConfig);
            AppendLog("SERVER", "Config copied to clipboard.");
        }
        catch (Exception ex)
        {
            AppendLog("ERROR", $"Copy failed: {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Stop tailing the log file when the window closes
        // Bridge keeps running — FModel.Exit kills it
        StopFileTailing();
        base.OnClosed(e);
    }

    // ---- Log Entry Model ----

    private class LogEntry
    {
        public DateTime Timestamp { get; init; }
        public string Tag { get; init; } = "";
        public string Message { get; init; } = "";
    }
}
