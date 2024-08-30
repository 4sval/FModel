using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse.UE4.Assets.Exports.Material;
using FModel.Framework;
using FModel.ViewModels;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.Views.Snooper;
using Newtonsoft.Json;

namespace FModel.Settings
{
    public sealed class UserSettings : ViewModel
    {
        public static UserSettings Default { get; set; }
#if DEBUG
        public static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FModel", "AppSettings_Debug.json");
#else
        public static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FModel", "AppSettings.json");
#endif

        static UserSettings()
        {
            Default = new UserSettings();
        }

        private static bool _bSave = true;
        public static void Save()
        {
            if (!_bSave || Default == null) return;
            Default.PerDirectory[Default.CurrentDir.GameDirectory] = Default.CurrentDir;
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(Default, Formatting.Indented));
        }

        public static void Delete()
        {
            if (File.Exists(FilePath))
            {
                _bSave = false;
                File.Delete(FilePath);
            }
        }

        public static bool IsEndpointValid(EEndpointType type, out EndpointSettings endpoint)
        {
            endpoint = Default.CurrentDir.Endpoints[(int) type];
            return endpoint.Overwrite || endpoint.IsValid;
        }

        [JsonIgnore]
        public ExporterOptions ExportOptions => new()
        {
            LodFormat = Default.LodExportFormat,
            MeshFormat = Default.MeshExportFormat,
            AnimFormat = Default.MeshExportFormat switch
            {
                EMeshFormat.UEFormat => EAnimFormat.UEFormat,
                _ => EAnimFormat.ActorX
            },
            MaterialFormat = Default.MaterialExportFormat,
            TextureFormat = Default.TextureExportFormat,
            SocketFormat = Default.SocketExportFormat,
            CompressionFormat = Default.CompressionFormat,
            Platform = Default.CurrentDir.TexturePlatform,
            ExportMorphTargets = Default.SaveMorphTargets,
            ExportMaterials = Default.SaveEmbeddedMaterials
        };

        private bool _showChangelog = true;
        public bool ShowChangelog
        {
            get => _showChangelog;
            set => SetProperty(ref _showChangelog, value);
        }

        private string _outputDirectory;
        public string OutputDirectory
        {
            get => _outputDirectory;
            set => SetProperty(ref _outputDirectory, value);
        }

        private string _rawDataDirectory;
        public string RawDataDirectory
        {
            get => _rawDataDirectory;
            set => SetProperty(ref _rawDataDirectory, value);
        }

        private string _propertiesDirectory;
        public string PropertiesDirectory
        {
            get => _propertiesDirectory;
            set => SetProperty(ref _propertiesDirectory, value);
        }

        private string _textureDirectory;
        public string TextureDirectory
        {
            get => _textureDirectory;
            set => SetProperty(ref _textureDirectory, value);
        }

        private string _audioDirectory;
        public string AudioDirectory
        {
            get => _audioDirectory;
            set => SetProperty(ref _audioDirectory, value);
        }

        private string _modelDirectory;
        public string ModelDirectory
        {
            get => _modelDirectory;
            set => SetProperty(ref _modelDirectory, value);
        }

        private string _gameDirectory = string.Empty;
        public string GameDirectory
        {
            get => _gameDirectory;
            set => SetProperty(ref _gameDirectory, value);
        }

        private int _lastOpenedSettingTab;
        public int LastOpenedSettingTab
        {
            get => _lastOpenedSettingTab;
            set => SetProperty(ref _lastOpenedSettingTab, value);
        }

        private bool _isAutoOpenSounds = true;
        public bool IsAutoOpenSounds
        {
            get => _isAutoOpenSounds;
            set => SetProperty(ref _isAutoOpenSounds, value);
        }

        private bool _isLoggerExpanded = true;
        public bool IsLoggerExpanded
        {
            get => _isLoggerExpanded;
            set => SetProperty(ref _isLoggerExpanded, value);
        }

        private GridLength _avalonImageSize = new (200);
        public GridLength AvalonImageSize
        {
            get => _avalonImageSize;
            set => SetProperty(ref _avalonImageSize, value);
        }

        private string _audioDeviceId;
        public string AudioDeviceId
        {
            get => _audioDeviceId;
            set => SetProperty(ref _audioDeviceId, value);
        }

        private float _audioPlayerVolume = 50.0F;
        public float AudioPlayerVolume
        {
            get => _audioPlayerVolume;
            set => SetProperty(ref _audioPlayerVolume, value);
        }

        private ELoadingMode _loadingMode = ELoadingMode.All;
        public ELoadingMode LoadingMode
        {
            get => _loadingMode;
            set => SetProperty(ref _loadingMode, value);
        }

