using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using CUE4Parse_MCP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "cue4parse-mcp.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Log.Information("CUE4Parse-MCP server starting (named pipe transport)");
BridgeLog.Emit("SERVER", "CUE4Parse-MCP v2.0.0 starting up");
BridgeLog.Emit("SERVER", $"Named pipe: \\\\.\\pipe\\{PipeTransport.PipeName}");
BridgeLog.Emit("SERVER", "FModel controls this process — waiting for proxy connections");

// Parse --mappings <path> from CLI args (passed by McpBridge when starting this process)
string? defaultMappingsPath = null;
{
    var cliArgs = Environment.GetCommandLineArgs();
    for (int i = 1; i < cliArgs.Length - 1; i++)
    {
        if (cliArgs[i].Equals("--mappings", StringComparison.OrdinalIgnoreCase))
        {
            defaultMappingsPath = cliArgs[i + 1];
            break;
        }
    }
    if (defaultMappingsPath != null && File.Exists(defaultMappingsPath))
        BridgeLog.Emit("SERVER", $"Default mappings: {defaultMappingsPath}");
    else if (defaultMappingsPath != null)
    {
        BridgeLog.Emit("SERVER", $"WARNING: --mappings path not found: {defaultMappingsPath}");
        defaultMappingsPath = null;
    }
    else
        BridgeLog.Emit("SERVER", "No --mappings arg — will search candidates at load_game time");
}

var transport = new PipeTransport(defaultMappingsPath);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    await transport.RunAsync(cts.Token);
}
catch (OperationCanceledException) { }
catch (Exception ex)
{
    Log.Fatal(ex, "MCP server crashed");
    BridgeLog.Emit("FATAL", $"Server crashed: {ex.Message}");
}
finally
{
    BridgeLog.Emit("SERVER", "Shutting down");
    Log.CloseAndFlush();
}

namespace CUE4Parse_MCP
{
    public static class BridgeLog
    {
        private static readonly object Lock = new();
        private static readonly TextWriter StdErr = Console.Error;

        public static readonly string EventLogPath =
            Path.Combine(AppContext.BaseDirectory, "bridge-events.log");

        private static readonly StreamWriter EventLogWriter = OpenEventLog();

        private static StreamWriter OpenEventLog()
        {
            try
            {
                var fs = new FileStream(EventLogPath,
                    FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                return new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };
            }
            catch { return new StreamWriter(Stream.Null); }
        }

        public static void Emit(string tag, string message)
        {
            var ts = DateTime.Now.ToString("HH:mm:ss.fff");
            var line = $"[{ts}] {tag,-8} | {message}";
            lock (Lock)
            {
                StdErr.WriteLine(line);
                StdErr.Flush();
                EventLogWriter.WriteLine(line);
            }
        }

        public static void Request(string method, JToken? id, string? summary = null)
        {
            var idStr = id?.ToString() ?? "n/a";
            var msg = summary != null ? $"{method} (id={idStr}) {summary}" : $"{method} (id={idStr})";
            Emit("REQ", msg);
        }

        public static void Response(JToken? id, int bytes, double elapsedMs)
            => Emit("RESP", $"id={id} | {bytes} bytes | {elapsedMs:F1}ms");

        public static void ToolCall(string toolName, JObject? args)
        {
            var argsSummary = args != null ? TruncateJson(args.ToString(Formatting.None), 200) : "{}";
            Emit("TOOL", $"→ {toolName}({argsSummary})");
        }

        public static void ToolResult(string toolName, bool isError, int resultBytes, double elapsedMs)
            => Emit("TOOL", $"← {toolName} [{(isError ? "ERROR" : "OK")}] {resultBytes} bytes in {elapsedMs:F1}ms");

        public static void Error(string message) => Emit("ERROR", message);

        private static string TruncateJson(string json, int max)
            => json.Length <= max ? json : json[..max] + "…";
    }

    /// <summary>
    /// MCP transport over a Windows named pipe.
    /// FModel starts this bridge; Claude Desktop connects via CUE4Parse-MCP-Proxy.exe.
    /// Each proxy connection gets its own pipe instance and ToolHandler session.
    /// GameProvider (asset data) is shared across all sessions.
    /// </summary>
    public class PipeTransport
    {
        public const string PipeName = "cue4parse-mcp";

