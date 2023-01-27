using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Material;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;

namespace FModel.ViewModels;

public class SettingsViewModel : ViewModel
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;
    private readonly DiscordHandler _discordHandler = DiscordService.DiscordHandler;

    private bool _useCustomOutputFolders;
    public bool UseCustomOutputFolders
    {
        get => _useCustomOutputFolders;
        set => SetProperty(ref _useCustomOutputFolders, value);
    }

    private EUpdateMode _selectedUpdateMode;
    public EUpdateMode SelectedUpdateMode
    {
        get => _selectedUpdateMode;
        set => SetProperty(ref _selectedUpdateMode, value);
    }

    private string _selectedPreset;
    public string SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            SetProperty(ref _selectedPreset, value);
            RaisePropertyChanged("EnableElements");
        }
    }

    private ETexturePlatform _selectedUePlatform;
    public ETexturePlatform SelectedUePlatform
    {
        get => _selectedUePlatform;
        set => SetProperty(ref _selectedUePlatform, value);
    }

    private EGame _selectedUeGame;
    public EGame SelectedUeGame
    {
        get => _selectedUeGame;
        set => SetProperty(ref _selectedUeGame, value);
    }

    private List<FCustomVersion> _selectedCustomVersions;
    public List<FCustomVersion> SelectedCustomVersions
    {
        get => _selectedCustomVersions;
        set => SetProperty(ref _selectedCustomVersions, value);
    }

    private Dictionary<string, bool> _selectedOptions;
    public Dictionary<string, bool> SelectedOptions
    {
        get => _selectedOptions;
        set => SetProperty(ref _selectedOptions, value);
    }

    private Dictionary<string, KeyValuePair<string, string>> _selectedMapStructTypes;
    public Dictionary<string, KeyValuePair<string, string>> SelectedMapStructTypes
    {
        get => _selectedMapStructTypes;
        set => SetProperty(ref _selectedMapStructTypes, value);
    }

    private FEndpoint _aesEndpoint;
    public FEndpoint AesEndpoint
    {
        get => _aesEndpoint;
        set => SetProperty(ref _aesEndpoint, value);
    }

    private FEndpoint _mappingEndpoint;
    public FEndpoint MappingEndpoint
    {
        get => _mappingEndpoint;
        set => SetProperty(ref _mappingEndpoint, value);
    }

    private ELanguage _selectedAssetLanguage;
    public ELanguage SelectedAssetLanguage
    {
        get => _selectedAssetLanguage;
        set => SetProperty(ref _selectedAssetLanguage, value);
    }

    private EAesReload _selectedAesReload;
    public EAesReload SelectedAesReload
    {
        get => _selectedAesReload;
        set => SetProperty(ref _selectedAesReload, value);
    }

    private EDiscordRpc _selectedDiscordRpc;
    public EDiscordRpc SelectedDiscordRpc
    {
        get => _selectedDiscordRpc;
        set => SetProperty(ref _selectedDiscordRpc, value);
    }

    private ECompressedAudio _selectedCompressedAudio;
    public ECompressedAudio SelectedCompressedAudio
    {
        get => _selectedCompressedAudio;
        set => SetProperty(ref _selectedCompressedAudio, value);
    }

    private EIconStyle _selectedCosmeticStyle;
    public EIconStyle SelectedCosmeticStyle
    {
        get => _selectedCosmeticStyle;
        set => SetProperty(ref _selectedCosmeticStyle, value);
    }

    private EMeshFormat _selectedMeshExportFormat;
    public EMeshFormat SelectedMeshExportFormat
    {
        get => _selectedMeshExportFormat;
        set => SetProperty(ref _selectedMeshExportFormat, value);
    }

    private ESocketFormat _selectedSocketExportFormat;
    public ESocketFormat SelectedSocketExportFormat
    {
        get => _selectedSocketExportFormat;
        set => SetProperty(ref _selectedSocketExportFormat, value);
    }

    private ELodFormat _selectedLodExportFormat;
    public ELodFormat SelectedLodExportFormat
    {
        get => _selectedLodExportFormat;
        set => SetProperty(ref _selectedLodExportFormat, value);
    }

    private EMaterialFormat _selectedMaterialExportFormat;
    public EMaterialFormat SelectedMaterialExportFormat
    {
        get => _selectedMaterialExportFormat;
        set => SetProperty(ref _selectedMaterialExportFormat, value);
    }

    private ETextureFormat _selectedTextureExportFormat;
    public ETextureFormat SelectedTextureExportFormat
    {
        get => _selectedTextureExportFormat;
        set => SetProperty(ref _selectedTextureExportFormat, value);
    }

    public ReadOnlyObservableCollection<EUpdateMode> UpdateModes { get; private set; }
    public ObservableCollection<string> Presets { get; private set; }
    public ReadOnlyObservableCollection<EGame> UeGames { get; private set; }
    public ReadOnlyObservableCollection<ELanguage> AssetLanguages { get; private set; }
    public ReadOnlyObservableCollection<EAesReload> AesReloads { get; private set; }
    public ReadOnlyObservableCollection<EDiscordRpc> DiscordRpcs { get; private set; }
    public ReadOnlyObservableCollection<ECompressedAudio> CompressedAudios { get; private set; }
    public ReadOnlyObservableCollection<EIconStyle> CosmeticStyles { get; private set; }
    public ReadOnlyObservableCollection<EMeshFormat> MeshExportFormats { get; private set; }
    public ReadOnlyObservableCollection<ESocketFormat> SocketExportFormats { get; private set; }
    public ReadOnlyObservableCollection<ELodFormat> LodExportFormats { get; private set; }
    public ReadOnlyObservableCollection<EMaterialFormat> MaterialExportFormats { get; private set; }
    public ReadOnlyObservableCollection<ETextureFormat> TextureExportFormats { get; private set; }
    public ReadOnlyObservableCollection<ETexturePlatform> Platforms { get; private set; }

    public bool EnableElements => SelectedPreset == Constants._NO_PRESET_TRIGGER;

    private readonly FGame _game;
    private Game _gamePreset;
    private string _outputSnapshot;
    private string _rawDataSnapshot;
    private string _propertiesSnapshot;
    private string _textureSnapshot;
    private string _audioSnapshot;
    private string _modelSnapshot;
    private string _gameSnapshot;
    private EUpdateMode _updateModeSnapshot;
    private string _presetSnapshot;
    private ETexturePlatform _uePlatformSnapshot;
    private EGame _ueGameSnapshot;
    private List<FCustomVersion> _customVersionsSnapshot;
    private Dictionary<string, bool> _optionsSnapshot;
    private Dictionary<string, KeyValuePair<string, string>> _mapStructTypesSnapshot;
    private ELanguage _assetLanguageSnapshot;
    private ECompressedAudio _compressedAudioSnapshot;
    private EIconStyle _cosmeticStyleSnapshot;
    private EMeshFormat _meshExportFormatSnapshot;
    private ESocketFormat _socketExportFormatSnapshot;
    private ELodFormat _lodExportFormatSnapshot;
    private EMaterialFormat _materialExportFormatSnapshot;
    private ETextureFormat _textureExportFormatSnapshot;

    private bool _mappingsUpdate = false;

    public SettingsViewModel(FGame game)
    {
        _game = game;
    }

    public void Initialize()
    {
        _outputSnapshot = UserSettings.Default.OutputDirectory;
        _rawDataSnapshot = UserSettings.Default.RawDataDirectory;
        _propertiesSnapshot = UserSettings.Default.PropertiesDirectory;
        _textureSnapshot = UserSettings.Default.TextureDirectory;
        _audioSnapshot = UserSettings.Default.AudioDirectory;
        _modelSnapshot = UserSettings.Default.ModelDirectory;
        _gameSnapshot = UserSettings.Default.GameDirectory;
        _updateModeSnapshot = UserSettings.Default.UpdateMode;
        _presetSnapshot = UserSettings.Default.Presets[_game];
        _uePlatformSnapshot = UserSettings.Default.OverridedPlatform;
        if (_game == FGame.Unknown && UserSettings.Default.ManualGames.TryGetValue(_gameSnapshot, out var settings))
        {
            _ueGameSnapshot = settings.OverridedGame;
            _customVersionsSnapshot = settings.OverridedCustomVersions;
            _optionsSnapshot = settings.OverridedOptions;
            _mapStructTypesSnapshot = settings.OverridedMapStructTypes;
        }
        else
        {
            _ueGameSnapshot = UserSettings.Default.OverridedGame[_game];
            _customVersionsSnapshot = UserSettings.Default.OverridedCustomVersions[_game];
            _optionsSnapshot = UserSettings.Default.OverridedOptions[_game];
            _mapStructTypesSnapshot = UserSettings.Default.OverridedMapStructTypes[_game];
        }

        if (UserSettings.Default.CustomEndpoints.TryGetValue(_game, out var endpoints))
        {
            AesEndpoint = endpoints[0];
            MappingEndpoint = endpoints[1];
            MappingEndpoint.PropertyChanged += (_, args) =>
            {
                if (!_mappingsUpdate)
                    _mappingsUpdate = args.PropertyName is "Overwrite" or "FilePath";
            };
        }

        _assetLanguageSnapshot = UserSettings.Default.AssetLanguage;
        _compressedAudioSnapshot = UserSettings.Default.CompressedAudioMode;
        _cosmeticStyleSnapshot = UserSettings.Default.CosmeticStyle;
        _meshExportFormatSnapshot = UserSettings.Default.MeshExportFormat;
        _socketExportFormatSnapshot = UserSettings.Default.SocketExportFormat;
        _lodExportFormatSnapshot = UserSettings.Default.LodExportFormat;
        _materialExportFormatSnapshot = UserSettings.Default.MaterialExportFormat;
        _textureExportFormatSnapshot = UserSettings.Default.TextureExportFormat;

        SelectedUpdateMode = _updateModeSnapshot;
        SelectedPreset = _presetSnapshot;
        SelectedUePlatform = _uePlatformSnapshot;
        SelectedUeGame = _ueGameSnapshot;
        SelectedCustomVersions = _customVersionsSnapshot;
        SelectedOptions = _optionsSnapshot;
        SelectedMapStructTypes = _mapStructTypesSnapshot;
        SelectedAssetLanguage = _assetLanguageSnapshot;
        SelectedCompressedAudio = _compressedAudioSnapshot;
        SelectedCosmeticStyle = _cosmeticStyleSnapshot;
        SelectedMeshExportFormat = _meshExportFormatSnapshot;
        SelectedSocketExportFormat = _socketExportFormatSnapshot;
        SelectedLodExportFormat = _lodExportFormatSnapshot;
        SelectedMaterialExportFormat = _materialExportFormatSnapshot;
        SelectedTextureExportFormat = _textureExportFormatSnapshot;
        SelectedAesReload = UserSettings.Default.AesReload;
        SelectedDiscordRpc = UserSettings.Default.DiscordRpc;

        UpdateModes = new ReadOnlyObservableCollection<EUpdateMode>(new ObservableCollection<EUpdateMode>(EnumerateUpdateModes()));
        Presets = new ObservableCollection<string>(EnumeratePresets());
        UeGames = new ReadOnlyObservableCollection<EGame>(new ObservableCollection<EGame>(EnumerateUeGames()));
        AssetLanguages = new ReadOnlyObservableCollection<ELanguage>(new ObservableCollection<ELanguage>(EnumerateAssetLanguages()));
        AesReloads = new ReadOnlyObservableCollection<EAesReload>(new ObservableCollection<EAesReload>(EnumerateAesReloads()));
        DiscordRpcs = new ReadOnlyObservableCollection<EDiscordRpc>(new ObservableCollection<EDiscordRpc>(EnumerateDiscordRpcs()));
        CompressedAudios = new ReadOnlyObservableCollection<ECompressedAudio>(new ObservableCollection<ECompressedAudio>(EnumerateCompressedAudios()));
        CosmeticStyles = new ReadOnlyObservableCollection<EIconStyle>(new ObservableCollection<EIconStyle>(EnumerateCosmeticStyles()));
        MeshExportFormats = new ReadOnlyObservableCollection<EMeshFormat>(new ObservableCollection<EMeshFormat>(EnumerateMeshExportFormat()));
        SocketExportFormats = new ReadOnlyObservableCollection<ESocketFormat>(new ObservableCollection<ESocketFormat>(EnumerateSocketExportFormat()));
        LodExportFormats = new ReadOnlyObservableCollection<ELodFormat>(new ObservableCollection<ELodFormat>(EnumerateLodExportFormat()));
        MaterialExportFormats = new ReadOnlyObservableCollection<EMaterialFormat>(new ObservableCollection<EMaterialFormat>(EnumerateMaterialExportFormat()));
        TextureExportFormats = new ReadOnlyObservableCollection<ETextureFormat>(new ObservableCollection<ETextureFormat>(EnumerateTextureExportFormat()));
        Platforms = new ReadOnlyObservableCollection<ETexturePlatform>(new ObservableCollection<ETexturePlatform>(EnumerateUePlatforms()));
    }

    public async Task InitPresets(string gameName)
    {
        await _threadWorkerView.Begin(cancellationToken =>
        {
            if (string.IsNullOrEmpty(gameName)) return;
            _gamePreset = _apiEndpointView.FModelApi.GetGames(cancellationToken, gameName);
        });

        if (_gamePreset?.Versions == null) return;
        foreach (var version in _gamePreset.Versions.Keys)
        {
            Presets.Add(version);
        }
    }

    public void SwitchPreset(string key)
    {
        if (_gamePreset?.Versions == null || !_gamePreset.Versions.TryGetValue(key, out var version)) return;
        SelectedUeGame = version.GameEnum.ToEnum(EGame.GAME_UE4_LATEST);

        SelectedCustomVersions = new List<FCustomVersion>();
        foreach (var (guid, v) in version.CustomVersions)
        {
            SelectedCustomVersions.Add(new FCustomVersion { Key = new FGuid(guid), Version = v });
        }

        SelectedOptions = new Dictionary<string, bool>();
        foreach (var (k, v) in version.Options)
        {
            SelectedOptions[k] = v;
        }

        SelectedMapStructTypes = new Dictionary<string, KeyValuePair<string, string>>();
        foreach (var (k, v) in version.MapStructTypes)
        {
            SelectedMapStructTypes[k] = v;
        }
    }

    public void ResetPreset()
    {
        SelectedUeGame = _ueGameSnapshot;
        SelectedCustomVersions = _customVersionsSnapshot;
        SelectedOptions = _optionsSnapshot;
        SelectedMapStructTypes = _mapStructTypesSnapshot;
    }

    public bool Save(out List<SettingsOut> whatShouldIDo)
    {
        var restart = false;
        whatShouldIDo = new List<SettingsOut>();

        if (_assetLanguageSnapshot != SelectedAssetLanguage)
            whatShouldIDo.Add(SettingsOut.ReloadLocres);
        if (_mappingsUpdate)
            whatShouldIDo.Add(SettingsOut.ReloadMappings);
        if (_updateModeSnapshot != SelectedUpdateMode)
            whatShouldIDo.Add(SettingsOut.CheckForUpdates);

        if (_ueGameSnapshot != SelectedUeGame || _customVersionsSnapshot != SelectedCustomVersions ||
            _uePlatformSnapshot != SelectedUePlatform || _optionsSnapshot != SelectedOptions || // combobox
            _mapStructTypesSnapshot != SelectedMapStructTypes ||
            _outputSnapshot != UserSettings.Default.OutputDirectory || // textbox
            _rawDataSnapshot != UserSettings.Default.RawDataDirectory || // textbox
            _propertiesSnapshot != UserSettings.Default.PropertiesDirectory || // textbox
            _textureSnapshot != UserSettings.Default.TextureDirectory || // textbox
            _audioSnapshot != UserSettings.Default.AudioDirectory || // textbox
            _modelSnapshot != UserSettings.Default.ModelDirectory || // textbox
            _gameSnapshot != UserSettings.Default.GameDirectory) // textbox
            restart = true;

        UserSettings.Default.UpdateMode = SelectedUpdateMode;
        UserSettings.Default.Presets[_game] = SelectedPreset;
        UserSettings.Default.OverridedPlatform = SelectedUePlatform;
        if (_game == FGame.Unknown && UserSettings.Default.ManualGames.ContainsKey(UserSettings.Default.GameDirectory))
        {
            UserSettings.Default.ManualGames[UserSettings.Default.GameDirectory].OverridedGame = SelectedUeGame;
            UserSettings.Default.ManualGames[UserSettings.Default.GameDirectory].OverridedCustomVersions = SelectedCustomVersions;
            UserSettings.Default.ManualGames[UserSettings.Default.GameDirectory].OverridedOptions = SelectedOptions;
            UserSettings.Default.ManualGames[UserSettings.Default.GameDirectory].OverridedMapStructTypes = SelectedMapStructTypes;
        }
        else
        {
            UserSettings.Default.OverridedGame[_game] = SelectedUeGame;
            UserSettings.Default.OverridedCustomVersions[_game] = SelectedCustomVersions;
            UserSettings.Default.OverridedOptions[_game] = SelectedOptions;
            UserSettings.Default.OverridedMapStructTypes[_game] = SelectedMapStructTypes;
        }

        UserSettings.Default.AssetLanguage = SelectedAssetLanguage;
        UserSettings.Default.CompressedAudioMode = SelectedCompressedAudio;
        UserSettings.Default.CosmeticStyle = SelectedCosmeticStyle;
        UserSettings.Default.MeshExportFormat = SelectedMeshExportFormat;
        UserSettings.Default.SocketExportFormat = SelectedSocketExportFormat;
        UserSettings.Default.LodExportFormat = SelectedLodExportFormat;
        UserSettings.Default.MaterialExportFormat = SelectedMaterialExportFormat;
        UserSettings.Default.TextureExportFormat = SelectedTextureExportFormat;
        UserSettings.Default.AesReload = SelectedAesReload;
        UserSettings.Default.DiscordRpc = SelectedDiscordRpc;

        if (SelectedDiscordRpc == EDiscordRpc.Never)
            _discordHandler.Shutdown();

        return restart;
    }

    private IEnumerable<EUpdateMode> EnumerateUpdateModes() => Enum.GetValues<EUpdateMode>();
    private IEnumerable<string> EnumeratePresets()
    {
        yield return Constants._NO_PRESET_TRIGGER;
    }
    private IEnumerable<EGame> EnumerateUeGames() => Enum.GetValues<EGame>();
    private IEnumerable<ELanguage> EnumerateAssetLanguages() => Enum.GetValues<ELanguage>();
    private IEnumerable<EAesReload> EnumerateAesReloads() => Enum.GetValues<EAesReload>();
    private IEnumerable<EDiscordRpc> EnumerateDiscordRpcs() => Enum.GetValues<EDiscordRpc>();
    private IEnumerable<ECompressedAudio> EnumerateCompressedAudios() => Enum.GetValues<ECompressedAudio>();
    private IEnumerable<EIconStyle> EnumerateCosmeticStyles() => Enum.GetValues<EIconStyle>();
    private IEnumerable<EMeshFormat> EnumerateMeshExportFormat() => Enum.GetValues<EMeshFormat>();
    private IEnumerable<ESocketFormat> EnumerateSocketExportFormat() => Enum.GetValues<ESocketFormat>();
    private IEnumerable<ELodFormat> EnumerateLodExportFormat() => Enum.GetValues<ELodFormat>();
    private IEnumerable<EMaterialFormat> EnumerateMaterialExportFormat() => Enum.GetValues<EMaterialFormat>();
    private IEnumerable<ETextureFormat> EnumerateTextureExportFormat() => Enum.GetValues<ETextureFormat>();
    private IEnumerable<ETexturePlatform> EnumerateUePlatforms() => Enum.GetValues<ETexturePlatform>();
}
