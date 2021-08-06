using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.ApiEndpoints.Models;

namespace FModel.ViewModels
{
    public class SettingsViewModel : ViewModel
    {
        private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
        private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;
        private readonly DiscordHandler _discordHandler = DiscordService.DiscordHandler;

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
            set => SetProperty(ref _selectedPreset, value);
        }

        private EGame _selectedUeGame;
        public EGame SelectedUeGame
        {
            get => _selectedUeGame;
            set => SetProperty(ref _selectedUeGame, value);
        }

        private UE4Version _selectedUeVersion;
        public UE4Version SelectedUeVersion
        {
            get => _selectedUeVersion;
            set => SetProperty(ref _selectedUeVersion, value);
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

        private EEnabledDisabled _selectedDirectoryStructure;
        public EEnabledDisabled SelectedDirectoryStructure
        {
            get => _selectedDirectoryStructure;
            set => SetProperty(ref _selectedDirectoryStructure, value);
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

        private EEnabledDisabled _selectedCosmeticDisplayAsset;
        public EEnabledDisabled SelectedCosmeticDisplayAsset
        {
            get => _selectedCosmeticDisplayAsset;
            set => SetProperty(ref _selectedCosmeticDisplayAsset, value);
        }
        
        private EMeshFormat _selectedMeshExportFormat;
        public EMeshFormat SelectedMeshExportFormat
        {
            get => _selectedMeshExportFormat;
            set => SetProperty(ref _selectedMeshExportFormat, value);
        }
        
        private ELodFormat _selectedLodExportFormat;
        public ELodFormat SelectedLodExportFormat
        {
            get => _selectedLodExportFormat;
            set => SetProperty(ref _selectedLodExportFormat, value);
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
        public ReadOnlyObservableCollection<UE4Version> UeVersions { get; private set; }
        public ReadOnlyObservableCollection<ELanguage> AssetLanguages { get; private set; }
        public ReadOnlyObservableCollection<EAesReload> AesReloads { get; private set; }
        public ReadOnlyObservableCollection<EDiscordRpc> DiscordRpcs { get; private set; }
        public ReadOnlyObservableCollection<EEnabledDisabled> DirectoryStructures { get; private set; }
        public ReadOnlyObservableCollection<ECompressedAudio> CompressedAudios { get; private set; }
        public ReadOnlyObservableCollection<EIconStyle> CosmeticStyles { get; private set; }
        public ReadOnlyObservableCollection<EEnabledDisabled> CosmeticDisplayAssets { get; private set; }
        public ReadOnlyObservableCollection<EMeshFormat> MeshExportFormats { get; private set; }
        public ReadOnlyObservableCollection<ELodFormat> LodExportFormats { get; private set; }
        public ReadOnlyObservableCollection<ETextureFormat> TextureExportFormats { get; private set; }

        private readonly FGame _game;
        private Game _gamePreset;
        private string _outputSnapshot;
        private string _gameSnapshot;
        private EUpdateMode _updateModeSnapshot;
        private string _presetSnapshot;
        private EGame _ueGameSnapshot;
        private UE4Version _ueVersionSnapshot;
        private ELanguage _assetLanguageSnapshot;
        private EEnabledDisabled _directoryStructureSnapshot;
        private ECompressedAudio _compressedAudioSnapshot;
        private EIconStyle _cosmeticStyleSnapshot;
        private EEnabledDisabled _cosmeticDisplayAssetSnapshot;
        private EMeshFormat _meshExportFormatSnapshot;
        private ELodFormat _lodExportFormatSnapshot;
        private ETextureFormat _textureExportFormatSnapshot;

        public SettingsViewModel(FGame game)
        {
            _game = game;
        }

        public void Initialize()
        {
            _outputSnapshot = UserSettings.Default.OutputDirectory;
            _gameSnapshot = UserSettings.Default.GameDirectory;
            _updateModeSnapshot = UserSettings.Default.UpdateMode;
            _presetSnapshot = UserSettings.Default.Presets[_game];
            _ueGameSnapshot = UserSettings.Default.OverridedGame[_game];
            _ueVersionSnapshot = UserSettings.Default.OverridedUEVersion[_game];
            _assetLanguageSnapshot = UserSettings.Default.AssetLanguage;
            _directoryStructureSnapshot = UserSettings.Default.KeepDirectoryStructure;
            _compressedAudioSnapshot = UserSettings.Default.CompressedAudioMode;
            _cosmeticStyleSnapshot = UserSettings.Default.CosmeticStyle;
            _cosmeticDisplayAssetSnapshot = UserSettings.Default.CosmeticDisplayAsset;
            _meshExportFormatSnapshot = UserSettings.Default.MeshExportFormat;
            _lodExportFormatSnapshot = UserSettings.Default.LodExportFormat;
            _textureExportFormatSnapshot = UserSettings.Default.TextureExportFormat;

            SelectedUpdateMode = _updateModeSnapshot;
            SelectedPreset = _presetSnapshot;
            SelectedUeGame = _ueGameSnapshot;
            SelectedUeVersion = _ueVersionSnapshot;
            SelectedAssetLanguage = _assetLanguageSnapshot;
            SelectedDirectoryStructure = _directoryStructureSnapshot;
            SelectedCompressedAudio = _compressedAudioSnapshot;
            SelectedCosmeticStyle = _cosmeticStyleSnapshot;
            SelectedCosmeticDisplayAsset = _cosmeticDisplayAssetSnapshot;
            SelectedMeshExportFormat = _meshExportFormatSnapshot;
            SelectedLodExportFormat = _lodExportFormatSnapshot;
            SelectedTextureExportFormat = _textureExportFormatSnapshot;
            SelectedAesReload = UserSettings.Default.AesReload;
            SelectedDiscordRpc = UserSettings.Default.DiscordRpc;

            UpdateModes = new ReadOnlyObservableCollection<EUpdateMode>(new ObservableCollection<EUpdateMode>(EnumerateUpdateModes()));
            Presets = new ObservableCollection<string>(EnumeratePresets());
            UeGames = new ReadOnlyObservableCollection<EGame>(new ObservableCollection<EGame>(EnumerateUeGames()));
            UeVersions = new ReadOnlyObservableCollection<UE4Version>(new ObservableCollection<UE4Version>(EnumerateUeVersions()));
            AssetLanguages = new ReadOnlyObservableCollection<ELanguage>(new ObservableCollection<ELanguage>(EnumerateAssetLanguages()));
            AesReloads = new ReadOnlyObservableCollection<EAesReload>(new ObservableCollection<EAesReload>(EnumerateAesReloads()));
            DiscordRpcs = new ReadOnlyObservableCollection<EDiscordRpc>(new ObservableCollection<EDiscordRpc>(EnumerateDiscordRpcs()));
            DirectoryStructures = new ReadOnlyObservableCollection<EEnabledDisabled>(new ObservableCollection<EEnabledDisabled>(EnumerateEnabledDisabled()));
            CompressedAudios = new ReadOnlyObservableCollection<ECompressedAudio>(new ObservableCollection<ECompressedAudio>(EnumerateCompressedAudios()));
            CosmeticStyles = new ReadOnlyObservableCollection<EIconStyle>(new ObservableCollection<EIconStyle>(EnumerateCosmeticStyles()));
            CosmeticDisplayAssets = new ReadOnlyObservableCollection<EEnabledDisabled>(new ObservableCollection<EEnabledDisabled>(EnumerateEnabledDisabled()));
            MeshExportFormats = new ReadOnlyObservableCollection<EMeshFormat>(new ObservableCollection<EMeshFormat>(EnumerateMeshExportFormat()));
            LodExportFormats = new ReadOnlyObservableCollection<ELodFormat>(new ObservableCollection<ELodFormat>(EnumerateLodExportFormat()));
            TextureExportFormats = new ReadOnlyObservableCollection<ETextureFormat>(new ObservableCollection<ETextureFormat>(EnumerateTextureExportFormat()));
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
            SelectedUeVersion = (UE4Version)version.UeVer;
        }
        
        public void ResetPreset()
        {
            SelectedUeGame = _ueGameSnapshot;
            SelectedUeVersion = _ueVersionSnapshot;
        }

        public SettingsOut Save()
        {
            var ret = SettingsOut.Nothing;

            if (_ueGameSnapshot != SelectedUeGame || _ueVersionSnapshot != SelectedUeVersion || // comboboxes
                _outputSnapshot != UserSettings.Default.OutputDirectory || // textbox
                _gameSnapshot != UserSettings.Default.GameDirectory) // textbox
                ret = SettingsOut.Restart;

            if (_assetLanguageSnapshot != SelectedAssetLanguage)
                ret = SettingsOut.ReloadLocres;

            UserSettings.Default.UpdateMode = SelectedUpdateMode;
            UserSettings.Default.Presets[_game] = SelectedPreset;
            UserSettings.Default.OverridedGame[_game] = SelectedUeGame;
            UserSettings.Default.OverridedUEVersion[_game] = SelectedUeVersion;
            UserSettings.Default.AssetLanguage = SelectedAssetLanguage;
            UserSettings.Default.KeepDirectoryStructure = SelectedDirectoryStructure;
            UserSettings.Default.CompressedAudioMode = SelectedCompressedAudio;
            UserSettings.Default.CosmeticStyle = SelectedCosmeticStyle;
            UserSettings.Default.CosmeticDisplayAsset = SelectedCosmeticDisplayAsset;
            UserSettings.Default.MeshExportFormat = SelectedMeshExportFormat;
            UserSettings.Default.LodExportFormat = SelectedLodExportFormat;
            UserSettings.Default.TextureExportFormat = SelectedTextureExportFormat;
            UserSettings.Default.AesReload = SelectedAesReload;
            UserSettings.Default.DiscordRpc = SelectedDiscordRpc;

            if (SelectedDiscordRpc == EDiscordRpc.Never)
                _discordHandler.Shutdown();

            return ret;
        }

        private IEnumerable<EUpdateMode> EnumerateUpdateModes() => Enum.GetValues(SelectedUpdateMode.GetType()).Cast<EUpdateMode>();
        private IEnumerable<string> EnumeratePresets()
        {
            yield return "None";
        }
        private IEnumerable<EGame> EnumerateUeGames() => Enum.GetValues(SelectedUeGame.GetType()).Cast<EGame>();
        private IEnumerable<UE4Version> EnumerateUeVersions() => Enum.GetValues(SelectedUeVersion.GetType()).Cast<UE4Version>();
        private IEnumerable<ELanguage> EnumerateAssetLanguages() => Enum.GetValues(SelectedAssetLanguage.GetType()).Cast<ELanguage>();
        private IEnumerable<EAesReload> EnumerateAesReloads() => Enum.GetValues(SelectedAesReload.GetType()).Cast<EAesReload>();
        private IEnumerable<EDiscordRpc> EnumerateDiscordRpcs() => Enum.GetValues(SelectedDiscordRpc.GetType()).Cast<EDiscordRpc>();
        private IEnumerable<ECompressedAudio> EnumerateCompressedAudios() => Enum.GetValues(SelectedCompressedAudio.GetType()).Cast<ECompressedAudio>();
        private IEnumerable<EIconStyle> EnumerateCosmeticStyles() => Enum.GetValues(SelectedCosmeticStyle.GetType()).Cast<EIconStyle>();
        private IEnumerable<EEnabledDisabled> EnumerateEnabledDisabled() => Enum.GetValues(SelectedCosmeticDisplayAsset.GetType()).Cast<EEnabledDisabled>();
        private IEnumerable<EMeshFormat> EnumerateMeshExportFormat() => Enum.GetValues(SelectedMeshExportFormat.GetType()).Cast<EMeshFormat>();
        private IEnumerable<ELodFormat> EnumerateLodExportFormat() => Enum.GetValues(SelectedLodExportFormat.GetType()).Cast<ELodFormat>();
        private IEnumerable<ETextureFormat> EnumerateTextureExportFormat() => Enum.GetValues(SelectedTextureExportFormat.GetType()).Cast<ETextureFormat>();
    }
}