        // Shared provider so game assets only need to be loaded once
        private readonly GameProvider _sharedProvider;

        public PipeTransport(string? defaultMappingsPath = null)
        {
            _sharedProvider = new GameProvider(defaultMappingsPath);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            int sessionCount = 0;  // accessed via Interlocked below

            while (!ct.IsCancellationRequested)
            {
                var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                try
                {
                    BridgeLog.Emit("SERVER", "Waiting for proxy connection…");
                    await pipe.WaitForConnectionAsync(ct);

                    var sessionId = Interlocked.Increment(ref sessionCount);
                    BridgeLog.Emit("SERVER", $"Proxy connected (session #{sessionId})");

                    // Each session gets its own handler that shares the provider
                    var sessionHandler = new ToolHandler(_sharedProvider);
                    _ = Task.Run(() => HandleSessionAsync(pipe, sessionHandler, sessionId, ct), ct);
                }
                catch (OperationCanceledException) { pipe.Dispose(); break; }
                catch (Exception ex)
                {
                    BridgeLog.Error($"Pipe accept error: {ex.Message}");
                    pipe.Dispose();
                }
            }
        }

        private static async Task HandleSessionAsync(
            NamedPipeServerStream pipe,
            ToolHandler handler,
            int sessionId,
            CancellationToken ct)
        {
            try
            {
                var reader = new StreamReader(pipe, Encoding.UTF8);
                var writer = new StreamWriter(pipe, new UTF8Encoding(false))
                {
                    AutoFlush = true,
                    NewLine = "\n"
                };

                while (!ct.IsCancellationRequested)
                {
                    string? line;
                    try { line = await reader.ReadLineAsync(ct); }
                    catch { break; }

                    if (line == null) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    JObject request;
                    try { request = JObject.Parse(line); }
                    catch (Exception ex)
                    {
                        BridgeLog.Error($"Malformed JSON: {ex.Message}");
                        continue;
                    }

                    var method = request["method"]?.ToString() ?? "?";
                    var id = request["id"];

                    string? summary = method == "tools/call"
                        ? $"tool={request["params"]?["name"]}"
                        : null;

                    BridgeLog.Request(method, id, summary);

                    var sw = Stopwatch.StartNew();
                    var response = handler.HandleRequest(request);
                    sw.Stop();

                    if (response != null)
                    {
                        var responseStr = response.ToString(Formatting.None);
                        BridgeLog.Response(id, Encoding.UTF8.GetByteCount(responseStr), sw.Elapsed.TotalMilliseconds);
                        await writer.WriteLineAsync(responseStr);
                    }
                }
            }
            catch { /* client disconnected */ }
            finally
            {
                BridgeLog.Emit("SERVER", $"Session #{sessionId} disconnected");
                pipe.Dispose();
            }
        }
    }

    public class ToolHandler
    {
        private readonly GameProvider _provider;

        // Constructor for shared provider (used by PipeTransport sessions)
        public ToolHandler(GameProvider provider) => _provider = provider;

        // Default constructor (standalone use)
        public ToolHandler() => _provider = new GameProvider();

        private static readonly JArray ToolDefinitions = JArray.Parse("""
        [
            {
                "name": "load_game",
                "description": "Load a game's IoStore/PAK archives. Must be called before any other tool.",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "game_dir": { "type": "string", "description": "Path to game install directory" },
                        "game": { "type": "string", "description": "Game identifier e.g. GAME_ARKSurvivalAscended", "default": "GAME_ARKSurvivalAscended" },
                        "aes_key": { "type": "string", "description": "Optional AES-256 key (0x... hex)" }
                    },
                    "required": ["game_dir"]
                }
            },
            {
                "name": "list_assets",
                "description": "List assets under a virtual path.",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "path": { "type": "string", "description": "Virtual path prefix" },
                        "extension_filter": { "type": "string" },
                        "limit": { "type": "integer", "default": 100 }
                    },
                    "required": ["path"]
                }
            },
            {
                "name": "extract_json",
                "description": "Extract a single asset as JSON.",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "asset_path": { "type": "string" },
                        "export_index": { "type": "integer", "default": -1 }
                    },
                    "required": ["asset_path"]
                }
            },
            {
                "name": "get_skeleton",
                "description": "Extract bone hierarchy from a USkeleton or USkeletalMesh.",
                "inputSchema": {
                    "type": "object",
                    "properties": { "asset_path": { "type": "string" } },
                    "required": ["asset_path"]
                }
            },
            {
                "name": "get_bounds",
                "description": "Extract ImportedBounds from a USkeletalMesh.",
                "inputSchema": {
                    "type": "object",
                    "properties": { "asset_path": { "type": "string" } },
                    "required": ["asset_path"]
                }
            },
            {
                "name": "search_assets",
                "description": "Search asset paths by substring.",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "pattern": { "type": "string" },
                        "extension_filter": { "type": "string" },
                        "limit": { "type": "integer", "default": 50 }
                    },
                    "required": ["pattern"]
                }
            }
        ]
        """);