        private EUpdateMode _updateMode = EUpdateMode.Beta;
        public EUpdateMode UpdateMode
        {
            get => _updateMode;
            set => SetProperty(ref _updateMode, value);
        }

        private string _commitHash = Constants.APP_VERSION;
        public string CommitHash
        {
            get => _commitHash;
            set => SetProperty(ref _commitHash, value);
        }

        private bool _keepDirectoryStructure = true;
        public bool KeepDirectoryStructure
        {
            get => _keepDirectoryStructure;
            set => SetProperty(ref _keepDirectoryStructure, value);
        }

        private ECompressedAudio _compressedAudioMode = ECompressedAudio.PlayDecompressed;
        public ECompressedAudio CompressedAudioMode
        {
            get => _compressedAudioMode;
            set => SetProperty(ref _compressedAudioMode, value);
        }

        private EAesReload _aesReload = EAesReload.OncePerDay;
        public EAesReload AesReload
        {
            get => _aesReload;
            set => SetProperty(ref _aesReload, value);
        }

        private EDiscordRpc _discordRpc = EDiscordRpc.Always;
        public EDiscordRpc DiscordRpc
        {
            get => _discordRpc;
            set => SetProperty(ref _discordRpc, value);
        }

        private ELanguage _assetLanguage = ELanguage.English;
        public ELanguage AssetLanguage
        {
            get => _assetLanguage;
            set => SetProperty(ref _assetLanguage, value);
        }

        private EIconStyle _cosmeticStyle = EIconStyle.Default;
        public EIconStyle CosmeticStyle
        {
            get => _cosmeticStyle;
            set => SetProperty(ref _cosmeticStyle, value);
        }

        private bool _cosmeticDisplayAsset;
        public bool CosmeticDisplayAsset
        {
            get => _cosmeticDisplayAsset;
            set => SetProperty(ref _cosmeticDisplayAsset, value);
        }

        private int _imageMergerMargin = 5;
        public int ImageMergerMargin
        {
            get => _imageMergerMargin;
            set => SetProperty(ref _imageMergerMargin, value);
        }

        private bool _readScriptData;
        public bool ReadScriptData
        {
            get => _readScriptData;
            set => SetProperty(ref _readScriptData, value);
        }

        private IDictionary<string, DirectorySettings> _perDirectory = new Dictionary<string, DirectorySettings>();
        public IDictionary<string, DirectorySettings> PerDirectory
        {
            get => _perDirectory;
            set => SetProperty(ref _perDirectory, value);
        }

        [JsonIgnore]
        public DirectorySettings CurrentDir { get; set; }
        [JsonIgnore]
        public string ShortCommitHash => CommitHash[..7];

        /// <summary>
        /// TO DELETEEEEEEEEEEEEE
        /// </summary>
        private IDictionary<string, GameSelectorViewModel.DetectedGame> _manualGames = new Dictionary<string, GameSelectorViewModel.DetectedGame>();
        public IDictionary<string, GameSelectorViewModel.DetectedGame> ManualGames
        {
            get => _manualGames;
            set => SetProperty(ref _manualGames, value);
        }

        private AuthResponse _lastAuthResponse = new() {AccessToken = "", ExpiresAt = DateTime.Now};
        public AuthResponse LastAuthResponse
        {
            get => _lastAuthResponse;
            set => SetProperty(ref _lastAuthResponse, value);
        }

        private Hotkey _dirLeftTab = new(Key.A);
        public Hotkey DirLeftTab
        {
            get => _dirLeftTab;
            set => SetProperty(ref _dirLeftTab, value);
        }

        private Hotkey _dirRightTab = new(Key.D);
        public Hotkey DirRightTab
        {
            get => _dirRightTab;
            set => SetProperty(ref _dirRightTab, value);
        }

        private Hotkey _assetLeftTab = new(Key.Q);
        public Hotkey AssetLeftTab
        {
            get => _assetLeftTab;
            set => SetProperty(ref _assetLeftTab, value);
        }

        private Hotkey _assetRightTab = new(Key.E);
        public Hotkey AssetRightTab
        {
            get => _assetRightTab;
            set => SetProperty(ref _assetRightTab, value);
        }

        private Hotkey _assetAddTab = new(Key.T, ModifierKeys.Control);
        public Hotkey AssetAddTab
        {
            get => _assetAddTab;
            set => SetProperty(ref _assetAddTab, value);
        }

        private Hotkey _assetRemoveTab = new(Key.W, ModifierKeys.Control);
        public Hotkey AssetRemoveTab
        {
            get => _assetRemoveTab;
            set => SetProperty(ref _assetRemoveTab, value);
        }

