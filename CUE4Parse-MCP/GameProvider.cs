using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CUE4Parse_MCP;

public class GameProvider
{
    // volatile ensures the reference is visible across threads without a full lock
    private volatile DefaultFileProvider? _provider;

    // Set by Program.cs from the --mappings CLI arg passed by McpBridge at launch
    private readonly string? _defaultMappingsPath;

    public GameProvider(string? defaultMappingsPath = null)
    {
        _defaultMappingsPath = defaultMappingsPath;
    }

    public string LoadGame(JObject args)
    {
        var gameDir = args["game_dir"]?.ToString()
            ?? throw new ArgumentException("game_dir is required");
        var gameName = args["game"]?.ToString() ?? "GAME_ARKSurvivalAscended";
        var aesKey = args["aes_key"]?.ToString();
        // Explicit arg overrides default; default was passed by McpBridge from FModel's .data cache
        var mappingsPath = args["mappings_path"]?.ToString() ?? _defaultMappingsPath;

        if (!Directory.Exists(gameDir))
            throw new DirectoryNotFoundException($"Game directory not found: {gameDir}");

        // Parse game enum
        if (!Enum.TryParse<EGame>(gameName, true, out var game))
            throw new ArgumentException($"Unknown game: {gameName}. Use values from CUE4Parse.UE4.Versions.EGame");

        Log.Information("Loading game from {Dir} as {Game}", gameDir, game);

        // Initialize Oodle decompression (required for IoStore .ucas files)
        if (OodleHelper.Instance == null)
        {
            // Check common game DLL locations first (avoids a network download)
            var oodleCandidates = new[]
            {
                Path.Combine(gameDir, "ShooterGame", "Binaries", "Win64", "oo2core_9_win64.dll"),
                Path.Combine(gameDir, "Engine", "Binaries", "Win64", "oo2core_9_win64.dll"),
                Path.Combine(gameDir, "Engine", "Binaries", "ThirdParty", "Oodle", "Win64", "UnrealOodle.dll"),
                Path.Combine(AppContext.BaseDirectory, OodleHelper.OODLE_NAME_OLD),
                Path.Combine(AppContext.BaseDirectory, OodleHelper.OODLE_NAME_CURRENT),
            };

            var oodlePath = oodleCandidates.FirstOrDefault(File.Exists);
            if (oodlePath != null)
            {
                Log.Information("Using Oodle DLL: {Path}", oodlePath);
                BridgeLog.Emit("SERVER", $"Oodle: {oodlePath}");
                OodleHelper.Initialize(oodlePath);
            }
            else
            {
                // Fall back to auto-download from GitHub (OodleUE shim)
                Log.Information("Oodle DLL not found locally — downloading shim");
                BridgeLog.Emit("SERVER", "Oodle DLL not found locally, downloading shim...");
                OodleHelper.Initialize(); // downloads oodle-data-shared.dll if needed
                BridgeLog.Emit("SERVER", "Oodle initialized via download");
            }
        }

        _provider = new DefaultFileProvider(gameDir, SearchOption.AllDirectories, true,
            new VersionContainer(game));

        _provider.Initialize();
        Log.Information("Initialized: {Count} files indexed", _provider.Files.Count);

        // Load type mappings (.usmap) — required for games with unversioned properties (UE5, ARK SA, etc.)
        // Auto-detect from: explicit arg > bridge exe dir > common FModel output dirs
        var usmapCandidates = new List<string>();
        if (!string.IsNullOrEmpty(mappingsPath)) usmapCandidates.Add(mappingsPath);
        usmapCandidates.AddRange(new[]
        {
            // Next to the bridge exe (user can drop .usmap here)
            Path.Combine(AppContext.BaseDirectory, "Mappings.usmap"),
            Path.Combine(AppContext.BaseDirectory, "mappings.usmap"),
            // FModel release output
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "FModel", "bin", "Release", "net8.0-windows", "win-x64", "Output", "Mappings", "ArkSurvivalAscended.usmap"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "FModel", "bin", "Release", "net8.0-windows", "win-x64", "Output", "Mappings", "Mappings.usmap"),
            // FModel debug output
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "FModel", "bin", "Debug", "net8.0-windows", "win-x64", "Output", "Mappings", "ArkSurvivalAscended.usmap"),
        });

        var resolvedUsmap = usmapCandidates.Select(p => Path.GetFullPath(p)).FirstOrDefault(File.Exists);
        if (resolvedUsmap != null)
        {
            _provider.MappingsContainer = new FileUsmapTypeMappingsProvider(resolvedUsmap);
            Log.Information("Mappings loaded: {Path}", resolvedUsmap);
            BridgeLog.Emit("SERVER", $"Mappings: {resolvedUsmap}");
        }
        else
        {
            BridgeLog.Emit("SERVER",
                "WARNING: No .usmap mappings file found. Assets with unversioned properties will fail to deserialize. " +
                "Export mappings from FModel (Settings → Advanced → Save Mappings) and place the .usmap next to CUE4Parse-MCP.exe, " +
                "or pass mappings_path to load_game.");
        }

        // Submit AES key if provided
        if (!string.IsNullOrEmpty(aesKey))
        {
            var key = new FAesKey(aesKey);
            _provider.SubmitKey(new FGuid(), key);
            Log.Information("AES key submitted");
        }
        else
        {
            // Try with empty key (unencrypted)
            _provider.SubmitKey(new FGuid(), new FAesKey("0x0000000000000000000000000000000000000000000000000000000000000000"));
        }

        // Mount and count
        var totalFiles = _provider.Files.Count;
        var mountedVfs = _provider.MountedVfs.Count;

        return JsonConvert.SerializeObject(new
        {
            success = true,
            total_files = totalFiles,
            mounted_archives = mountedVfs,
            game = game.ToString()
        }, Formatting.Indented);
    }

    public string ListAssets(JObject args)
    {
        EnsureLoaded();

        var pathPrefix = args["path"]?.ToString() ?? "";
        var extFilter = args["extension_filter"]?.ToString();
        var limit = args["limit"]?.Value<int>() ?? 100;

        var results = _provider!.Files
            .Where(kv => kv.Key.Contains(pathPrefix, StringComparison.OrdinalIgnoreCase))
            .Where(kv => string.IsNullOrEmpty(extFilter) ||
                         kv.Key.EndsWith($".{extFilter}", StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .Select(kv => new { path = kv.Key, size = kv.Value.Size })
            .ToList();

        return JsonConvert.SerializeObject(new
        {
            count = results.Count,
            truncated = results.Count >= limit,
            assets = results
        }, Formatting.Indented);
    }

    public string ExtractJson(JObject args)
    {
        EnsureLoaded();

        var assetPath = args["asset_path"]?.ToString()
            ?? throw new ArgumentException("asset_path is required");

        var pathNoExt = StripExtension(assetPath);
        Log.Information("Extracting: {Path}", pathNoExt);

        var package = _provider!.LoadPackage(pathNoExt);
        var exports = package.GetExports().ToList();

        Log.Information("Loaded {Count} exports from {Path}", exports.Count, pathNoExt);

        // Serialize to JSON
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Error = (sender, errorArgs) =>
            {
                Log.Warning("JSON serialization error: {Error}", errorArgs.ErrorContext.Error.Message);
                errorArgs.ErrorContext.Handled = true;
            }
        };

        var result = new JArray();
        foreach (var export in exports)
        {
            try
            {
                var json = JsonConvert.SerializeObject(export, settings);
                result.Add(new JObject
                {
                    ["type"] = export.GetType().Name,
                    ["name"] = export.Name,
                    ["data"] = JToken.Parse(json)
                });
            }
            catch (Exception ex)
            {
                result.Add(new JObject
                {
                    ["type"] = export.GetType().Name,
                    ["name"] = export.Name,
                    ["error"] = $"{ex.GetType().Name}: {ex.Message}"
                });
            }
        }

        return result.ToString(Formatting.Indented);
    }

    public string GetSkeleton(JObject args)
    {
        EnsureLoaded();

        var assetPath = args["asset_path"]?.ToString()
            ?? throw new ArgumentException("asset_path is required");

        var pathNoExt = StripExtension(assetPath);
        Log.Information("Loading skeleton from: {Path}", pathNoExt);

        var package = _provider!.LoadPackage(pathNoExt);
        var exports = package.GetExports().ToList();

        // Look for USkeleton first, then USkeletalMesh
        var skeleton = exports.OfType<USkeleton>().FirstOrDefault();
        if (skeleton != null)
        {
            return ExtractSkeletonData(skeleton.ReferenceSkeleton, skeleton.Name);
        }

        var skelMesh = exports.OfType<USkeletalMesh>().FirstOrDefault();
        if (skelMesh != null)
        {
            return ExtractSkeletonData(skelMesh.ReferenceSkeleton, skelMesh.Name,
                skelMesh.ImportedBounds);
        }

        // Fallback: try to serialize whatever we find
        var types = exports.Select(e => e.GetType().Name).ToList();
        return JsonConvert.SerializeObject(new
        {
            error = "No USkeleton or USkeletalMesh found in package",
            found_types = types,
            export_count = exports.Count
        }, Formatting.Indented);
    }

    public string GetBounds(JObject args)
    {
        EnsureLoaded();

        var assetPath = args["asset_path"]?.ToString()
            ?? throw new ArgumentException("asset_path is required");

        var pathNoExt = StripExtension(assetPath);
        Log.Information("Loading bounds from: {Path}", pathNoExt);

        var package = _provider!.LoadPackage(pathNoExt);
        var exports = package.GetExports().ToList();

        var skelMesh = exports.OfType<USkeletalMesh>().FirstOrDefault();
        if (skelMesh == null)
        {
            var types = exports.Select(e => e.GetType().Name).ToList();
            return JsonConvert.SerializeObject(new
            {
                error = "No USkeletalMesh found",
                found_types = types
            }, Formatting.Indented);
        }

        var bounds = skelMesh.ImportedBounds;
        return JsonConvert.SerializeObject(new
        {
            name = skelMesh.Name,
            imported_bounds = new
            {
                origin = new { x = bounds.Origin.X, y = bounds.Origin.Y, z = bounds.Origin.Z },
                box_extent = new { x = bounds.BoxExtent.X, y = bounds.BoxExtent.Y, z = bounds.BoxExtent.Z },
                sphere_radius = bounds.SphereRadius
            }
        }, Formatting.Indented);
    }

    public string SearchAssets(JObject args)
    {
        EnsureLoaded();

        var pattern = args["pattern"]?.ToString()
            ?? throw new ArgumentException("pattern is required");
        var extFilter = args["extension_filter"]?.ToString();
        var limit = args["limit"]?.Value<int>() ?? 50;

        var results = _provider!.Files
            .Where(kv => kv.Key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .Where(kv => string.IsNullOrEmpty(extFilter) ||
                         kv.Key.EndsWith($".{extFilter}", StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .Select(kv => kv.Key)
            .ToList();

        return JsonConvert.SerializeObject(new
        {
            pattern,
            count = results.Count,
            truncated = results.Count >= limit,
            paths = results
        }, Formatting.Indented);
    }

    // ---- Helpers ----

    private void EnsureLoaded()
    {
        if (_provider == null)
            throw new InvalidOperationException("No game loaded. Call load_game first.");
    }

    private static string StripExtension(string path)
    {
        foreach (var ext in new[] { ".uasset", ".umap", ".uexp" })
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return path[..^ext.Length];
        }
        return path;
    }

    private static string ExtractSkeletonData(
        FReferenceSkeleton refSkel,
        string name,
        FBoxSphereBounds? bounds = null)
    {
        var bones = new JArray();

        if (refSkel?.FinalRefBoneInfo != null)
        {
            for (int i = 0; i < refSkel.FinalRefBoneInfo.Length; i++)
            {
                var bone = refSkel.FinalRefBoneInfo[i];
                var boneObj = new JObject
                {
                    ["index"] = i,
                    ["name"] = bone.Name.Text,
                    ["parent_index"] = bone.ParentIndex
                };

                // Add ref pose transform if available
                if (refSkel.FinalRefBonePose != null && i < refSkel.FinalRefBonePose.Length)
                {
                    var pose = refSkel.FinalRefBonePose[i];
                    boneObj["ref_position"] = new JObject
                    {
                        ["x"] = pose.Translation.X,
                        ["y"] = pose.Translation.Y,
                        ["z"] = pose.Translation.Z
                    };
                    boneObj["ref_rotation"] = new JObject
                    {
                        ["x"] = pose.Rotation.X,
                        ["y"] = pose.Rotation.Y,
                        ["z"] = pose.Rotation.Z,
                        ["w"] = pose.Rotation.W
                    };
                    boneObj["ref_scale"] = new JObject
                    {
                        ["x"] = pose.Scale3D.X,
                        ["y"] = pose.Scale3D.Y,
                        ["z"] = pose.Scale3D.Z
                    };
                }

                bones.Add(boneObj);
            }
        }

        var result = new JObject
        {
            ["name"] = name,
            ["bone_count"] = bones.Count,
            ["bones"] = bones
        };

        if (bounds != null)
        {
            result["imported_bounds"] = new JObject
            {
                ["origin"] = new JObject
                {
                    ["x"] = bounds.Origin.X,
                    ["y"] = bounds.Origin.Y,
                    ["z"] = bounds.Origin.Z
                },
                ["box_extent"] = new JObject
                {
                    ["x"] = bounds.BoxExtent.X,
                    ["y"] = bounds.BoxExtent.Y,
                    ["z"] = bounds.BoxExtent.Z
                },
                ["sphere_radius"] = bounds.SphereRadius
            };
        }

        return result.ToString(Formatting.Indented);
    }
}