        public JObject? HandleRequest(JObject request)
        {
            var method = request["method"]?.ToString();
            var id = request["id"];

            bool isNotification = id == null && method != null &&
                (method.StartsWith("notifications/") || method == "initialized");
            if (isNotification) return null;

            return method switch
            {
                "initialize" => MakeResponse(id, new JObject
                {
                    ["protocolVersion"] = "2024-11-05",
                    ["capabilities"] = new JObject { ["tools"] = new JObject { ["listChanged"] = false } },
                    ["serverInfo"] = new JObject { ["name"] = "cue4parse-mcp", ["version"] = "2.0.0" }
                }),
                "tools/list" => MakeResponse(id, new JObject { ["tools"] = ToolDefinitions }),
                "tools/call" => HandleToolsCall(id, request["params"] as JObject),
                "ping" => MakeResponse(id, new JObject()),
                "shutdown" => MakeResponse(id, new JObject()),
                _ => MakeErrorResponse(id, -32601, $"Method not found: {method}")
            };
        }

        private JObject HandleToolsCall(JToken? id, JObject? parameters)
        {
            var toolName = parameters?["name"]?.ToString() ?? "unknown";
            var args = parameters?["arguments"] as JObject ?? new JObject();

            BridgeLog.ToolCall(toolName, args);
            var sw = Stopwatch.StartNew();

            try
            {
                var result = toolName switch
                {
                    "ping_bridge"   => $"{{\"status\":\"pong\",\"bridge\":\"CUE4Parse-MCP v2.0.0\",\"time\":\"{DateTime.Now:HH:mm:ss}\"}}",
                    "load_game"     => _provider.LoadGame(args),
                    "list_assets"   => _provider.ListAssets(args),
                    "extract_json"  => _provider.ExtractJson(args),
                    "get_skeleton"  => _provider.GetSkeleton(args),
                    "get_bounds"    => _provider.GetBounds(args),
                    "search_assets" => _provider.SearchAssets(args),
                    _ => throw new Exception($"Unknown tool: {toolName}")
                };

                sw.Stop();
                BridgeLog.ToolResult(toolName, false, Encoding.UTF8.GetByteCount(result), sw.Elapsed.TotalMilliseconds);
                return MakeResponse(id, new JObject
                {
                    ["content"] = new JArray { new JObject { ["type"] = "text", ["text"] = result } }
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                Log.Error(ex, "Tool '{Tool}' failed", toolName);
                var msg = $"{ex.GetType().Name}: {ex.Message}";
                BridgeLog.ToolResult(toolName, true, Encoding.UTF8.GetByteCount(msg), sw.Elapsed.TotalMilliseconds);
                BridgeLog.Error($"{toolName}: {msg}");
                return MakeResponse(id, new JObject
                {
                    ["content"] = new JArray { new JObject { ["type"] = "text", ["text"] = $"ERROR: {msg}\n{ex.StackTrace}" } },
                    ["isError"] = true
                });
            }
        }

        private static JObject MakeResponse(JToken? id, JObject result)
            => new() { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result };

        private static JObject MakeErrorResponse(JToken? id, int code, string message)
            => new() { ["jsonrpc"] = "2.0", ["id"] = id, ["error"] = new JObject { ["code"] = code, ["message"] = message } };
    }
}
