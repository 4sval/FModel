using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using FModel.Framework;
using FModel.ViewModels;
using FModel.ViewModels.ApiEndpoints.Models;
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

        public static void Save()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(Default, Formatting.Indented));
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
        
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

        private string _gameDirectory;
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

        private bool _isAutoExportData;
        public bool IsAutoExportData
        {
            get => _isAutoExportData;
            set => SetProperty(ref _isAutoExportData, value);
        }

        private bool _isAutoSaveProps;
        public bool IsAutoSaveProps
        {
            get => _isAutoSaveProps;
            set => SetProperty(ref _isAutoSaveProps, value);
        }

        private bool _isAutoSaveTextures;
        public bool IsAutoSaveTextures
        {
            get => _isAutoSaveTextures;
            set => SetProperty(ref _isAutoSaveTextures, value);
        }

        private bool _isAutoSaveMaterials;
        public bool IsAutoSaveMaterials
        {
            get => _isAutoSaveMaterials;
            set => SetProperty(ref _isAutoSaveMaterials, value);
        }

        private bool _isAutoSaveMeshes;
        public bool IsAutoSaveMeshes
        {
            get => _isAutoSaveMeshes;
            set => SetProperty(ref _isAutoSaveMeshes, value);
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

        private IDictionary<FGame, AesResponse> _aesKeys = new Dictionary<FGame, AesResponse>();
        public IDictionary<FGame, AesResponse> AesKeys
        {
            get => _aesKeys;
            set => SetProperty(ref _aesKeys, value);
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

        private ELoadingMode _loadingMode = ELoadingMode.Multiple;
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

        private EEnabledDisabled _keepDirectoryStructure = EEnabledDisabled.Enabled;
        public EEnabledDisabled KeepDirectoryStructure
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

        private EEnabledDisabled _cosmeticDisplayAsset = EEnabledDisabled.Disabled;
        public EEnabledDisabled CosmeticDisplayAsset
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

        private IDictionary<FGame, EGame> _overridedGame = new Dictionary<FGame, EGame>
        {
            {FGame.Unknown, EGame.GAME_UE4_LATEST},
            {FGame.FortniteGame, EGame.GAME_UE4_LATEST},
            {FGame.ShooterGame, EGame.GAME_VALORANT},
            {FGame.DeadByDaylight, EGame.GAME_UE4_LATEST},
            {FGame.OakGame, EGame.GAME_BORDERLANDS3},
            {FGame.Dungeons, EGame.GAME_UE4_LATEST},
            {FGame.WorldExplorers, EGame.GAME_UE4_LATEST},
            {FGame.g3, EGame.GAME_UE4_22},
            {FGame.StateOfDecay2, EGame.GAME_SOD2},
            {FGame.Prospect, EGame.GAME_UE4_LATEST},
            {FGame.Indiana, EGame.GAME_UE4_LATEST},
            {FGame.RogueCompany, EGame.GAME_ROGUECOMPANY},
            {FGame.SwGame, EGame.GAME_UE4_LATEST},
            {FGame.Platform, EGame.GAME_UE4_25},
            {FGame.BendGame, EGame.GAME_UE4_11}
        };
        public IDictionary<FGame, EGame> OverridedGame
        {
            get => _overridedGame;
            set => SetProperty(ref _overridedGame, value);
        }

        private IDictionary<FGame, UE4Version> _overridedUEVersion = new Dictionary<FGame, UE4Version>
        {
            {FGame.Unknown, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.FortniteGame, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.ShooterGame, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.DeadByDaylight, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.OakGame, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.Dungeons, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.WorldExplorers, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.g3, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.StateOfDecay2, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.Prospect, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.Indiana, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.RogueCompany, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.SwGame, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.Platform, UE4Version.VER_UE4_DETERMINE_BY_GAME},
            {FGame.BendGame, UE4Version.VER_UE4_DETERMINE_BY_GAME}
        };
        public IDictionary<FGame, UE4Version> OverridedUEVersion
        {
            get => _overridedUEVersion;
            set => SetProperty(ref _overridedUEVersion, value);
        }

        private IDictionary<FGame, IList<CustomDirectory>> _customDirectories = new Dictionary<FGame, IList<CustomDirectory>>
        {
            {FGame.Unknown, new List<CustomDirectory>()},
            {
                FGame.FortniteGame, new List<CustomDirectory>
                {
                    new("Cosmetics", "FortniteGame/Content/Athena/Items/Cosmetics/"),
                    new("Emotes [AUDIO]", "FortniteGame/Content/Athena/Sounds/Emotes/"),
                    new("Music Packs [AUDIO]", "FortniteGame/Content/Athena/Sounds/MusicPacks/"),
                    new("Weapons", "FortniteGame/Content/Athena/Items/Weapons/"),
                    new("Strings", "FortniteGame/Content/Localization/")
                }
            },
            {
                FGame.ShooterGame, new List<CustomDirectory>
                {
                    new("Audio", "ShooterGame/Content/WwiseAudio/Media/"),
                    new("Characters", "ShooterGame/Content/Characters/"),
                    new("Gun Buddies", "ShooterGame/Content/Equippables/Buddies/"),
                    new("Cards and Sprays", "ShooterGame/Content/Personalization/"),
                    new("Shop Backgrounds", "ShooterGame/Content/UI/OutOfGame/MainMenu/Store/Shared/Textures/"),
                    new("Weapon Renders", "ShooterGame/Content/UI/Screens/OutOfGame/MainMenu/Collection/Assets/Large/")
                }
            },
            {FGame.DeadByDaylight, new List<CustomDirectory>()},
            {FGame.OakGame, new List<CustomDirectory>()},
            {
                FGame.Dungeons, new List<CustomDirectory>
                {
                    new("Strings", "Dungeons/Content/Localization/")
                }
            },
            {
                FGame.WorldExplorers, new List<CustomDirectory>
                {
                    new("Loot", "WorldExplorers/Content/Loot/"),
                    new("Strings", "WorldExplorers/Content/Localization/")
                }
            },
            {
                FGame.g3, new List<CustomDirectory>
                {
                    new("Cosmetics", "g3/Content/Blueprints/Cosmetics/"),
                    new("Strings", "g3/Content/Localization/")
                }
            },
            {FGame.StateOfDecay2, new List<CustomDirectory>()},
            {FGame.Prospect, new List<CustomDirectory>()},
            {FGame.Indiana, new List<CustomDirectory>()},
            {FGame.RogueCompany, new List<CustomDirectory>()},
            {FGame.SwGame, new List<CustomDirectory>()},
            {FGame.Platform, new List<CustomDirectory>()},
            {FGame.BendGame, new List<CustomDirectory>()}
        };
        public IDictionary<FGame, IList<CustomDirectory>> CustomDirectories
        {
            get => _customDirectories;
            set => SetProperty(ref _customDirectories, value);
        }

        private DateTime _lastAesReload = DateTime.Today.AddDays(-1);
        public DateTime LastAesReload
        {
            get => _lastAesReload;
            set => SetProperty(ref _lastAesReload, value);
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

        private Hotkey _autoExportData = new(Key.F1);
        public Hotkey AutoExportData
        {
            get => _autoExportData;
            set => SetProperty(ref _autoExportData, value);
        }

        private Hotkey _autoSaveProps = new(Key.F2);
        public Hotkey AutoSaveProps
        {
            get => _autoSaveProps;
            set => SetProperty(ref _autoSaveProps, value);
        }

        private Hotkey _autoSaveTextures = new(Key.F3);
        public Hotkey AutoSaveTextures
        {
            get => _autoSaveTextures;
            set => SetProperty(ref _autoSaveTextures, value);
        }

        private Hotkey _autoSaveMaterials = new(Key.F4);
        public Hotkey AutoSaveMaterials
        {
            get => _autoSaveMaterials;
            set => SetProperty(ref _autoSaveMaterials, value);
        }

        private Hotkey _autoSaveMeshes = new(Key.F5);
        public Hotkey AutoSaveMeshes
        {
            get => _autoSaveMeshes;
            set => SetProperty(ref _autoSaveMeshes, value);
        }

        private Hotkey _autoOpenSounds = new(Key.F6);
        public Hotkey AutoOpenSounds
        {
            get => _autoOpenSounds;
            set => SetProperty(ref _autoOpenSounds, value);
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
        
        private ELodFormat _lodExportFormat = ELodFormat.FirstLod;
        public ELodFormat LodExportFormat
        {
            get => _lodExportFormat;
            set => SetProperty(ref _lodExportFormat, value);
        }
        
        private ETextureFormat _textureExportFormat = ETextureFormat.Png;
        public ETextureFormat TextureExportFormat
        {
            get => _textureExportFormat;
            set => SetProperty(ref _textureExportFormat, value);
        }
    }
}