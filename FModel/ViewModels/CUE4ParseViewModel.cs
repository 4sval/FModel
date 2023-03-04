using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AdonisUI.Controls;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Solaris;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Oodle.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Shaders;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Wwise;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Sounds;
using EpicManifestParser.Objects;
using FModel.Creator;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.Views;
using FModel.Views.Resources.Controls;
using FModel.Views.Snooper;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Serilog;
using SkiaSharp;

namespace FModel.ViewModels;

public class CUE4ParseViewModel : ViewModel
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;
    private readonly Regex _package = new(@"^(?!global|pakchunk.+optional\-).+(pak|utoc)$", // should be universal
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly Regex _fnLive = new(@"^FortniteGame(/|\\)Content(/|\\)Paks(/|\\)(pakchunk(?:0|10.*|20.*|\w+)-WindowsClient|global)\.(pak|utoc)$",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private FGame _game;
    public FGame Game
    {
        get => _game;
        set => SetProperty(ref _game, value);
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
            return Application.Current.Dispatcher.Invoke(delegate
            {
                return _snooper ??= new Snooper(
                    new GameWindowSettings { RenderFrequency = Snooper.GetMaxRefreshFrequency() },
                    new NativeWindowSettings
                    {
                        Size = new OpenTK.Mathematics.Vector2i(
                            Convert.ToInt32(SystemParameters.MaximizedPrimaryScreenWidth * .75),
                            Convert.ToInt32(SystemParameters.MaximizedPrimaryScreenHeight * .85)),
                        NumberOfSamples = Constants.SAMPLES_COUNT,
                        WindowBorder = WindowBorder.Resizable,
                        Flags = ContextFlags.ForwardCompatible,
                        Profile = ContextProfile.Core,
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
    public int LocalizedResourcesCount { get; set; }
    public bool HotfixedResourcesDone { get; set; }
    public int VirtualPathCount { get; set; }

    public CUE4ParseViewModel(string gameDirectory)
    {
        switch (gameDirectory)
        {
            case Constants._FN_LIVE_TRIGGER:
            {
                Game = FGame.FortniteGame;
                Provider = new StreamedFileProvider("FortniteLive", true,
                    new VersionContainer(
                        UserSettings.Default.OverridedGame[Game], UserSettings.Default.OverridedPlatform,
                        customVersions: UserSettings.Default.OverridedCustomVersions[Game],
                        optionOverrides: UserSettings.Default.OverridedOptions[Game]));
                break;
            }
            case Constants._VAL_LIVE_TRIGGER:
            {
                Game = FGame.ShooterGame;
                Provider = new StreamedFileProvider("ValorantLive", true,
                    new VersionContainer(
                        UserSettings.Default.OverridedGame[Game], UserSettings.Default.OverridedPlatform,
                        customVersions: UserSettings.Default.OverridedCustomVersions[Game],
                        optionOverrides: UserSettings.Default.OverridedOptions[Game]));
                break;
            }
            default:
            {
                var parent = gameDirectory.SubstringBeforeLast("\\Content").SubstringAfterLast("\\");
                if (gameDirectory.Contains("eFootball")) parent = gameDirectory.SubstringBeforeLast("\\pak").SubstringAfterLast("\\");
                Game = Helper.IAmThePanda(parent) ? FGame.PandaGame : parent.ToEnum(FGame.Unknown);
                var versions = new VersionContainer(UserSettings.Default.OverridedGame[Game], UserSettings.Default.OverridedPlatform,
                    customVersions: UserSettings.Default.OverridedCustomVersions[Game],
                    optionOverrides: UserSettings.Default.OverridedOptions[Game],
                    mapStructTypesOverrides: UserSettings.Default.OverridedMapStructTypes[Game]);

                switch (Game)
                {
                    case FGame.StateOfDecay2:
                    {
                        Provider = new DefaultFileProvider(new DirectoryInfo(gameDirectory), new List<DirectoryInfo>
                            {
                                new(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\StateOfDecay2\\Saved\\Paks"),
                                new(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\StateOfDecay2\\Saved\\DisabledPaks")
                            },
                            SearchOption.AllDirectories, true, versions);
                        break;
                    }
                    case FGame.FortniteGame:
                        Provider = new DefaultFileProvider(new DirectoryInfo(gameDirectory), new List<DirectoryInfo>
                            {
                                new(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\FortniteGame\\Saved\\PersistentDownloadDir\\InstalledBundles"),
                            },
                            SearchOption.AllDirectories, true, versions);
                        break;
                    case FGame.eFootball:
                        Provider = new DefaultFileProvider(new DirectoryInfo(gameDirectory), new List<DirectoryInfo>
                            {
                                new(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\KONAMI\\eFootball\\ST\\Download")
                            },
                            SearchOption.AllDirectories, true, versions);
                        break;
                    case FGame.Unknown when UserSettings.Default.ManualGames.TryGetValue(gameDirectory, out var settings):
                    {
                        versions = new VersionContainer(settings.OverridedGame, UserSettings.Default.OverridedPlatform,
                            customVersions: settings.OverridedCustomVersions,
                            optionOverrides: settings.OverridedOptions,
                            mapStructTypesOverrides: settings.OverridedMapStructTypes);
                        goto default;
                    }
                    default:
                    {
                        Provider = new DefaultFileProvider(gameDirectory, SearchOption.AllDirectories, true, versions);
                        break;
                    }
                }

                break;
            }
        }

        GameDirectory = new GameDirectoryViewModel();
        AssetsFolder = new AssetsFolderViewModel();
        SearchVm = new SearchViewModel();
        TabControl = new TabControlViewModel();
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
                            if (manifestInfo == null)
                            {
                                throw new Exception("Could not load latest Fortnite manifest, you may have to switch to your local installation.");
                            }

                            byte[] manifestData;
                            var chunksDir = Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, ".data"));
                            var manifestPath = Path.Combine(chunksDir.FullName, manifestInfo.FileName);
                            if (File.Exists(manifestPath))
                            {
                                manifestData = File.ReadAllBytes(manifestPath);
                            }
                            else
                            {
                                manifestData = manifestInfo.DownloadManifestData();
                                File.WriteAllBytes(manifestPath, manifestData);
                            }

                            var manifest = new Manifest(manifestData, new ManifestOptions
                            {
                                ChunkBaseUri = new Uri("http://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/ChunksV4/", UriKind.Absolute),
                                ChunkCacheDirectory = chunksDir
                            });

                            foreach (var fileManifest in manifest.FileManifests)
                            {
                                if (!_fnLive.IsMatch(fileManifest.Name)) continue;

                                //var casStream = manifest.FileManifests.FirstOrDefault(x => x.Name.Equals(fileManifest.Name.Replace(".utoc", ".ucas")));
                                //p.Initialize(fileManifest.Name, new[] {fileManifest.GetStream(), casStream.GetStream()});
                                p.Initialize(fileManifest.Name, new Stream[] { fileManifest.GetStream() }
                                    , it => new FStreamArchive(it, manifest.FileManifests.First(x => x.Name.Equals(it)).GetStream(), p.Versions));
                            }

                            FLogger.AppendInformation();
                            FLogger.AppendText($"Fortnite has been loaded successfully in {manifest.ParseTime.TotalMilliseconds}ms", Constants.WHITE, true);
                            FLogger.AppendWarning();
                            FLogger.AppendText($"Mappings must match '{manifest.BuildVersion}' in order to avoid errors", Constants.WHITE, true);
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
                                p.Initialize(manifestInfo.Paks[i].GetFullName(), new[] { manifestInfo.GetPakStream(i) });
                            }

                            FLogger.AppendInformation();
                            FLogger.AppendText($"Valorant '{manifestInfo.Header.GameVersion}' has been loaded successfully", Constants.WHITE, true);
                            break;
                        }
                    }

                    break;
                case DefaultFileProvider d:
                    d.Initialize();
                    break;
            }

            foreach (var vfs in Provider.UnloadedVfs) // push files from the provider to the ui
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (vfs.Length <= 365 || !_package.IsMatch(vfs.Name)) continue;

                GameDirectory.Add(vfs);
            }
        });
    }

    /// <summary>
    /// load virtual files system from GameDirectory
    /// </summary>
    /// <returns></returns>
    public async Task LoadVfs(IEnumerable<FileItem> aesKeys)
    {
        await _threadWorkerView.Begin(cancellationToken =>
        {
            GameDirectory.DeactivateAll();

            // load files using UnloadedVfs to include non-encrypted vfs
            foreach (var key in aesKeys)
            {
                cancellationToken.ThrowIfCancellationRequested(); // cancel if needed

                var k = key.Key.Trim();
                if (k.Length != 66) k = Constants.ZERO_64_CHAR;
                Provider.SubmitKey(key.Guid, new FAesKey(k));
            }

            // files in MountedVfs will be enabled
            foreach (var file in GameDirectory.DirectoryFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Provider.MountedVfs.FirstOrDefault(x => x.Name == file.Name) is not { } vfs)
                {
                    if (Provider.UnloadedVfs.FirstOrDefault(x => x.Name == file.Name) is IoStoreReader store)
                        file.FileCount = (int) store.Info.TocEntryCount - 1;

                    continue;
                }

                file.IsEnabled = true;
                file.MountPoint = vfs.MountPoint;
                file.FileCount = vfs.FileCount;
            }

            Game = Helper.IAmThePanda(Provider.GameName) ? FGame.PandaGame : Provider.GameName.ToEnum(Game);
        });
    }

    public void ClearProvider()
    {
        if (Provider == null) return;

        AssetsFolder.Folders.Clear();
        SearchVm.SearchResults.Clear();
        Helper.CloseWindow<AdonisWindow>("Search View");
        Provider.UnloadAllVfs();
        GC.Collect();
    }

    public async Task RefreshAes()
    {
        // game directory dependent, we don't have the provider game name yet since we don't have aes keys
        // except when this comes from the AES Manager
        if (!UserSettings.IsEndpointValid(Game, EEndpointType.Aes, out var endpoint))
            return;

        await _threadWorkerView.Begin(cancellationToken =>
        {
            var aes = _apiEndpointView.DynamicApi.GetAesKeys(cancellationToken, endpoint.Url, endpoint.Path);
            if (aes is not { IsValid: true }) return;

            UserSettings.Default.AesKeys[Game] = aes;
        });
    }

    public async Task InitInformation()
    {
        await _threadWorkerView.Begin(cancellationToken =>
        {
            var info = _apiEndpointView.FModelApi.GetNews(cancellationToken, Provider.GameName);
            if (info == null) return;

            for (var i = 0; i < info.Messages.Length; i++)
            {
                FLogger.AppendText(info.Messages[i], info.Colors[i], bool.Parse(info.NewLines[i]));
            }
        });
    }

    public async Task InitMappings()
    {
        if (!UserSettings.IsEndpointValid(Game, EEndpointType.Mapping, out var endpoint))
        {
            Provider.MappingsContainer = null;
            return;
        }

        await _threadWorkerView.Begin(cancellationToken =>
        {
            if (endpoint.Overwrite && File.Exists(endpoint.FilePath))
            {
                Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(endpoint.FilePath);
                FLogger.AppendInformation();
                FLogger.AppendText($"Mappings pulled from '{endpoint.FilePath.SubstringAfterLast("\\")}'", Constants.WHITE, true);
            }
            else if (endpoint.IsValid)
            {
                var mappingsFolder = Path.Combine(UserSettings.Default.OutputDirectory, ".data");
                var mappings = _apiEndpointView.DynamicApi.GetMappings(cancellationToken, endpoint.Url, endpoint.Path);
                if (mappings is { Length: > 0 })
                {
                    foreach (var mapping in mappings)
                    {
                        if (!mapping.IsValid) continue;

                        var mappingPath = Path.Combine(mappingsFolder, mapping.FileName);
                        if (!File.Exists(mappingPath))
                        {
                            _apiEndpointView.DownloadFile(mapping.Url, mappingPath);
                        }

                        Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingPath);
                        FLogger.AppendInformation();
                        FLogger.AppendText($"Mappings pulled from '{mapping.FileName}'", Constants.WHITE, true);
                        break;
                    }
                }

                if (Provider.MappingsContainer == null)
                {
                    var latestUsmaps = new DirectoryInfo(mappingsFolder).GetFiles("*_oo.usmap");
                    if (latestUsmaps.Length <= 0) return;

                    var latestUsmapInfo = latestUsmaps.OrderBy(f => f.LastWriteTime).Last();
                    Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(latestUsmapInfo.FullName);
                    FLogger.AppendWarning();
                    FLogger.AppendText($"Mappings pulled from '{latestUsmapInfo.Name}'", Constants.WHITE, true);
                }
            }
        });
    }

    public async Task LoadLocalizedResources()
    {
        await LoadGameLocalizedResources();
        await LoadHotfixedLocalizedResources();
        await _threadWorkerView.Begin(_ => Utils.Typefaces = new Typefaces(this));
        if (LocalizedResourcesCount > 0)
        {
            FLogger.AppendInformation();
            FLogger.AppendText($"{LocalizedResourcesCount} localized resources loaded for '{UserSettings.Default.AssetLanguage.GetDescription()}'", Constants.WHITE, true);
        }
        else
        {
            FLogger.AppendWarning();
            FLogger.AppendText($"Could not load localized resources in '{UserSettings.Default.AssetLanguage.GetDescription()}', language may not exist", Constants.WHITE, true);
        }
    }

    private async Task LoadGameLocalizedResources()
    {
        if (LocalizedResourcesCount > 0) return;
        await _threadWorkerView.Begin(cancellationToken =>
        {
            LocalizedResourcesCount = Provider.LoadLocalization(UserSettings.Default.AssetLanguage, cancellationToken);
        });
    }

    /// <summary>
    /// Load hotfixed localized resources
    /// </summary>
    /// <remarks>Functions only when LoadLocalizedResources is used prior to this.</remarks>
    private async Task LoadHotfixedLocalizedResources()
    {
        if (Game != FGame.FortniteGame || HotfixedResourcesDone) return;
        await _threadWorkerView.Begin(cancellationToken =>
        {
            var hotfixes = ApplicationService.ApiEndpointView.CentralApi.GetHotfixes(cancellationToken, Provider.GetLanguageCode(UserSettings.Default.AssetLanguage));
            if (hotfixes == null) return;

            HotfixedResourcesDone = true;
            foreach (var entries in hotfixes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Provider.LocalizedResources.ContainsKey(entries.Key))
                    Provider.LocalizedResources[entries.Key] = new Dictionary<string, string>();

                foreach (var keyValue in entries.Value)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Provider.LocalizedResources[entries.Key][keyValue.Key] = keyValue.Value;
                    LocalizedResourcesCount++;
                }
            }
        });
    }

    public async Task LoadVirtualPaths()
    {
        if (VirtualPathCount > 0) return;
        await _threadWorkerView.Begin(cancellationToken =>
        {
            VirtualPathCount = Provider.LoadVirtualPaths(UserSettings.Default.OverridedGame[Game].GetVersion(), cancellationToken);
            if (VirtualPathCount > 0)
            {
                FLogger.AppendInformation();
                FLogger.AppendText($"{VirtualPathCount} virtual paths loaded", Constants.WHITE, true);
            }
            else
            {
                FLogger.AppendWarning();
                FLogger.AppendText("Could not load virtual paths, plugin manifest may not exist", Constants.WHITE, true);
            }
        });
    }

    public void ExtractSelected(CancellationToken cancellationToken, IEnumerable<AssetItem> assetItems)
    {
        foreach (var asset in assetItems)
        {
            Thread.Sleep(10);
            cancellationToken.ThrowIfCancellationRequested();
            Extract(cancellationToken, asset.FullPath, TabControl.HasNoTabs);
        }
    }

    private void BulkFolder(CancellationToken cancellationToken, TreeItem folder, Action<AssetItem> action)
    {
        foreach (var asset in folder.AssetsList.Assets)
        {
            Thread.Sleep(10);
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

        if (addNewTab && TabControl.CanAddTabs)
        {
            TabControl.AddTab(fileName, directory);
        }
        else
        {
            TabControl.SelectedTab.Header = fileName;
            TabControl.SelectedTab.Directory = directory;
        }

        var autoProperties = bulk == (EBulkType.Properties | EBulkType.Auto);
        var autoTextures = bulk == (EBulkType.Textures | EBulkType.Auto);
        TabControl.SelectedTab.ClearImages();
        TabControl.SelectedTab.ResetDocumentText();
        TabControl.SelectedTab.ScrollTrigger = null;
        TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector(ext);
        switch (ext)
        {
            case "uasset":
            case "umap":
            {
                var exports = Provider.LoadObjectExports(fullPath); // cancellationToken
                TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(exports, Formatting.Indented), autoProperties);
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
            case "js":
            case "po":
            case "h":
            {
                if (Provider.TrySaveAsset(fullPath, out var data))
                {
                    using var stream = new MemoryStream(data) { Position = 0 };
                    using var reader = new StreamReader(stream);

                    TabControl.SelectedTab.SetDocumentText(reader.ReadToEnd(), autoProperties);
                }

                break;
            }
            case "locmeta":
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var metadata = new FTextLocalizationMetaDataResource(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(metadata, Formatting.Indented), autoProperties);
                }

                break;
            }
            case "locres":
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var locres = new FTextLocalizationResource(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(locres, Formatting.Indented), autoProperties);
                }

                break;
            }
            case "bin" when fileName.Contains("AssetRegistry"):
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var registry = new FAssetRegistryState(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(registry, Formatting.Indented), autoProperties);
                }

                break;
            }
            case "bnk":
            case "pck":
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var wwise = new WwiseReader(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(wwise, Formatting.Indented), autoProperties);
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
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(header, Formatting.Indented), autoProperties);
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
                    TabControl.SelectedTab.AddImage(fileName.SubstringBeforeLast("."), false, SKBitmap.Decode(stream), autoTextures);
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

                    TabControl.SelectedTab.AddImage(fileName.SubstringBeforeLast("."), false, bitmap, autoTextures);
                }

                break;
            }
            case "ufont":
            case "otf":
            case "ttf":
                FLogger.AppendWarning();
                FLogger.AppendText($"Export '{fileName}' raw data and change its extension if you want it to be an installable font file", Constants.WHITE, true);
                break;
            case "ushaderbytecode":
            case "ushadercode":
            {
                if (Provider.TryCreateReader(fullPath, out var archive))
                {
                    var ar = new FShaderCodeArchive(archive);
                    TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(ar, Formatting.Indented), autoProperties);
                }

                break;
            }
            default:
            {
                FLogger.AppendWarning();
                FLogger.AppendText($"The package '{fileName}' is of an unknown type.", Constants.WHITE, true);
                break;
            }
        }
    }

    public void ExtractAndScroll(CancellationToken cancellationToken, string fullPath, string objectName)
    {
        Log.Information("User CTRL-CLICKED to extract '{FullPath}'", fullPath);
        TabControl.AddTab(fullPath.SubstringAfterLast('/'), fullPath.SubstringBeforeLast('/'));
        TabControl.SelectedTab.ScrollTrigger = objectName;

        var exports = Provider.LoadObjectExports(fullPath); // cancellationToken
        TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector(""); // json
        TabControl.SelectedTab.SetDocumentText(JsonConvert.SerializeObject(exports, Formatting.Indented), false);

        foreach (var e in exports)
        {
            if (CheckExport(cancellationToken, e))
                break;
        }
    }

    private bool CheckExport(CancellationToken cancellationToken, UObject export, EBulkType bulk = EBulkType.None) // return true once you wanna stop searching for exports
    {
        var isNone = bulk == EBulkType.None;
        var loadTextures = isNone || HasFlag(bulk, EBulkType.Textures);
        switch (export)
        {
            case USolarisDigest solarisDigest when isNone:
            {
                if (!TabControl.CanAddTabs) return false;

                TabControl.AddTab($"{solarisDigest.ProjectName}.verse");
                TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector("verse");
                TabControl.SelectedTab.SetDocumentText(solarisDigest.ReadableCode, false);
                return true;
            }
            case UTexture2D { IsVirtual: false } texture when loadTextures:
            {
                TabControl.SelectedTab.AddImage(texture, HasFlag(bulk, EBulkType.Auto));
                return false;
            }
            case UAkMediaAssetData when isNone:
            case USoundWave when isNone:
            {
                var shouldDecompress = UserSettings.Default.CompressedAudioMode == ECompressedAudio.PlayDecompressed;
                export.Decode(shouldDecompress, out var audioFormat, out var data);
                if (data == null || string.IsNullOrEmpty(audioFormat) || export.Owner == null)
                    return false;

                SaveAndPlaySound(Path.Combine(TabControl.SelectedTab.Directory, TabControl.SelectedTab.Header.SubstringBeforeLast('.')).Replace('\\', '/'), audioFormat, data);
                return false;
            }
            case UWorld when isNone && UserSettings.Default.PreviewWorlds:
            case UStaticMesh when isNone && UserSettings.Default.PreviewStaticMeshes:
            case USkeletalMesh when isNone && UserSettings.Default.PreviewSkeletalMeshes:
            case UMaterialInstance when isNone && UserSettings.Default.PreviewMaterials && !ModelIsOverwritingMaterial &&
                                        !(Game == FGame.FortniteGame && export.Owner != null && (export.Owner.Name.EndsWith($"/MI_OfferImages/{export.Name}", StringComparison.OrdinalIgnoreCase) ||
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
                SaveExport(export, HasFlag(bulk, EBulkType.Auto));
                return true;
            }
            default:
            {
                if (!loadTextures)
                    return false;

                using var package = new CreatorPackage(export, UserSettings.Default.CosmeticStyle);
                if (!package.TryConstructCreator(out var creator))
                    return false;

                creator.ParseForInfo();
                TabControl.SelectedTab.AddImage(export.Name, false, creator.Draw(), HasFlag(bulk, EBulkType.Auto));
                return true;
            }
        }
    }

    private void SaveAndPlaySound(string fullPath, string ext, byte[] data)
    {
        if (fullPath.StartsWith("/")) fullPath = fullPath[1..];
        var savedAudioPath = Path.Combine(UserSettings.Default.AudioDirectory,
            UserSettings.Default.KeepDirectoryStructure ? fullPath : fullPath.SubstringAfterLast('/')).Replace('\\', '/') + $".{ext.ToLower()}";

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

    private void SaveExport(UObject export, bool auto)
    {
        var exportOptions = new ExporterOptions
        {
            LodFormat = UserSettings.Default.LodExportFormat,
            MeshFormat = UserSettings.Default.MeshExportFormat,
            MaterialFormat = UserSettings.Default.MaterialExportFormat,
            TextureFormat = UserSettings.Default.TextureExportFormat,
            SocketFormat = UserSettings.Default.SocketExportFormat,
            Platform = UserSettings.Default.OverridedPlatform,
            ExportMorphTargets = UserSettings.Default.SaveMorphTargets
        };
        var toSave = new Exporter(export, exportOptions);

        string dir;
        if (!auto)
        {
            var folderBrowser = new VistaFolderBrowserDialog();
            if (folderBrowser.ShowDialog() == true)
                dir = folderBrowser.SelectedPath;
            else return;
        }
        else dir = UserSettings.Default.ModelDirectory;

        var toSaveDirectory = new DirectoryInfo(dir);
        if (toSave.TryWriteToDir(toSaveDirectory, out var label, out var savedFilePath))
        {
            Log.Information("Successfully saved {FilePath}", savedFilePath);
            FLogger.AppendInformation();
            FLogger.AppendText("Successfully saved ", Constants.WHITE);
            FLogger.AppendLink(label, savedFilePath, true);
        }
        else
        {
            Log.Warning("{FileName} could not be saved", export.Name);
            FLogger.AppendWarning();
            FLogger.AppendText($"Could not save '{export.Name}'", Constants.WHITE, true);
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

            if (updateUi)
            {
                Log.Information("{FileName} successfully exported", fileName);
                FLogger.AppendInformation();
                FLogger.AppendText("Successfully exported ", Constants.WHITE);
                FLogger.AppendLink(fileName, path, true);
            }
        }
        else if (updateUi)
        {
            Log.Error("{FileName} could not be exported", fileName);
            FLogger.AppendError();
            FLogger.AppendText($"Could not export '{fileName}'", Constants.WHITE, true);
        }
    }

    private static bool HasFlag(EBulkType a, EBulkType b)
    {
        return (a & b) == b;
    }
}
