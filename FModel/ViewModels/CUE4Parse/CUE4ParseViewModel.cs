using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.GameTypes.KRD.Assets.Exports;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Verse;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.BinaryConfig;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Objects.UObject.Editor;
using CUE4Parse.UE4.Oodle.Objects;
using CUE4Parse.UE4.Shaders;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Wwise;
using CUE4Parse.Utils;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Sounds;
using FModel.Creator;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.Views;
using FModel.Views.Resources.Controls;
using FModel.Views.Snooper;
using Newtonsoft.Json;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Serilog;
using SkiaSharp;
using UE4Config.Parsing;
using Application = System.Windows.Application;

namespace FModel.ViewModels.CUE4Parse;

public partial class CUE4ParseViewModel : ViewModel
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;
    private readonly Regex _fnLiveRegex = new(@"^FortniteGame[/\\]Content[/\\]Paks[/\\]",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
            if (_snooper != null)
                return _snooper;

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
    public AbstractVfsFileProvider DiffProvider { get; }
    public GameDirectoryViewModel GameDirectory { get; }
    public GameDirectoryViewModel DiffGameDirectory { get; }
    public AssetsFolderViewModel AssetsFolder { get; }
    public SearchViewModel SearchVm { get; }
    public TabControlViewModel TabControl { get; }
    public ConfigIni IoStoreOnDemand { get; }
    private Lazy<WwiseProvider> _wwiseProviderLazy;
    public WwiseProvider WwiseProvider => _wwiseProviderLazy.Value;

    public CUE4ParseViewModel()
    {
        Provider = CreateProvider(UserSettings.Default.CurrentDir, _fnLiveRegex);

        if (UserSettings.Default.DiffDir != null)
        {
            DiffProvider = CreateProvider(UserSettings.Default.DiffDir, _fnLiveRegex);
            DiffGameDirectory = new GameDirectoryViewModel();
        }

        RefreshReadSettings();

        GameDirectory = new GameDirectoryViewModel();
        AssetsFolder = new AssetsFolderViewModel();
        SearchVm = new SearchViewModel();
        TabControl = new TabControlViewModel();
        IoStoreOnDemand = new ConfigIni(nameof(IoStoreOnDemand));
    }

    public async Task InitInformation()
    {
        await _threadWorkerView.Begin(cancellationToken =>
        {
            var info = _apiEndpointView.FModelApi.GetNews(cancellationToken, Provider.ProjectName);
            if (info == null)
                return;

            FLogger.Append(ELog.None, () =>
            {
                for (var i = 0; i < info.Messages.Length; i++)
                {
                    FLogger.Text(info.Messages[i], info.Colors[i], bool.Parse(info.NewLines[i]));
                }
            });
        });
    }

    public Task VerifyConsoleVariables()
    {
        if (Provider.Versions["StripAdditiveRefPose"])
        {
            FLogger.Append(ELog.Warning, () =>
                FLogger.Text("Additive animations have their reference pose stripped, which will lead to inaccurate preview and export", Constants.WHITE, true));
        }

        if (Provider.Versions.Game is EGame.GAME_UE4_LATEST or EGame.GAME_UE5_LATEST && !Provider.ProjectName.Equals("FortniteGame", StringComparison.OrdinalIgnoreCase)) // ignore fortnite globally
        {
            FLogger.Append(ELog.Warning, () =>
                FLogger.Text($"Experimental UE version selected, likely unsuitable for '{Provider.GameDisplayName ?? Provider.ProjectName}'", Constants.WHITE, true));
        }

        return Task.CompletedTask;
    }

    public Task VerifyOnDemandArchives()
    {
        // only local fortnite
        if (Provider is not DefaultFileProvider || !Provider.ProjectName.Equals("FortniteGame", StringComparison.OrdinalIgnoreCase))
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
            if (inst.Count <= 0)
                return;

            var ioStoreOnDemandPath = Path.Combine(UserSettings.Default.GameDirectory, "..\\..\\..\\Cloud", inst[0].Value.SubstringAfterLast("/").SubstringBefore("\""));
            if (!File.Exists(ioStoreOnDemandPath))
                return;

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

    public void ExtractSelected(CancellationToken cancellationToken, IEnumerable<GameFile> assetItems)
    {
        foreach (var entry in assetItems)
        {
            Thread.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            Extract(cancellationToken, entry, TabControl.HasNoTabs);
        }
    }

    private void BulkFolder(CancellationToken cancellationToken, TreeItem folder, Action<GameFile> action)
    {
        foreach (var entry in folder.AssetsList.Assets)
        {
            Thread.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                action(entry);
            }
            catch
            {
                // ignore
            }
        }

        foreach (var f in folder.Folders)
            BulkFolder(cancellationToken, f, action);
    }

    public void ExportFolder(CancellationToken cancellationToken, TreeItem folder)
    {
        Parallel.ForEach(folder.AssetsList.Assets, entry =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExportData(entry, false);
        });

        foreach (var f in folder.Folders)
            ExportFolder(cancellationToken, f);
    }

    private static string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj, Formatting.Indented);

    public void ExtractFolder(CancellationToken cancellationToken, TreeItem folder)
        => BulkFolder(cancellationToken, folder, asset => Extract(cancellationToken, asset, TabControl.HasNoTabs));

    public void SaveFolder(CancellationToken cancellationToken, TreeItem folder)
        => BulkFolder(cancellationToken, folder, asset => Extract(cancellationToken, asset, TabControl.HasNoTabs, EBulkType.Properties | EBulkType.Auto));

    public void TextureFolder(CancellationToken cancellationToken, TreeItem folder)
        => BulkFolder(cancellationToken, folder, asset => Extract(cancellationToken, asset, TabControl.HasNoTabs, EBulkType.Textures | EBulkType.Auto));

    public void ModelFolder(CancellationToken cancellationToken, TreeItem folder)
        => BulkFolder(cancellationToken, folder, asset => Extract(cancellationToken, asset, TabControl.HasNoTabs, EBulkType.Meshes | EBulkType.Auto));

    public void AnimationFolder(CancellationToken cancellationToken, TreeItem folder)
        => BulkFolder(cancellationToken, folder, asset => Extract(cancellationToken, asset, TabControl.HasNoTabs, EBulkType.Animations | EBulkType.Auto));

    public void Extract(CancellationToken cancellationToken, GameFile entry, bool addNewTab = false, EBulkType bulk = EBulkType.None)
    {
        Log.Information("User DOUBLE-CLICKED to extract '{FullPath}'", entry.Path);

        if (addNewTab && TabControl.CanAddTabs)
            TabControl.AddTab(entry);
        else
            TabControl.SelectedTab.SoftReset(entry);

        TabControl.SelectedTab.DiffContent = null;
        TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector(entry.Extension);

        var updateUi = !HasFlag(bulk, EBulkType.Auto);
        var saveProperties = HasFlag(bulk, EBulkType.Properties);
        var saveTextures = HasFlag(bulk, EBulkType.Textures);

        switch (entry.Extension.ToLowerInvariant())
        {
            case "uasset":
            case "umap":
                {
                    var result = Provider.GetLoadPackageResult(entry);
                    TabControl.SelectedTab.TitleExtra = result.TabTitleExtra;

                    if (saveProperties || updateUi)
                    {
                        TabControl.SelectedTab.SetDocumentText(Serialize(result.GetDisplayData(saveProperties)), saveProperties, updateUi);
                        if (saveProperties)
                            break; // do not search for viewable exports if we are dealing with jsons
                    }

                    for (var i = result.InclusiveStart; i < result.ExclusiveEnd; i++)
                    {
                        if (CheckExport(cancellationToken, result.Package, i, bulk))
                            break;
                    }

                    break;
                }
            case "ini" when entry.Name.Contains("BinaryConfig"):
                {
                    var configCache = new FConfigCacheIni(entry.CreateReader());

                    TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector("json");
                    TabControl.SelectedTab.SetDocumentText(Serialize(configCache), saveProperties, updateUi);

                    break;
                }
            case "png":
            case "jpg":
            case "bmp":
                {
                    var data = Provider.SaveAsset(entry);
                    using var stream = new MemoryStream(data);
                    TabControl.SelectedTab.AddImage(entry.NameWithoutExtension, false, SKBitmap.Decode(stream), saveTextures, updateUi);
                    break;
                }
            case "svg":
                {
                    var data = Provider.SaveAsset(entry);
                    using var stream = new MemoryStream(data);
                    var bitmap = RenderSvg(stream);
                    TabControl.SelectedTab.AddImage(entry.NameWithoutExtension, false, bitmap, saveTextures, updateUi);
                    break;
                }
            // Audio
            case "xvag":
            case "flac":
            case "at9":
            case "wem":
            case "wav":
            case "WAV":
            case "ogg":
            // TODO: CSCore.MediaFoundation.MediaFoundationException The byte stream type of the given URL is unsupported. case "aif":
                {
                    var data = Provider.SaveAsset(entry);
                    SaveAndPlaySound(entry.PathWithoutExtension, entry.Extension, data);
                    break;
                }
            // Wwise
            case "bnk":
            case "pck":
                {
                    var archive = entry.CreateReader();
                    var wwise = new WwiseReader(archive);
                    TabControl.SelectedTab.SetDocumentText(Serialize(wwise), saveProperties, updateUi);

                    foreach (var (name, data) in wwise.WwiseEncodedMedias)
                    {
                        SaveAndPlaySound(entry.Path.SubstringBeforeWithLast('/') + name, "WEM", data);
                    }

                    break;
                }
            // Fonts
            case "ufont":
            case "otf":
            case "ttf":
                {
                    FLogger.Append(ELog.Warning, () =>
                        FLogger.Text($"Export '{entry.Name}' raw data and change its extension if you want it to be an installable font file", Constants.WHITE, true));
                    break;
                }
            case "res": // just skip
            case "luac": // compiled lua
            case "bytes": // wuthering waves
                break;

            default:
                {
                    if (TryExtractStructuredText(entry, Provider, out var text))
                    {
                        TabControl.SelectedTab.SetDocumentText(text, saveProperties, updateUi);
                    }
                    else
                    {
                        FLogger.Append(ELog.Warning, () =>
                            FLogger.Text($"The package '{entry.Name}' is of an unknown type.", Constants.WHITE, true));
                    }

                    break;
                }
        }
    }

    private static bool TryExtractStructuredText(GameFile entry, AbstractVfsFileProvider provider, out string resultText)
    {
        resultText = string.Empty;
        switch (entry.Extension.ToLowerInvariant())
        {
            // UE
            case "uasset":
            case "umap":
                {
                    var package = provider.GetLoadPackageResult(entry);
                    resultText = Serialize(package.GetDisplayData());
                    return true;
                }

            // Localization
            case "locmeta":
                resultText = Serialize(new FTextLocalizationMetaDataResource(entry.CreateReader()));
                return true;
            case "locres":
                resultText = Serialize(new FTextLocalizationResource(entry.CreateReader()));
                return true;

            case "bin" when entry.Name.Contains("AssetRegistry", StringComparison.OrdinalIgnoreCase):
                resultText = Serialize(new FAssetRegistryState(entry.CreateReader()));
                return true;
            case "bin" when entry.Name.Contains("GlobalShaderCache", StringComparison.OrdinalIgnoreCase):
                resultText = Serialize(new FGlobalShaderCache(entry.CreateReader()));
                return true;

            // Wwise
            case "bnk":
            case "pck":
                {
                    var archive = entry.CreateReader();
                    var wwise = new WwiseReader(archive);
                    resultText = Serialize(wwise);
                    return true;
                }

            case "udic":
                resultText = Serialize(new FOodleDictionaryArchive(entry.CreateReader()).Header);
                return true;
            case "ushaderbytecode":
            case "ushadercode":
                resultText = Serialize(new FShaderCodeArchive(entry.CreateReader()));
                return true;
            case "upipelinecache":
                resultText = Serialize(new FPipelineCacheFile(entry.CreateReader()));
                return true;
            case "stinfo":
                {
                    var archive = entry.CreateReader();
                    var ar = new FShaderTypeHashes(archive);
                    resultText = Serialize(ar);
                    return true;
                }

            // Common text-based formats
            case "archive":
            case "dnearchive": // Banishers: Ghosts of New Eden
            case "gitignore":
            case "LICENSE":
            case "template":
            case "stumeta": // LIS: Double Exposure
            case "json":
            case "manifest":
            case "uproject":
            case "uplugin":
            case "upluginmanifest":
            case "code-workspace":
            case "projectstore":
            case "uefnproject":
            case "uparam": // Steel Hunters
            case "xml":
            case "ini":
            case "txt":
            case "log":
            case "bat":
            case "cfg":
            case "csv":
            case "pem":
            case "tps":
            case "glslfx":
            case "cptake":
            case "spi1d":
            case "uref":
            case "cube":
            case "usda":
            case "ocio":
            case "tgc": // State of Decay 2
            case "cpp":
            case "apx":
            case "udn":
            case "doc":
            case "lua":
            case "vdf":
            case "js":
            case "po":
            case "h":
            case "md":
            case "c":
            case "hpp":
            case "cs":
            case "vb":
            case "py":
            case "markdown":
            case "yml":
            case "yaml":
            case "sh":
            case "cmd":
            case "sql":
            case "css":
            case "scss":
            case "less":
            case "ts":
            case "tsx":
            case "jsx":
            case "html":
            case "htm":
            case "lsd": // Days Gone
            case "dat":
            case "ddr":
            case "ide":
            case "ipl":
            case "zon":
            case "verse":
            case "vmodule":
                {
                    var data = provider.SaveAsset(entry);
                    using var stream = new MemoryStream(data) { Position = 0 };
                    using var reader = new StreamReader(stream);
                    resultText = reader.ReadToEnd();
                    return true;
                }
            default:
                break;
        }

        return false;
    }

    public void ExtractAndScroll(CancellationToken cancellationToken, string fullPath, string objectName, string parentExportType)
    {
        Log.Information("User CTRL-CLICKED to extract '{FullPath}'", fullPath);

        var entry = Provider[fullPath];
        TabControl.AddTab(entry, parentExportType);
        TabControl.SelectedTab.ScrollTrigger = objectName;

        var result = Provider.GetLoadPackageResult(entry, objectName);

        TabControl.SelectedTab.TitleExtra = result.TabTitleExtra;
        TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector(""); // json
        TabControl.SelectedTab.SetDocumentText(Serialize(result.GetDisplayData()), false, false);

        for (var i = result.InclusiveStart; i < result.ExclusiveEnd; i++)
        {
            if (CheckExport(cancellationToken, result.Package, i))
                break;
        }
    }

    private bool CheckExport(CancellationToken cancellationToken, IPackage pkg, int index, EBulkType bulk = EBulkType.None) // return true once you want to stop searching for exports
    {
        var isNone = bulk == EBulkType.None;
        var updateUi = !HasFlag(bulk, EBulkType.Auto);
        var saveTextures = HasFlag(bulk, EBulkType.Textures);

        var pointer = new FPackageIndex(pkg, index + 1).ResolvedObject;
        if (pointer?.Object is null)
            return false;

        var dummy = ((AbstractUePackage) pkg).ConstructObject(pointer.Class?.Object?.Value as UStruct, pkg);
        switch (dummy)
        {
            case UVerseDigest when isNone && pointer.Object.Value is UVerseDigest verseDigest:
                {
                    if (!TabControl.CanAddTabs)
                        return false;

                    TabControl.AddTab($"{verseDigest.ProjectName}.verse");
                    TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector("verse");
                    TabControl.SelectedTab.SetDocumentText(verseDigest.ReadableCode, false, false);
                    return true;
                }
            case UTexture when (isNone || saveTextures) && pointer.Object.Value is UTexture texture:
                {
                    TabControl.SelectedTab.AddImage(texture, saveTextures, updateUi);
                    return false;
                }
            case USvgAsset when (isNone || saveTextures) && pointer.Object.Value is USvgAsset svgasset:
                {
                    var data = svgasset.GetOrDefault<byte[]>("SvgData");
                    var sourceFile = svgasset.GetOrDefault<string>("SourceFile");
                    using var stream = new MemoryStream(data);
                    stream.Position = 0;
                    var bitmap = RenderSvg(stream);

                    if (saveTextures)
                    {
                        var fileName = sourceFile.SubstringAfterLast('/');
                        var path = Path.Combine(UserSettings.Default.TextureDirectory,
                            UserSettings.Default.KeepDirectoryStructure ? TabControl.SelectedTab.Entry.Directory : "", fileName!).Replace('\\', '/');

                        Directory.CreateDirectory(path.SubstringBeforeLast('/'));

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
            case UAkAudioEvent when isNone && pointer.Object.Value is UAkAudioEvent audioEvent:
                {
                    var extractedSounds = WwiseProvider.ExtractAudioEventSounds(audioEvent);
                    foreach (var sound in extractedSounds)
                    {
                        SaveAndPlaySound(sound.OutputPath, sound.Extension, sound.Data);
                    }
                    return false;
                }
            case UAkMediaAssetData when isNone:
            case USoundWave when isNone:
                {
                    var shouldDecompress = UserSettings.Default.CompressedAudioMode == ECompressedAudio.PlayDecompressed;
                    pointer.Object.Value.Decode(shouldDecompress, out var audioFormat, out var data);
                    var hasAf = !string.IsNullOrEmpty(audioFormat);
                    if (data == null || !hasAf)
                    {
                        if (hasAf)
                            FLogger.Append(ELog.Warning, () => FLogger.Text($"Unsupported audio format '{audioFormat}'", Constants.WHITE, true));
                        return false;
                    }

                    SaveAndPlaySound(TabControl.SelectedTab.Entry.PathWithoutExtension.Replace('\\', '/'), audioFormat, data);
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
                                        !(Provider.ProjectName.Equals("FortniteGame", StringComparison.OrdinalIgnoreCase) &&
                                          (pkg.Name.Contains("/MI_OfferImages/", StringComparison.OrdinalIgnoreCase) ||
                                           pkg.Name.Contains("/RenderSwitch_Materials/", StringComparison.OrdinalIgnoreCase) ||
                                           pkg.Name.Contains("/MI_BPTile/", StringComparison.OrdinalIgnoreCase))):
                {
                    if (SnooperViewer.TryLoadExport(cancellationToken, dummy, pointer.Object))
                        SnooperViewer.Run();
                    return true;
                }
            case UMaterialInstance when isNone && ModelIsOverwritingMaterial && pointer.Object.Value is UMaterialInstance m:
                {
                    SnooperViewer.Renderer.Swap(m);
                    SnooperViewer.Run();
                    return true;
                }
            case UAnimSequenceBase when isNone && UserSettings.Default.PreviewAnimations || ModelIsWaitingAnimation:
                {
                    // animate all animations using their specified skeleton or when we explicitly asked for a loaded model to be animated (ignoring whether we wanted to preview animations)
                    SnooperViewer.Renderer.Animate(pointer.Object.Value);
                    SnooperViewer.Run();
                    return true;
                }
            case UStaticMesh when HasFlag(bulk, EBulkType.Meshes):
            case USkeletalMesh when HasFlag(bulk, EBulkType.Meshes):
            case USkeleton when UserSettings.Default.SaveSkeletonAsMesh && HasFlag(bulk, EBulkType.Meshes):
            // case UMaterialInstance when HasFlag(bulk, EBulkType.Materials): // read the fucking json
            case UAnimSequenceBase when HasFlag(bulk, EBulkType.Animations):
                {
                    SaveExport(pointer.Object.Value, updateUi);
                    return true;
                }
            default:
                {
                    if (!isNone && !saveTextures)
                        return false;

                    using var cPackage = new CreatorPackage(pkg.Name, dummy.ExportType, pointer.Object, UserSettings.Default.CosmeticStyle);
                    if (!cPackage.TryConstructCreator(out var creator))
                        return false;

                    creator.ParseForInfo();
                    TabControl.SelectedTab.AddImage(pointer.Object.Value.Name, false, creator.Draw(), saveTextures, updateUi);
                    return true;

                }
        }
    }

    public void ShowMetadata(GameFile entry)
    {
        var package = Provider.LoadPackage(entry);

        if (TabControl.CanAddTabs)
            TabControl.AddTab(entry);
        else
            TabControl.SelectedTab.SoftReset(entry);

        TabControl.SelectedTab.TitleExtra = "Metadata";
        TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector("");

        TabControl.SelectedTab.SetDocumentText(Serialize(package), false, false);
    }

    [GeneratedRegex("__verse_0x[a-fA-F0-9]{8}_")]
    private static partial Regex UnmangledCasedNameRegex();
    [GeneratedRegex(@"CallFunc_([A-Za-z0-9_]+)_ReturnValue")]
    private static partial Regex CallFuncReturnValueRegex();
    public void Decompile(GameFile entry)
    {
        if (TabControl.CanAddTabs)
            TabControl.AddTab(entry);
        else
            TabControl.SelectedTab.SoftReset(entry);

        TabControl.SelectedTab.TitleExtra = "Decompiled";
        TabControl.SelectedTab.Highlighter = AvalonExtensions.HighlighterSelector("cpp");

        UClassCookedMetaData cookedMetaData = null;
        try
        {
            var editorPkg = Provider.LoadPackage(entry.Path.Replace(".uasset", ".o.uasset"));
            cookedMetaData = editorPkg.GetExport<UClassCookedMetaData>("CookedClassMetaData");
        }
        catch
        {
            // ignored
        }

        var cppList = new List<string>();
        var pkg = Provider.LoadPackage(entry);
        for (var i = 0; i < pkg.ExportMapLength; i++)
        {
            var pointer = new FPackageIndex(pkg, i + 1).ResolvedObject;
            if (pointer?.Object is null && pointer.Class?.Object?.Value is null)
                continue;

            var dummy = ((AbstractUePackage) pkg).ConstructObject(pointer.Class?.Object?.Value as UStruct, pkg);
            if (dummy is not UClass || pointer.Object.Value is not UClass blueprint)
                continue;

            cppList.Add(blueprint.DecompileBlueprintToPseudo(cookedMetaData));
        }

        var cpp = cppList.Count > 1 ? string.Join("\n\n", cppList) : cppList.FirstOrDefault() ?? string.Empty;
        if (entry.Path.Contains("_Verse.uasset"))
        {
            cpp = UnmangledCasedNameRegex().Replace(cpp, ""); // UnmangleCasedName
        }
        cpp = CallFuncReturnValueRegex().Replace(cpp, "$1");


        TabControl.SelectedTab.SetDocumentText(cpp, false, false);
    }

    private void SaveAndPlaySound(string fullPath, string ext, byte[] data)
    {
        if (fullPath.StartsWith('/'))
            fullPath = fullPath[1..];
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

    private readonly object _rawData = new();
    public void ExportData(GameFile entry, bool updateUi = true)
    {
        if (Provider.TrySavePackage(entry, out var assets))
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

            Log.Information("{FileName} successfully exported", entry.Name);
            if (updateUi)
            {
                FLogger.Append(ELog.Information, () =>
                {
                    FLogger.Text("Successfully exported ", Constants.WHITE);
                    FLogger.Link(entry.Name, path, true);
                });
            }
        }
        else
        {
            Log.Error("{FileName} could not be exported", entry.Name);
            if (updateUi)
                FLogger.Append(ELog.Error, () => FLogger.Text($"Could not export '{entry.Name}'", Constants.WHITE, true));
        }
    }

    private static bool HasFlag(EBulkType a, EBulkType b)
    {
        return (a & b) == b;
    }

    private static SKBitmap RenderSvg(Stream stream)
    {
        const int size = 512;
        var svg = new SkiaSharp.Extended.Svg.SKSvg();
        svg.Load(stream);
        var bmp = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bmp);
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.FilterQuality = SKFilterQuality.Medium;
        canvas.Clear(SKColors.Transparent);
        canvas.DrawPicture(svg.Picture, paint);
        return bmp;
    }
}
