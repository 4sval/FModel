// CUE4Parse-MCP-Proxy  (persistent MCP server — Claude Desktop spawns this once and it stays alive)
//
// Architecture:
//   - Claude Desktop spawns this proxy at startup via claude_desktop_config.json "command"
//   - Proxy handles initialize / tools/list itself so tools always appear in Claude
//   - For actual tool calls (tools/call), proxy opens a named pipe to the bridge,
//     does a quick initialize handshake, sends the request, returns the response, closes pipe
//   - If bridge isn't running, tool calls return a helpful error — no restart needed
//   - When FModel starts the bridge and you say "ping bridge", the proxy connects instantly
//
// Named pipe: \\.\pipe\cue4parse-mcp  (same as CUE4Parse-MCP.exe)

using System.IO.Pipes;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

const string PipeName      = "cue4parse-mcp";
const int    BridgeTimeout = 5000; // ms to wait for bridge connection per tool call

// ── Tool definitions — declared up front so the main loop can reference them ──
var ToolList = JArray.Parse("""
[
    {
        "name": "ping_bridge",
        "description": "Test connectivity to the FModel CUE4Parse bridge. Returns pong + status if bridge is running, or an error with instructions if it is not.",
        "inputSchema": { "type": "object", "properties": {} }
    },
    {
        "name": "load_game",
        "description": "Load a game's IoStore/PAK archives. Must be called before any other tool. Game state persists across calls.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "game_dir": { "type": "string", "description": "Path to game install directory" },
                "game":     { "type": "string", "description": "Game identifier e.g. GAME_ARKSurvivalAscended", "default": "GAME_ARKSurvivalAscended" },
                "aes_key":  { "type": "string", "description": "Optional AES-256 key (0x... hex)" }
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
                "path":             { "type": "string" },
                "extension_filter": { "type": "string" },
                "limit":            { "type": "integer", "default": 100 }
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
                "asset_path":    { "type": "string" },
                "export_index":  { "type": "integer", "default": -1 }
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
                "pattern":          { "type": "string" },
                "extension_filter": { "type": "string" },
                "limit":            { "type": "integer", "default": 50 }
            },
            "required": ["pattern"]
        }
    }
]
""");

var stdin  = new StreamReader(Console.OpenStandardInput(),  Encoding.UTF8);
var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false))
{
    AutoFlush = true,
    NewLine   = "\n"
};

// ── MCP message loop ──────────────────────────────────────────────────────────
while (true)
{
    string? line;
    try { line = await stdin.ReadLineAsync(); }
    catch { break; }

    if (line == null) break;
    if (string.IsNullOrWhiteSpace(line)) continue;

    JObject req;
    try { req = JObject.Parse(line); }
    catch { continue; }

    var method = req["method"]?.ToString();
    var id     = req["id"];

    // Notifications have no id — no response
    if (id == null) continue;

    JObject resp = method switch
    {
        "initialize" => Result(id, new JObject
        {
            ["protocolVersion"] = "2024-11-05",
            ["capabilities"]    = new JObject { ["tools"] = new JObject { ["listChanged"] = false } },
            ["serverInfo"]      = new JObject { ["name"] = "cue4parse-mcp-proxy", ["version"] = "2.0.0" }
        }),

        "tools/list" => Result(id, new JObject { ["tools"] = ToolList }),

        "tools/call" => await ForwardToolCallAsync(id, req),

        "ping"     => Result(id, new JObject()),
        "shutdown" => Result(id, new JObject()),

        _ => Error(id, -32601, $"Method not found: {method}")
    };

    await stdout.WriteLineAsync(resp.ToString(Formatting.None));

    if (method == "shutdown") break;
}

// ── Bridge forwarding ─────────────────────────────────────────────────────────
// Each tool call opens a fresh pipe connection, does the MCP handshake, sends
// the request, reads the response, then closes. The bridge keeps game state in
// a shared GameProvider across connections so load_game only needs one call.

async Task<JObject> ForwardToolCallAsync(JToken? id, JObject originalReq)
{
    try
    {
        using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        try { await pipe.ConnectAsync(BridgeTimeout); }
        catch { return BridgeDownError(id); }

        var pr = new StreamReader(pipe, Encoding.UTF8);
        var pw = new StreamWriter(pipe, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };

        // ── MCP initialize handshake with bridge ──
        var initReq = new JObject
        {
            ["jsonrpc"] = "2.0", ["id"] = 0, ["method"] = "initialize",
            ["params"]  = new JObject
            {
                ["protocolVersion"] = "2024-11-05",
                ["capabilities"]    = new JObject(),
                ["clientInfo"]      = new JObject { ["name"] = "proxy", ["version"] = "2.0" }
            }
        };
        await pw.WriteLineAsync(initReq.ToString(Formatting.None));

        using var initCts = new CancellationTokenSource(BridgeTimeout);
        var initLine = await pr.ReadLineAsync(initCts.Token);
        if (initLine == null) return BridgeDownError(id);

        // Send initialized notification (bridge expects it before accepting tool calls)
        await pw.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");

        // ── Forward the actual request ──
        await pw.WriteLineAsync(originalReq.ToString(Formatting.None));

        using var respCts = new CancellationTokenSource(30_000); // 30 s for tool execution
        var respLine = await pr.ReadLineAsync(respCts.Token);
        if (respLine == null) return Error(id, -32000, "Bridge returned no response");

        var resp = JObject.Parse(respLine);
        resp["id"] = id; // fix up id to match what Claude sent
        return resp;
    }
    catch (Exception ex)
    {
        return Error(id, -32000, $"Proxy error: {ex.Message}");
    }
}

JObject BridgeDownError(JToken? id) => Result(id, new JObject
{
    ["content"] = new JArray
    {
        new JObject
        {
            ["type"] = "text",
            ["text"] = "Bridge not running — open FModel and click Views → MCP Bridge → Start Bridge"
        }
    },
    ["isError"] = true
});

JObject Result(JToken? id, JObject result) =>
    new() { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result };

JObject Error(JToken? id, int code, string message) =>
    new() { ["jsonrpc"] = "2.0", ["id"] = id, ["error"] = new JObject { ["code"] = code, ["message"] = message } };

