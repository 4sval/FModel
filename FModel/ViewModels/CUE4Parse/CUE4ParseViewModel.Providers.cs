using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdonisUI.Controls;
using CUE4Parse.Compression;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using EpicManifestParser;
using EpicManifestParser.UE;
using EpicManifestParser.ZlibngDotNetDecompressor;
using FModel.Settings;
using FModel.Views.Resources.Controls;
using Serilog;

namespace FModel.ViewModels.CUE4Parse;

public partial class CUE4ParseViewModel
{
    public async Task Initialize()
    {
        await _threadWorkerView.Begin(cancellationToken =>
        {
            foreach (var (provider, dir) in ProvidersWithDirectories())
            {
                InitializeProvider(provider, dir, cancellationToken);
            }

            ForEachProvider(provider =>
            {
                Log.Information($"{provider.Versions.Game} ({provider.Versions.Platform}) | Archives: x{provider.UnloadedVfs.Count} | AES: x{provider.RequiredKeys.Count} | Loose Files: x{provider.Files.Count}");
            });
        });
    }

    private void InitializeProvider(AbstractVfsFileProvider provider, DirectorySettings dir, CancellationToken cancellationToken)
    {
        switch (provider)
        {
            case StreamedFileProvider p:
                switch (p.LiveGame)
                {
                    case "FortniteLive":
                        {
                            var manifestInfo = _apiEndpointView.EpicApi.GetManifest(cancellationToken);
                            if (manifestInfo is null)
                            {
                                throw new FileLoadException("Could not load latest Fortnite manifest, you may have to switch to your local installation.");
                            }

                            var cacheDir = Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, ".data")).FullName;
                            var manifestOptions = new ManifestParseOptions
                            {
                                ChunkCacheDirectory = cacheDir,
                                ManifestCacheDirectory = cacheDir,
                                ChunkBaseUrl = "http://download.epicgames.com/Builds/Fortnite/CloudDir/",
                                Decompressor = ManifestZlibngDotNetDecompressor.Decompress,
                                DecompressorState = ZlibHelper.Instance,
                                CacheChunksAsIs = false
                            };

                            var startTs = Stopwatch.GetTimestamp();
                            FBuildPatchAppManifest manifest;

                            try
                            {
                                (manifest, _) = manifestInfo.DownloadAndParseAsync(manifestOptions,
                                    cancellationToken: cancellationToken,
                                    elementManifestPredicate: static x => x.Uri.Host == "download.epicgames.com"
                                ).GetAwaiter().GetResult();
                            }
                            catch (HttpRequestException ex)
                            {
                                Log.Error("Failed to download manifest ({ManifestUri})", ex.Data["ManifestUri"]?.ToString() ?? "");
                                throw;
                            }

                            if (manifest.TryFindFile("Cloud/IoStoreOnDemand.ini", out var ioStoreOnDemandFile))
                            {
                                IoStoreOnDemand.Read(new StreamReader(ioStoreOnDemandFile.GetStream()));
                            }

                            Parallel.ForEach(manifest.Files.Where(x => _fnLiveRegex.IsMatch(x.FileName)), fileManifest =>
                            {
                                p.RegisterVfs(fileManifest.FileName, [fileManifest.GetStream()],
                                    it => new FRandomAccessStreamArchive(it, manifest.FindFile(it)!.GetStream(), p.Versions));
                            });

                            var elapsedTime = Stopwatch.GetElapsedTime(startTs);
                            FLogger.Append(ELog.Information, () =>
                                FLogger.Text($"Fortnite [LIVE] has been loaded successfully in {elapsedTime.TotalMilliseconds:F1}ms", Constants.WHITE, true));
                            break;
                        }
                    case "ValorantLive":
                        {
                            var manifest = _apiEndpointView.ValorantApi.GetManifest(cancellationToken);
                            if (manifest == null)
                            {
                                throw new Exception("Could not load latest Valorant manifest, you may have to switch to your local installation.");
                            }

                            Parallel.ForEach(manifest.Paks, pak =>
                            {
                                p.RegisterVfs(pak.GetFullName(), [pak.GetStream(manifest)]);
                            });

                            FLogger.Append(ELog.Information, () =>
                                FLogger.Text($"Valorant '{manifest.Header.GameVersion}' has been loaded successfully", Constants.WHITE, true));
                            break;
                        }
                }
                break;
            case DefaultFileProvider:
            {
                var ioStoreOnDemandPath = Path.Combine(dir.GameDirectory, "..\\..\\..\\Cloud\\IoStoreOnDemand.ini");
                if (File.Exists(ioStoreOnDemandPath))
                {
                    using var s = new StreamReader(ioStoreOnDemandPath);
                    IoStoreOnDemand.Read(s);
                }
                break;
            }
        }
        provider.Initialize();
    }

    private static AbstractVfsFileProvider CreateProvider(DirectorySettings dir, Regex fnLiveRegex)
    {
        var gameDirectory = dir.GameDirectory;
        var versionContainer = new VersionContainer(
            game: dir.UeVersion,
            platform: dir.TexturePlatform,
            customVersions: new FCustomVersionContainer(dir.Versioning.CustomVersions),
            optionOverrides: dir.Versioning.Options,
            mapStructTypesOverrides: dir.Versioning.MapStructTypes
        );
        var pathComparer = StringComparer.OrdinalIgnoreCase;

        switch (gameDirectory)
        {
            case Constants._FN_LIVE_TRIGGER:
                return new StreamedFileProvider("FortniteLive", versionContainer, pathComparer);
            case Constants._VAL_LIVE_TRIGGER:
                return new StreamedFileProvider("ValorantLive", versionContainer, pathComparer);
            default:
                {
                    var project = gameDirectory.SubstringBeforeLast(gameDirectory.Contains("eFootball") ? "\\pak" : "\\Content").SubstringAfterLast("\\");
                    return project switch
                    {
                        "StateOfDecay2" => new DefaultFileProvider(new DirectoryInfo(gameDirectory),
                            [
                                new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\StateOfDecay2\\Saved\\Paks"),
                                new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\StateOfDecay2\\Saved\\DisabledPaks")
                            ], SearchOption.AllDirectories, versionContainer, pathComparer),
                        "eFootball" => new DefaultFileProvider(new DirectoryInfo(gameDirectory),
                            [
                                new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\KONAMI\\eFootball\\ST\\Download")
                            ], SearchOption.AllDirectories, versionContainer, pathComparer),
                        _ => new DefaultFileProvider(gameDirectory, SearchOption.AllDirectories, versionContainer, pathComparer)
                    };
                }
        }
    }

    public void RefreshReadSettings()
    {
        Provider.ReadScriptData = UserSettings.Default.ReadScriptData;
        Provider.ReadShaderMaps = UserSettings.Default.ReadShaderMaps;
        Provider.ReadNaniteData = true;

        if (UserSettings.Default.DiffDir == null || DiffProvider == null) return;

        DiffProvider.ReadScriptData = UserSettings.Default.ReadScriptData;
        DiffProvider.ReadShaderMaps = UserSettings.Default.ReadShaderMaps;
        DiffProvider.ReadNaniteData = true;
    }

    public IEnumerable<AbstractVfsFileProvider> AllProviders()
    {
        yield return Provider;
        if (DiffProvider != null)
            yield return DiffProvider;
    }

    public void ForEachProvider(Action<AbstractVfsFileProvider> action)
    {
        foreach (var provider in AllProviders())
            action(provider);
    }

    private IEnumerable<(AbstractVfsFileProvider Provider, EndpointSettings Endpoint)> ProvidersWithEndpoints(EEndpointType type)
    {
        if (UserSettings.IsEndpointValid(UserSettings.Default.CurrentDir, type, out var mainEndpoint))
            yield return (Provider, mainEndpoint);

        if (DiffProvider != null && UserSettings.Default.DiffDir != null
                                 && UserSettings.IsEndpointValid(UserSettings.Default.DiffDir, type, out var diffEndpoint))
        {
            yield return (DiffProvider, diffEndpoint);
        }
    }

    public IEnumerable<(AbstractVfsFileProvider Provider, DirectorySettings Dir)> ProvidersWithDirectories()
    {
        yield return (Provider, UserSettings.Default.CurrentDir);
        if (DiffProvider != null && UserSettings.Default.DiffDir != null)
            yield return (DiffProvider, UserSettings.Default.DiffDir);
    }

    public void ClearProvider()
    {
        AssetsFolder.Folders.Clear();
        SearchVm.SearchResults.Clear();
        Helper.CloseWindow<AdonisWindow>("Search View");

        ForEachProvider(provider => provider.UnloadNonStreamedVfs());

        GC.Collect();
    }
}
