using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.UEFormat.Enums;
using CUE4Parse.UE4.Assets.Exports.Material;
using FModel.Extensions.Themes;
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

    private EGame _selectedUeGame;
    public EGame SelectedUeGame
    {
        get => _selectedUeGame;
        set => SetProperty(ref _selectedUeGame, value);
    }

    public bool CanGenerateMapping => UserSettings.Default.CurrentDir?.UeVersion >= EGame.GAME_UE4_18;

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

    private EUsmapCompressionMethod _selectedMappingCompression;
    public EUsmapCompressionMethod SelectedMappingCompression
    {
        get => _selectedMappingCompression;
        set => SetProperty(ref _selectedMappingCompression, value);
    }

    private bool _isGeneratingMapping;
    public bool IsGeneratingMapping
    {
        get => _isGeneratingMapping;
        set => SetProperty(ref _isGeneratingMapping, value);
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

    private EJsonHighlightTheme _selectedJsonHighlightTheme;
    public EJsonHighlightTheme SelectedJsonHighlightTheme
    {
        get => _selectedJsonHighlightTheme;
        set => SetProperty(ref _selectedJsonHighlightTheme, value);
    }

    private ulong _criwareDecryptionKey;
    public ulong CriwareDecryptionKey
    {
        get => _criwareDecryptionKey;
        set => SetProperty(ref _criwareDecryptionKey, value);
    }

    private string _unluacOpcodeMap;
    public string UnluacOpcodeMap
    {
        get => _unluacOpcodeMap;
        set => SetProperty(ref _unluacOpcodeMap, value);
    }

    public ExportOptionsViewModel Options { get; } = new(showExportImmediatelyOption: true);

    public ReadOnlyObservableCollection<EGame> UeGames { get; private set; }
    public ReadOnlyObservableCollection<ELanguage> AssetLanguages { get; private set; }
    public ReadOnlyObservableCollection<EAesReload> AesReloads { get; private set; }
    public ReadOnlyObservableCollection<EDiscordRpc> DiscordRpcs { get; private set; }
    public ReadOnlyObservableCollection<ECompressedAudio> CompressedAudios { get; private set; }
    public ReadOnlyObservableCollection<EUsmapCompressionMethod> MappingCompressions { get; private set; }
    public ReadOnlyObservableCollection<EIconStyle> CosmeticStyles { get; private set; }
    public ReadOnlyObservableCollection<EJsonHighlightTheme> JsonHighlightThemes { get; private set; }

    private string _gameSnapshot;
    private EGame _ueGameSnapshot;
    private IList<FCustomVersion> _customVersionsSnapshot;
    private IDictionary<string, bool> _optionsSnapshot;
    private IDictionary<string, KeyValuePair<string, string>> _mapStructTypesSnapshot;
    private ELanguage _assetLanguageSnapshot;

    public SettingsViewModel()
    {

    }

    public void Initialize()
    {
        _gameSnapshot = UserSettings.Default.GameDirectory;
        _ueGameSnapshot = UserSettings.Default.CurrentDir.UeVersion;
        _customVersionsSnapshot = UserSettings.Default.CurrentDir.Versioning.CustomVersions;
        _optionsSnapshot = UserSettings.Default.CurrentDir.Versioning.Options;
        _mapStructTypesSnapshot = UserSettings.Default.CurrentDir.Versioning.MapStructTypes;

        AesEndpoint = UserSettings.Default.CurrentDir.Endpoints[0];
        MappingEndpoint = UserSettings.Default.CurrentDir.Endpoints[1];

        _assetLanguageSnapshot = UserSettings.Default.AssetLanguage;

        SelectedUeGame = _ueGameSnapshot;
        SelectedCustomVersions = _customVersionsSnapshot;
        SelectedOptions = _optionsSnapshot;
        SelectedMapStructTypes = _mapStructTypesSnapshot;
        SelectedAssetLanguage = _assetLanguageSnapshot;
        SelectedCompressedAudio = UserSettings.Default.CompressedAudioMode;
        SelectedMappingCompression = UserSettings.Default.MappingCompression;
        SelectedCosmeticStyle = UserSettings.Default.CosmeticStyle;
        CriwareDecryptionKey = UserSettings.Default.CurrentDir.CriwareDecryptionKey;
        UnluacOpcodeMap = UserSettings.Default.CurrentDir.UnluacOpCodeMap;
        SelectedJsonHighlightTheme = UserSettings.Default.JsonHighlightTheme;
        SelectedAesReload = UserSettings.Default.AesReload;
        SelectedDiscordRpc = UserSettings.Default.DiscordRpc;

        UeGames = new ReadOnlyObservableCollection<EGame>(new ObservableCollection<EGame>(EnumerateUeGames()));
        AssetLanguages = new ReadOnlyObservableCollection<ELanguage>(new ObservableCollection<ELanguage>(EnumerateAssetLanguages()));
        AesReloads = new ReadOnlyObservableCollection<EAesReload>(new ObservableCollection<EAesReload>(EnumerateAesReloads()));
        DiscordRpcs = new ReadOnlyObservableCollection<EDiscordRpc>(new ObservableCollection<EDiscordRpc>(EnumerateDiscordRpcs()));
        CompressedAudios = new ReadOnlyObservableCollection<ECompressedAudio>(new ObservableCollection<ECompressedAudio>(EnumerateCompressedAudios()));
        MappingCompressions = new ReadOnlyObservableCollection<EUsmapCompressionMethod>(new ObservableCollection<EUsmapCompressionMethod>(EnumerateMappingCompressions()));
        CosmeticStyles = new ReadOnlyObservableCollection<EIconStyle>(new ObservableCollection<EIconStyle>(EnumerateCosmeticStyles()));
        JsonHighlightThemes = new ReadOnlyObservableCollection<EJsonHighlightTheme>(new ObservableCollection<EJsonHighlightTheme>(EnumerateJsonHighlightThemes()));
    }

    public bool Save(out List<SettingsOut> whatShouldIDo)
    {
        var restart = false;
        whatShouldIDo = new List<SettingsOut>();

        if (_assetLanguageSnapshot != SelectedAssetLanguage)
            whatShouldIDo.Add(SettingsOut.ReloadLocres);

        if (_ueGameSnapshot != SelectedUeGame || _customVersionsSnapshot != SelectedCustomVersions ||
            _optionsSnapshot != SelectedOptions || // combobox
            _mapStructTypesSnapshot != SelectedMapStructTypes ||
            _gameSnapshot != UserSettings.Default.GameDirectory) // textbox
            restart = true;

        UserSettings.Default.CurrentDir.UeVersion = SelectedUeGame;
        UserSettings.Default.CurrentDir.Versioning.CustomVersions = SelectedCustomVersions;
        UserSettings.Default.CurrentDir.Versioning.Options = SelectedOptions;
        UserSettings.Default.CurrentDir.Versioning.MapStructTypes = SelectedMapStructTypes;
        UserSettings.Default.CurrentDir.CriwareDecryptionKey = CriwareDecryptionKey;
        UserSettings.Default.CurrentDir.UnluacOpCodeMap = UnluacOpcodeMap;

        UserSettings.Default.AssetLanguage = SelectedAssetLanguage;
        UserSettings.Default.MappingCompression = SelectedMappingCompression;
        UserSettings.Default.CompressedAudioMode = SelectedCompressedAudio;
        UserSettings.Default.CosmeticStyle = SelectedCosmeticStyle;
        UserSettings.Default.AesReload = SelectedAesReload;
        UserSettings.Default.DiscordRpc = SelectedDiscordRpc;
        UserSettings.Default.JsonHighlightTheme = SelectedJsonHighlightTheme;

        Options.SaveAsUserDefaults();

        if (SelectedDiscordRpc == EDiscordRpc.Never)
            _discordHandler.Shutdown();

        return restart;
    }

    private IEnumerable<EGame> EnumerateUeGames()
        => Enum.GetValues<EGame>()
            .GroupBy(value => (int)value)
            .Select(group => group.First())
            .OrderBy(value => ((int)value & 0xFF) == 0);
    private IEnumerable<ELanguage> EnumerateAssetLanguages() => Enum.GetValues<ELanguage>();
    private IEnumerable<EAesReload> EnumerateAesReloads() => Enum.GetValues<EAesReload>();
    private IEnumerable<EDiscordRpc> EnumerateDiscordRpcs() => Enum.GetValues<EDiscordRpc>();
    private IEnumerable<ECompressedAudio> EnumerateCompressedAudios() => Enum.GetValues<ECompressedAudio>();
    private IEnumerable<EUsmapCompressionMethod> EnumerateMappingCompressions() => Enum.GetValues<EUsmapCompressionMethod>()
            .Where(value => value is not (EUsmapCompressionMethod.Oodle or EUsmapCompressionMethod.Unknown))
            .Reverse();
    private IEnumerable<EIconStyle> EnumerateCosmeticStyles() => Enum.GetValues<EIconStyle>();
    private IEnumerable<EJsonHighlightTheme> EnumerateJsonHighlightThemes() => Enum.GetValues<EJsonHighlightTheme>();
}