        private Hotkey _addAudio = new(Key.N, ModifierKeys.Control);
        public Hotkey AddAudio
        {
            get => _addAudio;
            set => SetProperty(ref _addAudio, value);
        }

        private Hotkey _playPauseAudio = new(Key.K);
        public Hotkey PlayPauseAudio
        {
            get => _playPauseAudio;
            set => SetProperty(ref _playPauseAudio, value);
        }

        private Hotkey _previousAudio = new(Key.J);
        public Hotkey PreviousAudio
        {
            get => _previousAudio;
            set => SetProperty(ref _previousAudio, value);
        }

        private Hotkey _nextAudio = new(Key.L);
        public Hotkey NextAudio
        {
            get => _nextAudio;
            set => SetProperty(ref _nextAudio, value);
        }

        private EMeshFormat _meshExportFormat = EMeshFormat.ActorX;
        public EMeshFormat MeshExportFormat
        {
            get => _meshExportFormat;
            set => SetProperty(ref _meshExportFormat, value);
        }

        private EMaterialFormat _materialExportFormat = EMaterialFormat.FirstLayer;
        public EMaterialFormat MaterialExportFormat
        {
            get => _materialExportFormat;
            set => SetProperty(ref _materialExportFormat, value);
        }

        private ETextureFormat _textureExportFormat = ETextureFormat.Png;
        public ETextureFormat TextureExportFormat
        {
            get => _textureExportFormat;
            set => SetProperty(ref _textureExportFormat, value);
        }

        private ESocketFormat _socketExportFormat = ESocketFormat.Bone;
        public ESocketFormat SocketExportFormat
        {
            get => _socketExportFormat;
            set => SetProperty(ref _socketExportFormat, value);
        }

        private EFileCompressionFormat _compressionFormat = EFileCompressionFormat.ZSTD;
        public EFileCompressionFormat CompressionFormat
        {
            get => _compressionFormat;
            set => SetProperty(ref _compressionFormat, value);
        }

        private ELodFormat _lodExportFormat = ELodFormat.FirstLod;
        public ELodFormat LodExportFormat
        {
            get => _lodExportFormat;
            set => SetProperty(ref _lodExportFormat, value);
        }

        private bool _showSkybox = true;
        public bool ShowSkybox
        {
            get => _showSkybox;
            set => SetProperty(ref _showSkybox, value);
        }

        private bool _showGrid = true;
        public bool ShowGrid
        {
            get => _showGrid;
            set => SetProperty(ref _showGrid, value);
        }

        private bool _animateWithRotationOnly;
        public bool AnimateWithRotationOnly
        {
            get => _animateWithRotationOnly;
            set => SetProperty(ref _animateWithRotationOnly, value);
        }

        private Camera.WorldMode _cameraMode = Camera.WorldMode.Arcball;
        public Camera.WorldMode CameraMode
        {
            get => _cameraMode;
            set => SetProperty(ref _cameraMode, value);
        }

        private int _previewMaxTextureSize = 1024;
        public int PreviewMaxTextureSize
        {
            get => _previewMaxTextureSize;
            set => SetProperty(ref _previewMaxTextureSize, value);
        }

        private bool _previewStaticMeshes = true;
        public bool PreviewStaticMeshes
        {
            get => _previewStaticMeshes;
            set => SetProperty(ref _previewStaticMeshes, value);
        }

        private bool _previewSkeletalMeshes = true;
        public bool PreviewSkeletalMeshes
        {
            get => _previewSkeletalMeshes;
            set => SetProperty(ref _previewSkeletalMeshes, value);
        }

        private bool _previewMaterials = true;
        public bool PreviewMaterials
        {
            get => _previewMaterials;
            set => SetProperty(ref _previewMaterials, value);
        }

        private bool _previewWorlds = true;
        public bool PreviewWorlds
        {
            get => _previewWorlds;
            set => SetProperty(ref _previewWorlds, value);
        }

        private bool _saveMorphTargets = true;
        public bool SaveMorphTargets
        {
            get => _saveMorphTargets;
            set => SetProperty(ref _saveMorphTargets, value);
        }

        private bool _saveEmbeddedMaterials = true;
        public bool SaveEmbeddedMaterials
        {
            get => _saveEmbeddedMaterials;
            set => SetProperty(ref _saveEmbeddedMaterials, value);
        }

        private bool _saveSkeletonAsMesh;
        public bool SaveSkeletonAsMesh
        {
            get => _saveSkeletonAsMesh;
            set => SetProperty(ref _saveSkeletonAsMesh, value);
        }
    }
}
