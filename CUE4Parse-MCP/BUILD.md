# CUE4Parse MCP Server - Build & Setup

## Build

Open a terminal in the `CUE4Parse-MCP` directory and run:

```
dotnet build -c Release
```

The built executable will be at:
```
bin\Release\net8.0\CUE4Parse-MCP.exe
```

## Register as MCP Server

Add this to your Claude desktop config at `%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "cue4parse": {
      "command": "<path-to-repo>\\CUE4Parse-MCP\\bin\\Release\\net8.0\\CUE4Parse-MCP.exe",
      "args": []
    }
  }
}
```

Then restart Claude desktop.

## Available Tools

- **load_game** - Load ARK SA archives (call this first)
- **list_assets** - Browse virtual file tree
- **search_assets** - Search assets by name pattern
- **extract_json** - Full JSON export of any asset
- **get_skeleton** - Extract bone hierarchy from skeleton/mesh assets
- **get_bounds** - Extract mesh bounding box data

## Example Usage (from Claude)

1. Load the game:
   ```
   load_game(game_dir: "C:/path/to/ARK Survival Ascended", game: "GAME_ARKSurvivalAscended")
   ```

2. Search for a Rex skeleton:
   ```
   search_assets(pattern: "Rex_Skeleton", extension_filter: "uasset")
   ```

3. Extract the skeleton:
   ```
   get_skeleton(asset_path: "ShooterGame/Content/ASA/Dinos/Rex/Rex_Skeleton.uasset")
   ```

## Logs

Logs are written to `cue4parse-mcp.log` in the exe's directory. Check here for errors.
