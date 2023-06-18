using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Material;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;

namespace FModel.ViewModels;

public class SettingsViewModel : ViewModel
{
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

    private IList<FCustomVersion> _selectedCustomVersions;
    public IList<FCustomVersion> SelectedCustomVersions
    {
        get => _selectedCustomVersions;
        set => SetProperty(ref _selectedCustomVersions, value);
    }

    private IDictionary<string, bool> _selectedOptions;
    public IDictionary<string, bool> SelectedOptions
    {
        get => _selectedOptions;
        set => SetProperty(ref _selectedOptions, value);
    }

    private IDictionary<string, KeyValuePair<string, string>> _selectedMapStructTypes;
    public IDictionary<string, KeyValuePair<string, string>> SelectedMapStructTypes
    {
        get => _selectedMapStructTypes;
        set => SetProperty(ref _selectedMapStructTypes, value);
    }

    private EndpointSettings _aesEndpoint;
    public EndpointSettings AesEndpoint
    {
        get => _aesEndpoint;
        set => SetProperty(ref _aesEndpoint, value);
    }

    private EndpointSettings _mappingEndpoint;
    public EndpointSettings MappingEndpoint
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

    private string _outputSnapshot;
    private string _rawDataSnapshot;
    private string _propertiesSnapshot;
    private string _textureSnapshot;
    private string _audioSnapshot;
    private string _modelSnapshot;
    private string _gameSnapshot;
    private EUpdateMode _updateModeSnapshot;
    private ETexturePlatform _uePlatformSnapshot;
    private EGame _ueGameSnapshot;
    private IList<FCustomVersion> _customVersionsSnapshot;
    private IDictionary<string, bool> _optionsSnapshot;
    private IDictionary<string, KeyValuePair<string, string>> _mapStructTypesSnapshot;
    private ELanguage _assetLanguageSnapshot;
    private ECompressedAudio _compressedAudioSnapshot;
    private EIconStyle _cosmeticStyleSnapshot;
    private EMeshFormat _meshExportFormatSnapshot;
    private ESocketFormat _socketExportFormatSnapshot;
    private ELodFormat _lodExportFormatSnapshot;
    private EMaterialFormat _materialExportFormatSnapshot;
    private ETextureFormat _textureExportFormatSnapshot;

    private bool _mappingsUpdate = false;

    public SettingsViewModel()
    {

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
        _uePlatformSnapshot = UserSettings.Default.CurrentDir.TexturePlatform;
        _ueGameSnapshot = UserSettings.Default.CurrentDir.UeVersion;
        _customVersionsSnapshot = UserSettings.Default.CurrentDir.Versioning.CustomVersions;
        _optionsSnapshot = UserSettings.Default.CurrentDir.Versioning.Options;
        _mapStructTypesSnapshot = UserSettings.Default.CurrentDir.Versioning.MapStructTypes;

        AesEndpoint = UserSettings.Default.CurrentDir.Endpoints[0];
        MappingEndpoint = UserSettings.Default.CurrentDir.Endpoints[1];
        MappingEndpoint.PropertyChanged += (_, args) =>
        {
            if (!_mappingsUpdate)
                _mappingsUpdate = args.PropertyName is "Overwrite" or "FilePath";
        };

        _assetLanguageSnapshot = UserSettings.Default.AssetLanguage;
        _compressedAudioSnapshot = UserSettings.Default.CompressedAudioMode;
        _cosmeticStyleSnapshot = UserSettings.Default.CosmeticStyle;
        _meshExportFormatSnapshot = UserSettings.Default.MeshExportFormat;
        _socketExportFormatSnapshot = UserSettings.Default.SocketExportFormat;
        _lodExportFormatSnapshot = UserSettings.Default.LodExportFormat;
        _materialExportFormatSnapshot = UserSettings.Default.MaterialExportFormat;
        _textureExportFormatSnapshot = UserSettings.Default.TextureExportFormat;

        SelectedUpdateMode = _updateModeSnapshot;
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
        UserSettings.Default.CurrentDir.UeVersion = SelectedUeGame;
        UserSettings.Default.CurrentDir.TexturePlatform = SelectedUePlatform;
        UserSettings.Default.CurrentDir.Versioning.CustomVersions = SelectedCustomVersions;
        UserSettings.Default.CurrentDir.Versioning.Options = SelectedOptions;
        UserSettings.Default.CurrentDir.Versioning.MapStructTypes = SelectedMapStructTypes;

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
