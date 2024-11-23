using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using AdonisUI.Controls;

using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.GameTypes.ApexMobile.Encryption.Aes;
using CUE4Parse.GameTypes.DBD.Encryption.Aes;
using CUE4Parse.GameTypes.DeltaForce.Encryption.Aes;
using CUE4Parse.GameTypes.DreamStar.Encryption.Aes;
using CUE4Parse.GameTypes.FSR.Encryption.Aes;
using CUE4Parse.GameTypes.FunkoFusion.Encryption.Aes;
using CUE4Parse.GameTypes.KRD.Assets.Exports;
using CUE4Parse.GameTypes.MJS.Encryption.Aes;
using CUE4Parse.GameTypes.NetEase.MAR.Encryption.Aes;
using CUE4Parse.GameTypes.PAXDEI.Encryption.Aes;
using CUE4Parse.GameTypes.Rennsport.Encryption.Aes;
using CUE4Parse.GameTypes.Snowbreak.Encryption.Aes;
using CUE4Parse.GameTypes.UDWN.Encryption.Aes;
using CUE4Parse.GameTypes.THPS.Encryption.Aes;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Verse;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Oodle.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Shaders;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Wwise;

using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.UE4.Assets;
using EpicManifestParser;
using EpicManifestParser.ZlibngDotNetDecompressor;
using FModel.Creator;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.Views;
using FModel.Views.Resources.Controls;
using FModel.Views.Snooper;

using Newtonsoft.Json;
using OffiUtils;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

using Serilog;

using SkiaSharp;

using UE4Config.Parsing;

using Application = System.Windows.Application;

namespace FModel.ViewModels;

public class CUE4ParseViewModel : ViewModel
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;
    private readonly Regex _fnLive = new(@"^FortniteGame[/\\]Content[/\\]Paks[/\\]",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private string _internalGameName;
    public string InternalGameName
    {
        get => _internalGameName;
        set => SetProperty(ref _internalGameName, value);
    }

    private bool _modelIsOverwritingMaterial;
    public bool ModelIsOverwritingMaterial
    {
        get => _modelIsOverwritingMaterial;
        set => SetProperty(ref _modelIsOverwritingMaterial, value);
    }

    private bool _modelIsWaitingAnimation;
    public bool ModelIsWaitingAnimation
    {
        get => _modelIsWaitingAnimation;
        set => SetProperty(ref _modelIsWaitingAnimation, value);
    }

    public bool IsSnooperOpen => _snooper is { Exists: true, IsVisible: true };
    private Snooper _snooper;
    public Snooper SnooperViewer
    {
        get
        {
            if (_snooper != null) return _snooper;

            return Application.Current.Dispatcher.Invoke(delegate
            {
                var scale = ImGuiController.GetDpiScale();
                var htz = Snooper.GetMaxRefreshFrequency();
                return _snooper = new Snooper(
                    new GameWindowSettings { UpdateFrequency = htz },
                    new NativeWindowSettings
                    {
                        ClientSize = new OpenTK.Mathematics.Vector2i(
                            Convert.ToInt32(SystemParameters.MaximizedPrimaryScreenWidth * .75 * scale),
                            Convert.ToInt32(SystemParameters.MaximizedPrimaryScreenHeight * .85 * scale)),
                        NumberOfSamples = Constants.SAMPLES_COUNT,
                        WindowBorder = WindowBorder.Resizable,
                        Flags = ContextFlags.ForwardCompatible,
                        Profile = ContextProfile.Core,
                        Vsync = VSyncMode.Adaptive,
                        APIVersion = new Version(4, 6),
                        StartVisible = false,
                        StartFocused = false,
                        Title = "3D Viewer"
                    });
            });
        }
    }

    public AbstractVfsFileProvider Provider { get; }
    public GameDirectoryViewModel GameDirectory { get; }
    public AssetsFolderViewModel AssetsFolder { get; }
    public SearchViewModel SearchVm { get; }
    public TabControlViewModel TabControl { get; }
    public ConfigIni IoStoreOnDemand { get; }

    public CUE4ParseViewModel()
    {
        var currentDir = UserSettings.Default.CurrentDir;
        var gameDirectory = currentDir.GameDirectory;
        var versionContainer = new VersionContainer(
            game: currentDir.UeVersion, platform: currentDir.TexturePlatform,
            customVersions: new FCustomVersionContainer(currentDir.Versioning.CustomVersions),
            optionOverrides: currentDir.Versioning.Options,
            mapStructTypesOverrides: currentDir.Versioning.MapStructTypes);

        switch (gameDirectory)
        {
            case Constants._FN_LIVE_TRIGGER:
            {
                InternalGameName = "FortniteGame";
                Provider = new StreamedFileProvider("FortniteLive", true, versionContainer);
                break;
            }
            case Constants._VAL_LIVE_TRIGGER:
            {
                InternalGameName = "ShooterGame";
                Provider = new StreamedFileProvider("ValorantLive", true, versionContainer);
                break;
            }
            default:
            {
                InternalGameName = gameDirectory.SubstringBeforeLast(gameDirectory.Contains("eFootball") ? "\\pak" : "\\Content").SubstringAfterLast("\\");
                Provider = InternalGameName switch
                {
                    "StateOfDecay2" => new DefaultFileProvider(new DirectoryInfo(gameDirectory),
                    [
                        new(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\StateOfDecay2\\Saved\\Paks"),
                        new(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\StateOfDecay2\\Saved\\DisabledPaks")
                    ], SearchOption.AllDirectories, true, versionContainer),
                    "eFootball" => new DefaultFileProvider(new DirectoryInfo(gameDirectory),
                    [
                        new(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\KONAMI\\eFootball\\ST\\Download")
                    ], SearchOption.AllDirectories, true, versionContainer),
                    _ => new DefaultFileProvider(gameDirectory, SearchOption.AllDirectories, true, versionContainer)
                };

                break;
            }
        }
        Provider.ReadScriptData = UserSettings.Default.ReadScriptData;
        Provider.CustomEncryption = Provider.Versions.Game switch
        {
            EGame.GAME_ApexLegendsMobile => ApexLegendsMobileAes.DecryptApexMobile,
            EGame.GAME_Snowbreak => SnowbreakAes.SnowbreakDecrypt,
            EGame.GAME_MarvelRivals => MarvelAes.MarvelDecrypt,
            EGame.GAME_Undawn => ToaaAes.ToaaDecrypt,
            EGame.GAME_DeadByDaylight => DBDAes.DbDDecrypt,
            EGame.GAME_PaxDei => PaxDeiAes.PaxDeiDecrypt,
            EGame.GAME_3on3FreeStyleRebound => FreeStyleReboundAes.FSRDecrypt,
            EGame.GAME_DreamStar => DreamStarAes.DreamStarDecrypt,
            EGame.GAME_DeltaForceHawkOps => DeltaForceAes.DeltaForceDecrypt,
            EGame.GAME_MonsterJamShowdown => MonsterJamShowdownAes.MonsterJamShowdownDecrypt,
            EGame.GAME_Rennsport => RennsportAes.RennsportDecrypt,
            EGame.GAME_FunkoFusion => FunkoFusionAes.FunkoFusionDecrypt,
            EGame.GAME_TonyHawkProSkater12 => THPS12Aes.THPS12Decrypt,
            _ => Provider.CustomEncryption
        };

        GameDirectory = new GameDirectoryViewModel();
        AssetsFolder = new AssetsFolderViewModel();
        SearchVm = new SearchViewModel();
        TabControl = new TabControlViewModel();
        IoStoreOnDemand = new ConfigIni(nameof(IoStoreOnDemand));
    }

    public async Task Initialize()
    {
        await _threadWorkerView.Begin(cancellationToken =>
        {
            switch (Provider)
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
                                ChunkBaseUrl = "http://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/",
                                Decompressor = ManifestZlibngDotNetDecompressor.Decompress,
                                DecompressorState = ZlibHelper.Instance,
                                CacheChunksAsIs = false
                            };

                            var startTs = Stopwatch.GetTimestamp();
                            var (manifest, _) = manifestInfo.DownloadAndParseAsync(manifestOptions,
                                cancellationToken: cancellationToken,
                                elementManifestPredicate: x => x.Uri.Host is ("epicgames-download1.akamaized.net" or "download.epicgames.com")
                                ).GetAwaiter().GetResult();
                            var parseTime = Stopwatch.GetElapsedTime(startTs);

                            foreach (var fileManifest in manifest.Files)
                            {
                                if (fileManifest.FileName.Equals("Cloud/IoStoreOnDemand.ini", StringComparison.OrdinalIgnoreCase))
                                {
                                    IoStoreOnDemand.Read(new StreamReader(fileManifest.GetStream()));
                                    continue;
                                }

                                if (!_fnLive.IsMatch(fileManifest.FileName))
                                {
                                    continue;
                                }

                                p.RegisterVfs(fileManifest.FileName, [fileManifest.GetStream()]
                                    , it => new FRandomAccessStreamArchive(it, manifest.Files.First(x => x.FileName.Equals(it)).GetStream(), p.Versions));
                            }

                            FLogger.Append(ELog.Information, () =>
                                FLogger.Text($"Fortnite [LIVE] has been loaded successfully in {parseTime.TotalMilliseconds}ms", Constants.WHITE, true));
                            break;
                        }
                        case "ValorantLive":
                        {
                            var manifestInfo = _apiEndpointView.ValorantApi.GetManifest(cancellationToken);
                            if (manifestInfo == null)
                            {
                                throw new Exception("Could not load latest Valorant manifest, you may have to switch to your local installation.");
                            }

                            for (var i = 0; i < manifestInfo.Paks.Length; i++)
                            {
                                p.RegisterVfs(manifestInfo.Paks[i].GetFullName(), [manifestInfo.GetPakStream(i)]);
                            }

                            FLogger.Append(ELog.Information, () =>
                                FLogger.Text($"Valorant '{manifestInfo.Header.GameVersion}' has been loaded successfully", Constants.WHITE, true));
                            break;
                        }
                    }

                    break;
                case DefaultFileProvider:
                {
                    var ioStoreOnDemandPath = Path.Combine(UserSettings.Default.GameDirectory, "..\\..\\..\\Cloud\\IoStoreOnDemand.ini");
                    if (File.Exists(ioStoreOnDemandPath))
                    {
                        using var s = new StreamReader(ioStoreOnDemandPath);
                        IoStoreOnDemand.Read(s);
                    }
                    break;
                }
            }

            Provider.Initialize();
            Log.Information($"{Provider.Versions.Game} ({Provider.Versions.Platform}) | Archives: x{Provider.UnloadedVfs.Count} | AES: x{Provider.RequiredKeys.Count}");
        });
    }

    /// <summary>
    /// load virtual files system from GameDirectory
    /// </summary>
    /// <returns></returns>
    public void LoadVfs(IEnumerable<KeyValuePair<FGuid, FAesKey>> aesKeys)
    {
        Provider.SubmitKeys(aesKeys);
        Provider.PostMount();
        InternalGameName = Provider.InternalGameName;

        var aesMax = Provider.RequiredKeys.Count + Provider.Keys.Count;
        var archiveMax = Provider.UnloadedVfs.Count + Provider.MountedVfs.Count;
        Log.Information($"Project: {InternalGameName} | Mounted: {Provider.MountedVfs.Count}/{archiveMax} | AES: {Provider.Keys.Count}/{aesMax}");
    }

    public void ClearProvider()
    {
        if (Provider == null) return;

        AssetsFolder.Folders.Clear();
        SearchVm.SearchResults.Clear();
        Helper.CloseWindow<AdonisWindow>("Search View");
        Provider.UnloadNonStreamedVfs();
        GC.Collect();
    }

    public async Task RefreshAes()
    {
        // game directory dependent, we don't have the provider game name yet since we don't have aes keys
        // except when this comes from the AES Manager
        if (!UserSettings.IsEndpointValid(EEndpointType.Aes, out var endpoint))
            return;

        await _threadWorkerView.Begin(cancellationToken =>
        {
            var aes = _apiEndpointView.DynamicApi.GetAesKeys(cancellationToken, endpoint.Url, endpoint.Path);
            if (aes is not { IsValid: true }) return;

            UserSettings.Default.CurrentDir.AesKeys = aes;
        });
    }

    public async Task InitInformation()
    {
        await _threadWorkerView.Begin(cancellationToken =>
        {
            var info = _apiEndpointView.FModelApi.GetNews(cancellationToken, Provider.InternalGameName);
            if (info == null) return;

            FLogger.Append(ELog.None, () =>
            {
                for (var i = 0; i < info.Messages.Length; i++)
                {
                    FLogger.Text(info.Messages[i], info.Colors[i], bool.Parse(info.NewLines[i]));
                }
            });
        });
    }

    public Task InitMappings(bool force = false)
    {
        if (!UserSettings.IsEndpointValid(EEndpointType.Mapping, out var endpoint))
        {
            Provider.MappingsContainer = null;
            return Task.CompletedTask;
        }

        return Task.Run(() =>
        {
            var l = ELog.Information;
            if (endpoint.Overwrite && File.Exists(endpoint.FilePath))
            {
                Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(endpoint.FilePath);
            }
            else if (endpoint.IsValid)
            {
                var mappingsFolder = Path.Combine(UserSettings.Default.OutputDirectory, ".data");
                if (endpoint.Path == "$.[?(@.meta.compressionMethod=='Oodle')].['url','fileName']") endpoint.Path = "$.[0].['url','fileName']";
                var mappings = _apiEndpointView.DynamicApi.GetMappings(default, endpoint.Url, endpoint.Path);
                if (mappings is { Length: > 0 })
                {
                    foreach (var mapping in mappings)
                    {
                        if (!mapping.IsValid) continue;

                        var mappingPath = Path.Combine(mappingsFolder, mapping.FileName);
                        if (force || !File.Exists(mappingPath))
                        {
                            _apiEndpointView.DownloadFile(mapping.Url, mappingPath);
                        }

                        Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingPath);
                        break;
                    }
                }

                if (Provider.MappingsContainer == null)
                {
                    var latestUsmaps = new DirectoryInfo(mappingsFolder).GetFiles("*_oo.usmap");
                    if (latestUsmaps.Length <= 0) return;

                    var latestUsmapInfo = latestUsmaps.OrderBy(f => f.LastWriteTime).Last();
                    Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(latestUsmapInfo.FullName);
                    l = ELog.Warning;
                }
            }

            if (Provider.MappingsContainer is FileUsmapTypeMappingsProvider m)
            {
                Log.Information($"Mappings pulled from '{m.FileName}'");
                FLogger.Append(l, () => FLogger.Text($"Mappings pulled from '{m.FileName}'", Constants.WHITE, true));
            }
        });
    }

    public Task VerifyConsoleVariables()
    {
        if (Provider.Versions["StripAdditiveRefPose"])
        {
            FLogger.Append(ELog.Warning, () =>
                FLogger.Text("Additive animations have their reference pose stripped, which will lead to inaccurate preview and export", Constants.WHITE, true));
        }

        return Task.CompletedTask;
    }

    public Task VerifyOnDemandArchives()
    {
        // only local fortnite
        if (Provider is not DefaultFileProvider || !Provider.InternalGameName.Equals("FortniteGame", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        // scuffed but working
        var persistentDownloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteGame/Saved/PersistentDownloadDir");
        var iasFileInfo = new FileInfo(Path.Combine(persistentDownloadDir, "ias", "ias.cache.0"));
        if (!iasFileInfo.Exists || iasFileInfo.Length == 0)
            return Task.CompletedTask;

        return Task.Run(async () =>
        {
            var inst = new List<InstructionToken>();
            IoStoreOnDemand.FindPropertyInstructions("Endpoint", "TocPath", inst);
            if (inst.Count <= 0) return;

            var ioStoreOnDemandPath = Path.Combine(UserSettings.Default.GameDirectory, "..\\..\\..\\Cloud", inst[0].Value.SubstringAfterLast("/").SubstringBefore("\""));
            if (!File.Exists(ioStoreOnDemandPath)) return;

            await _apiEndpointView.EpicApi.VerifyAuth(default);
            await Provider.RegisterVfs(new IoChunkToc(ioStoreOnDemandPath), new IoStoreOnDemandOptions
            {
                ChunkBaseUri = new Uri("https://download.epicgames.com/ias/fortnite/", UriKind.Absolute),
                ChunkCacheDirectory = Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, ".data")),
                Authorization = new AuthenticationHeaderValue("Bearer", UserSettings.Default.LastAuthResponse.AccessToken),
                Timeout = TimeSpan.FromSeconds(30)
            });
            var onDemandCount = await Provider.MountAsync();
            FLogger.Append(ELog.Information, () =>
                FLogger.Text($"{onDemandCount} on-demand archive{(onDemandCount > 1 ? "s" : "")} streamed via epicgames.com", Constants.WHITE, true));
        });
    }

    public int LocalizedResourcesCount { get; set; }
    public bool LocalResourcesDone { get; set; }
    public bool HotfixedResourcesDone { get; set; }
    public async Task LoadLocalizedResources()
    {
        var snapshot = LocalizedResourcesCount;
        await Task.WhenAll(LoadGameLocalizedResources(), LoadHotfixedLocalizedResources()).ConfigureAwait(false);
        if (snapshot != LocalizedResourcesCount)
        {
            FLogger.Append(ELog.Information, () =>
                FLogger.Text($"{LocalizedResourcesCount} localized resources loaded for '{UserSettings.Default.AssetLanguage.GetDescription()}'", Constants.WHITE, true));
            Utils.Typefaces = new Typefaces(this);
        }
    }
    private Task LoadGameLocalizedResources()
    {
        if (LocalResourcesDone) return Task.CompletedTask;
        return Task.Run(() =>
        {
            LocalizedResourcesCount += Provider.LoadLocalization(UserSettings.Default.AssetLanguage);
            LocalResourcesDone = true;
        });
    }
    private Task LoadHotfixedLocalizedResources()
    {
        if (!Provider.InternalGameName.Equals("fortnitegame", StringComparison.OrdinalIgnoreCase) || HotfixedResourcesDone) return Task.CompletedTask;
        return Task.Run(() =>
        {
            var hotfixes = ApplicationService.ApiEndpointView.CentralApi.GetHotfixes(default, Provider.GetLanguageCode(UserSettings.Default.AssetLanguage));
            if (hotfixes == null) return;

            HotfixedResourcesDone = true;
            foreach (var entries in hotfixes)
            {
                if (!Provider.LocalizedResources.ContainsKey(entries.Key))
                    Provider.LocalizedResources[entries.Key] = new Dictionary<string, string>();

                foreach (var keyValue in entries.Value)
                {
                    Provider.LocalizedResources[entries.Key][keyValue.Key] = keyValue.Value;
                    LocalizedResourcesCount++;
                }
            }
        });
    }

    private int _virtualPathCount { get; set; }
    public Task LoadVirtualPaths()
    {
        if (_virtualPathCount > 0) return Task.CompletedTask;
        return Task.Run(() =>
        {
            _virtualPathCount = Provider.LoadVirtualPaths(UserSettings.Default.CurrentDir.UeVersion.GetVersion());
            if (_virtualPathCount > 0)
            {
                FLogger.Append(ELog.Information, () =>
                    FLogger.Text($"{_virtualPathCount} virtual paths loaded", Constants.WHITE, true));
            }
            else
            {
                FLogger.Append(ELog.Warning, () =>
                    FLogger.Text("Could not load virtual paths, plugin manifest may not exist", Constants.WHITE, true));
            }
        });
    }

    public void ExtractSelected(CancellationToken cancellationToken, IEnumerable<AssetItem> assetItems)
    {
        foreach (var asset in assetItems)
        {
            Thread.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            Extract(cancellationToken, asset.FullPath, TabControl.HasNoTabs);
        }
    }

    private void BulkFolder(CancellationToken cancellationToken, TreeItem folder, Action<AssetItem> action)
    {
        foreach (var asset in folder.AssetsList.Assets)
        {
            Thread.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                action(asset);
            }
            catch
            {
                // ignore
            }
        }

        foreach (var f in folder.Folders) BulkFolder(cancellationToken, f, action);
    }

    public void ExportFolder(CancellationToken cancellationToken, TreeItem folder)
    {
        Parallel.ForEach(folder.AssetsList.Assets, asset =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExportData(asset.FullPath, false);
        });

        foreach (var f in folder.Folders) ExportFolder(cancellationToken, f);
    }

    public void ExtractFolder(CancellationToken cancellationToken, TreeItem folder)
        => BulkFolder(cancellationToken, folder, asset => Extract(cancellationToken, asset.FullPath, TabControl.HasNoTabs));

    public void SaveFolder(CancellationToken cancellationToken, TreeItem folder)
        => BulkFolder(cancellationToken, folder, asset => Extract(cancellationToken, asset.FullPath, TabControl.HasNoTabs, EBulkType.Properties | EBulkType.Auto));

    public void TextureFolder(CancellationToken cancellationToken, TreeItem folder)
        => BulkFolder(cancellationToken, folder, asset => Extract(cancellationToken, asset.FullPath, TabControl.HasNoTabs, EBulkType.Textures | EBulkType.Auto));

    public void ModelFolder(CancellationToken cancellationToken, TreeItem folder)
        => BulkFolder(cancellationToken, folder, asset => Extract(cancellationToken, asset.FullPath, TabControl.HasNoTabs, EBulkType.Meshes | EBulkType.Auto));

    public void AnimationFolder(CancellationToken cancellationToken, TreeItem folder)
        => BulkFolder(cancellationToken, folder, asset => Extract(cancellationToken, asset.FullPath, TabControl.HasNoTabs, EBulkType.Animations | EBulkType.Auto));

    public void Extract(CancellationToken cancellationToken, string fullPath, bool addNewTab = false, EBulkType bulk = EBulkType.None)
    {
        Log.Information("User DOUBLE-CLICKED to extract '{FullPath}'", fullPath);

        var directory = fullPath.SubstringBeforeLast('/');
        var fileName = fullPath.SubstringAfterLast('/');
        var ext = fullPath.SubstringAfterLast('.').ToLower();

        if (addNewTab && TabControl.CanAddTabs) TabControl.AddTab(fileName, directory);
        else TabControl.SelectedTab.SoftReset(fileName, directory);
        TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector(ext);

        var updateUi = !HasFlag(bulk, EBulkType.Auto);
        var saveProperties = HasFlag(bulk, EBulkType.Properties);
        var saveTextures = HasFlag(bulk, EBulkType.Textures);
        switch (ext)
        {
            case "uasset":
            case "umap":
            {
                var exports = Provider.LoadAllObjects(fullPath);
                TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(exports, Formatting.Indented), saveProperties, updateUi);
                if (HasFlag(bulk, EBulkType.Properties)) break; // do not search for viewable exports if we are dealing with jsons

                foreach (var e in exports)
                {
                    if (CheckExport(cancellationToken, e, bulk))
                        break;
                }

                break;
            }
            case "upluginmanifest":
            case "uproject":
            case "manifest":
            case "uplugin":
            case "archive":
            case "vmodule":
            case "verse":
            case "html":
            case "json":
            case "ini":
            case "txt":
            case "log":
            case "bat":
            case "dat":
            case "cfg":
            case "ide":
            case "ipl":
            case "zon":
            case "xml":
            case "css":
            case "csv":
            case "pem":
            case "tps":
            case "lua":
            case "js":
            case "po":
            case "h":
            {
                if (Provider.TrySaveAsset(fullPath, out var data))
                {
                    using var stream = new MemoryStream(data) { Position = 0 };
                    using var reader = new StreamReader(stream);

                    TabControl.SelectedTab.SetDocumentText(reader.ReadToEnd(), saveProperties, updateUi);
                }

                break;
            }
            case "locmeta":
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var metadata = new FTextLocalizationMetaDataResource(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(metadata, Formatting.Indented), saveProperties, updateUi);
                }

                break;
            }
            case "locres":
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var locres = new FTextLocalizationResource(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(locres, Formatting.Indented), saveProperties, updateUi);
                }

                break;
            }
            case "bin" when fileName.Contains("AssetRegistry", StringComparison.OrdinalIgnoreCase):
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var registry = new FAssetRegistryState(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(registry, Formatting.Indented), saveProperties, updateUi);
                }

                break;
            }
            case "bin" when fileName.Contains("GlobalShaderCache", StringComparison.OrdinalIgnoreCase):
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var registry = new FGlobalShaderCache(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(registry, Formatting.Indented), saveProperties, updateUi);
                }

                break;
            }
            case "bnk":
            case "pck":
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var wwise = new WwiseReader(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(wwise, Formatting.Indented), saveProperties, updateUi);
                    foreach (var (name, data) in wwise.WwiseEncodedMedias)
                    {
                        SaveAndPlaySound(fullPath.SubstringBeforeWithLast("/") + name, "WEM", data);
                    }
                }

                break;
            }
            case "wem":
            {
                if (Provider.TrySaveAsset(fullPath, out var input))
                    SaveAndPlaySound(fullPath, "WEM", input);

                break;
            }
            case "udic":
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var header = new FOodleDictionaryArchive(archive).Header;
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(header, Formatting.Indented), saveProperties, updateUi);
                }

                break;
            }
            case "png":
            case "jpg":
            case "bmp":
            {
                if (Provider.TrySaveAsset(fullPath, out var data))
                {
                    using var stream = new MemoryStream(data) { Position = 0 };
                    TabControl.SelectedTab.AddImage(fileName.SubstringBeforeLast("."), false, SKBitmap.Decode(stream), saveTextures, updateUi);
                }

                break;
            }
            case "svg":
            {
                if (Provider.TrySaveAsset(fullPath, out var data))
                {
                    using var stream = new MemoryStream(data) { Position = 0 };
                    var svg = new SkiaSharp.Extended.Svg.SKSvg(new SKSize(512, 512));
                    svg.Load(stream);

                    var bitmap = new SKBitmap(512, 512);
                    using (var canvas = new SKCanvas(bitmap))
                    using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium })
                    {
                        canvas.DrawPicture(svg.Picture, paint);
                    }

                    TabControl.SelectedTab.AddImage(fileName.SubstringBeforeLast("."), false, bitmap, saveTextures, updateUi);
                }

                break;
            }
            case "ufont":
            case "otf":
            case "ttf":
                FLogger.Append(ELog.Warning, () =>
                    FLogger.Text($"Export '{fileName}' raw data and change its extension if you want it to be an installable font file", Constants.WHITE, true));
                break;
            case "ushaderbytecode":
            case "ushadercode":
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var ar = new FShaderCodeArchive(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(ar, Formatting.Indented), saveProperties, updateUi);
                }

                break;
            }
            default:
            {
                FLogger.Append(ELog.Warning, () =>
                    FLogger.Text($"The package '{fileName}' is of an unknown type.", Constants.WHITE, true));
                break;
            }
        }
    }

    public void ExtractAndScroll(CancellationToken cancellationToken, string fullPath, string objectName, string parentExportType)
    {
        Log.Information("User CTRL-CLICKED to extract '{FullPath}'", fullPath);
        TabControl.AddTab(fullPath.SubstringAfterLast('/'), fullPath.SubstringBeforeLast('/'), parentExportType);
        TabControl.SelectedTab.ScrollTrigger = objectName;

        var exports = Provider.LoadAllObjects(fullPath);
        TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector(""); // json
        TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(exports, Formatting.Indented), false, false);

        foreach (var e in exports)
        {
            if (CheckExport(cancellationToken, e))
                break;
        }
    }

    private bool CheckExport(CancellationToken cancellationToken, UObject export, EBulkType bulk = EBulkType.None) // return true once you wanna stop searching for exports
    {
        var isNone = bulk == EBulkType.None;
        var updateUi = !HasFlag(bulk, EBulkType.Auto);
        var saveTextures = HasFlag(bulk, EBulkType.Textures);
        switch (export)
        {
            case UVerseDigest verseDigest when isNone:
            {
                if (!TabControl.CanAddTabs) return false;

                TabControl.AddTab($"{verseDigest.ProjectName}.verse");
                TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector("verse");
                TabControl.SelectedTab.SetDocumentText(verseDigest.ReadableCode, false, false);
                return true;
            }
            case UTexture texture when isNone || saveTextures:
            {
                TabControl.SelectedTab.AddImage(texture, saveTextures, updateUi);
                return false;
            }
            case USvgAsset svgasset when isNone || saveTextures:
            {
                var data = svgasset.GetOrDefault<byte[]>("SvgData");
                var sourceFile = svgasset.GetOrDefault<string>("SourceFile");
                using var stream = new MemoryStream(data) { Position = 0 };
                var svg = new SkiaSharp.Extended.Svg.SKSvg(new SKSize(512, 512));
                svg.Load(stream);

                var bitmap = new SKBitmap(512, 512);
                using (var canvas = new SKCanvas(bitmap))
                using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium })
                {
                    canvas.DrawPicture(svg.Picture, paint);
                }

                if (saveTextures)
                {
                    var fileName = sourceFile.SubstringAfterLast('/');
                    var t = new TabImage(fileName, false, bitmap);
                    var path = Path.Combine(UserSettings.Default.TextureDirectory,
                        UserSettings.Default.KeepDirectoryStructure ? TabControl.SelectedTab.Directory : "", fileName!).Replace('\\', '/');

                    System.IO.Directory.CreateDirectory(path.SubstringBeforeLast('/'));

                    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    fs.Write(data, 0, data.Length);
                    if (File.Exists(path))
                    {
                        Log.Information("{FileName} successfully saved", fileName);
                        if (updateUi)
                        {
                            FLogger.Append(ELog.Information, () =>
                            {
                                FLogger.Text("Successfully saved ", Constants.WHITE);
                                FLogger.Link(fileName, path, true);
                            });
                        }
                    }
                    else
                    {
                        Log.Error("{FileName} could not be saved", fileName);
                        if (updateUi)
                            FLogger.Append(ELog.Error, () => FLogger.Text($"Could not save '{fileName}'", Constants.WHITE, true));
                    }
                }

                TabControl.SelectedTab.AddImage(sourceFile.SubstringAfterLast('/'), false, bitmap, false, updateUi);
                return false;
            }
            case UAkMediaAssetData when isNone:
            case USoundWave when isNone:
            {
                var shouldDecompress = UserSettings.Default.CompressedAudioMode == ECompressedAudio.PlayDecompressed;
                export.Decode(shouldDecompress, out var audioFormat, out var data);
                var hasAf = !string.IsNullOrEmpty(audioFormat);
                if (data == null || !hasAf || export.Owner == null)
                {
                    if (hasAf) FLogger.Append(ELog.Warning, () => FLogger.Text($"Unsupported audio format '{audioFormat}'", Constants.WHITE, true));
                    return false;
                }

                SaveAndPlaySound(Path.Combine(TabControl.SelectedTab.Directory, TabControl.SelectedTab.Header.SubstringBeforeLast('.')).Replace('\\', '/'), audioFormat, data);
                return false;
            }
            case UWorld when isNone && UserSettings.Default.PreviewWorlds:
            case UBlueprintGeneratedClass when isNone && UserSettings.Default.PreviewWorlds && TabControl.SelectedTab.ParentExportType switch
            {
                "JunoBuildInstructionsItemDefinition" => true,
                "JunoBuildingSetAccountItemDefinition" => true,
                "JunoBuildingPropAccountItemDefinition" => true,
                _ => false
            }:
            case UPaperSprite when isNone && UserSettings.Default.PreviewMaterials:
            case UStaticMesh when isNone && UserSettings.Default.PreviewStaticMeshes:
            case USkeletalMesh when isNone && UserSettings.Default.PreviewSkeletalMeshes:
            case USkeleton when isNone && UserSettings.Default.SaveSkeletonAsMesh:
            case UMaterialInstance when isNone && UserSettings.Default.PreviewMaterials && !ModelIsOverwritingMaterial &&
                                        !(Provider.InternalGameName.Equals("FortniteGame", StringComparison.OrdinalIgnoreCase) && export.Owner != null &&
                                          (export.Owner.Name.Contains("/MI_OfferImages/", StringComparison.OrdinalIgnoreCase) ||
                                            export.Owner.Name.EndsWith($"/RenderSwitch_Materials/{export.Name}", StringComparison.OrdinalIgnoreCase) ||
                                            export.Owner.Name.EndsWith($"/MI_BPTile/{export.Name}", StringComparison.OrdinalIgnoreCase))):
            {
                if (SnooperViewer.TryLoadExport(cancellationToken, export))
                    SnooperViewer.Run();
                return true;
            }
            case UMaterialInstance m when isNone && ModelIsOverwritingMaterial:
            {
                SnooperViewer.Renderer.Swap(m);
                SnooperViewer.Run();
                return true;
            }
            case UAnimSequence when isNone && ModelIsWaitingAnimation:
            case UAnimMontage when isNone && ModelIsWaitingAnimation:
            case UAnimComposite when isNone && ModelIsWaitingAnimation:
            {
                SnooperViewer.Renderer.Animate(export);
                SnooperViewer.Run();
                return true;
            }
            case UStaticMesh when HasFlag(bulk, EBulkType.Meshes):
            case USkeletalMesh when HasFlag(bulk, EBulkType.Meshes):
            case USkeleton when UserSettings.Default.SaveSkeletonAsMesh && HasFlag(bulk, EBulkType.Meshes):
            // case UMaterialInstance when HasFlag(bulk, EBulkType.Materials): // read the fucking json
            case UAnimSequence when HasFlag(bulk, EBulkType.Animations):
            case UAnimMontage when HasFlag(bulk, EBulkType.Animations):
            case UAnimComposite when HasFlag(bulk, EBulkType.Animations):
            {
                SaveExport(export, updateUi);
                return true;
            }
            default:
            {
                if (!isNone && !saveTextures) return false;

                using var package = new CreatorPackage(export, UserSettings.Default.CosmeticStyle);
                if (!package.TryConstructCreator(out var creator))
                    return false;

                creator.ParseForInfo();
                TabControl.SelectedTab.AddImage(export.Name, false, creator.Draw(), saveTextures, updateUi);
                return true;

            }
        }
    }

    public void ShowMetadata(string fullPath)
    {
        var package = Provider.LoadPackage(fullPath);

        var directory = fullPath.SubstringBeforeLast('/');
        var fileName = $"{fullPath.SubstringAfterLast('/')} (Metadata)";

        if (TabControl.CanAddTabs) TabControl.AddTab(fileName, directory);
        else TabControl.SelectedTab.SoftReset(fileName, directory);
        TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector("");

        TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(package, Formatting.Indented), false, false);
    }

    private void SaveAndPlaySound(string fullPath, string ext, byte[] data)
    {
        if (fullPath.StartsWith("/")) fullPath = fullPath[1..];
        var savedAudioPath = Path.Combine(UserSettings.Default.AudioDirectory,
            UserSettings.Default.KeepDirectoryStructure ? fullPath : fullPath.SubstringAfterLast('/')).Replace('\\', '/') + $".{ext.ToLowerInvariant()}";

        if (!UserSettings.Default.IsAutoOpenSounds)
        {
            Directory.CreateDirectory(savedAudioPath.SubstringBeforeLast('/'));
            using var stream = new FileStream(savedAudioPath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);
            writer.Write(data);
            writer.Flush();
            return;
        }

        // TODO
        // since we are currently in a thread, the audio player's lifetime (memory-wise) will keep the current thread up and running until fmodel itself closes
        // the solution would be to kill the current thread at this line and then open the audio player without "Application.Current.Dispatcher.Invoke"
        // but the ThreadWorkerViewModel is an idiot and doesn't understand we want to kill the current thread inside the current thread and continue the code
        Application.Current.Dispatcher.Invoke(delegate
        {
            var audioPlayer = Helper.GetWindow<AudioPlayer>("Audio Player", () => new AudioPlayer().Show());
            audioPlayer.Load(data, savedAudioPath);
        });
    }

    private void SaveExport(UObject export, bool updateUi = true)
    {
        var toSave = new Exporter(export, UserSettings.Default.ExportOptions);
        var toSaveDirectory = new DirectoryInfo(UserSettings.Default.ModelDirectory);
        if (toSave.TryWriteToDir(toSaveDirectory, out var label, out var savedFilePath))
        {
            Log.Information("Successfully saved {FilePath}", savedFilePath);
            if (updateUi)
            {
                FLogger.Append(ELog.Information, () =>
                {
                    FLogger.Text("Successfully saved ", Constants.WHITE);
                    FLogger.Link(label, savedFilePath, true);
                });
            }
        }
        else
        {
            Log.Error("{FileName} could not be saved", export.Name);
            FLogger.Append(ELog.Error, () => FLogger.Text($"Could not save '{export.Name}'", Constants.WHITE, true));
        }
    }

    private readonly object _rawData = new ();
    public void ExportData(string fullPath, bool updateUi = true)
    {
        var fileName = fullPath.SubstringAfterLast('/');
        if (Provider.TrySavePackage(fullPath, out var assets))
        {
            string path = UserSettings.Default.RawDataDirectory;
            Parallel.ForEach(assets, kvp =>
            {
                lock (_rawData)
                {
                    path = Path.Combine(UserSettings.Default.RawDataDirectory, UserSettings.Default.KeepDirectoryStructure ? kvp.Key : kvp.Key.SubstringAfterLast('/')).Replace('\\', '/');
                    Directory.CreateDirectory(path.SubstringBeforeLast('/'));
                    File.WriteAllBytes(path, kvp.Value);
                }
            });

            Log.Information("{FileName} successfully exported", fileName);
            if (updateUi)
            {
                FLogger.Append(ELog.Information, () =>
                {
                    FLogger.Text("Successfully exported ", Constants.WHITE);
                    FLogger.Link(fileName, path, true);
                });
            }
        }
        else
        {
            Log.Error("{FileName} could not be exported", fileName);
            if (updateUi)
                FLogger.Append(ELog.Error, () => FLogger.Text($"Could not export '{fileName}'", Constants.WHITE, true));
        }
    }

    private static bool HasFlag(EBulkType a, EBulkType b)
    {
        return (a & b) == b;
    }
}
