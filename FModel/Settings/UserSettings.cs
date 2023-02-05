using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
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

        public static void Save()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(Default, Formatting.Indented));
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }

        public static bool IsEndpointValid(FGame game, EEndpointType type, out FEndpoint endpoint)
        {
            endpoint = null;
            if (!Default.CustomEndpoints.TryGetValue(game, out var endpoints))
                return false;

            endpoint = endpoints[(int) type];
            return endpoint.Overwrite || endpoint.IsValid;
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

        // <gameDirectory as string, settings>
        // can't refactor to use this data layout for everything
        // because it will wipe old user settings that relies on FGame
        private IDictionary<string, GameSelectorViewModel.DetectedGame> _manualGames = new Dictionary<string, GameSelectorViewModel.DetectedGame>();
        public IDictionary<string, GameSelectorViewModel.DetectedGame> ManualGames
        {
            get => _manualGames;
            set => SetProperty(ref _manualGames, value);
        }

        private ETexturePlatform _overridedPlatform = ETexturePlatform.DesktopMobile;
        public ETexturePlatform OverridedPlatform
        {
            get => _overridedPlatform;
            set => SetProperty(ref _overridedPlatform, value);
        }

        private IDictionary<FGame, string> _presets = new Dictionary<FGame, string>
        {
            {FGame.Unknown, Constants._NO_PRESET_TRIGGER},
            {FGame.FortniteGame, Constants._NO_PRESET_TRIGGER},
            {FGame.ShooterGame, Constants._NO_PRESET_TRIGGER},
            {FGame.DeadByDaylight, Constants._NO_PRESET_TRIGGER},
            {FGame.OakGame, Constants._NO_PRESET_TRIGGER},
            {FGame.Dungeons, Constants._NO_PRESET_TRIGGER},
            {FGame.WorldExplorers, Constants._NO_PRESET_TRIGGER},
            {FGame.g3, Constants._NO_PRESET_TRIGGER},
            {FGame.StateOfDecay2, Constants._NO_PRESET_TRIGGER},
            {FGame.Prospect, Constants._NO_PRESET_TRIGGER},
            {FGame.Indiana, Constants._NO_PRESET_TRIGGER},
            {FGame.RogueCompany, Constants._NO_PRESET_TRIGGER},
            {FGame.SwGame, Constants._NO_PRESET_TRIGGER},
            {FGame.Platform, Constants._NO_PRESET_TRIGGER},
            {FGame.BendGame, Constants._NO_PRESET_TRIGGER},
            {FGame.TslGame, Constants._NO_PRESET_TRIGGER},
            {FGame.PortalWars, Constants._NO_PRESET_TRIGGER},
            {FGame.Gameface, Constants._NO_PRESET_TRIGGER},
            {FGame.Athena, Constants._NO_PRESET_TRIGGER},
            {FGame.PandaGame, Constants._NO_PRESET_TRIGGER},
            {FGame.Hotta, Constants._NO_PRESET_TRIGGER},
            {FGame.eFootball, Constants._NO_PRESET_TRIGGER}
        };
        public IDictionary<FGame, string> Presets
        {
            get => _presets;
            set => SetProperty(ref _presets, value);
        }

        private IDictionary<FGame, EGame> _overridedGame = new Dictionary<FGame, EGame>
        {
            {FGame.Unknown, EGame.GAME_UE4_LATEST},
            {FGame.FortniteGame, EGame.GAME_UE5_2},
            {FGame.ShooterGame, EGame.GAME_Valorant},
            {FGame.DeadByDaylight, EGame.GAME_UE4_27},
            {FGame.OakGame, EGame.GAME_Borderlands3},
            {FGame.Dungeons, EGame.GAME_UE4_22},
            {FGame.WorldExplorers, EGame.GAME_UE4_24},
            {FGame.g3, EGame.GAME_UE4_22},
            {FGame.StateOfDecay2, EGame.GAME_StateOfDecay2},
            {FGame.Prospect, EGame.GAME_Splitgate},
            {FGame.Indiana, EGame.GAME_UE4_21},
            {FGame.RogueCompany, EGame.GAME_RogueCompany},
            {FGame.SwGame, EGame.GAME_UE4_LATEST},
            {FGame.Platform, EGame.GAME_UE4_26},
            {FGame.BendGame, EGame.GAME_UE4_11},
            {FGame.TslGame, EGame.GAME_PlayerUnknownsBattlegrounds},
            {FGame.PortalWars, EGame.GAME_UE4_27},
            {FGame.Gameface, EGame.GAME_GTATheTrilogyDefinitiveEdition},
            {FGame.Athena, EGame.GAME_SeaOfThieves},
            {FGame.PandaGame, EGame.GAME_UE4_26},
            {FGame.Hotta, EGame.GAME_TowerOfFantasy},
            {FGame.eFootball, EGame.GAME_UE4_26}
        };
        public IDictionary<FGame, EGame> OverridedGame
        {
            get => _overridedGame;
            set => SetProperty(ref _overridedGame, value);
        }

        private IDictionary<FGame, List<FCustomVersion>> _overridedCustomVersions = new Dictionary<FGame, List<FCustomVersion>>
        {
            {FGame.Unknown, null},
            {FGame.FortniteGame, null},
            {FGame.ShooterGame, null},
            {FGame.DeadByDaylight, null},
            {FGame.OakGame, null},
            {FGame.Dungeons, null},
            {FGame.WorldExplorers, null},
            {FGame.g3, null},
            {FGame.StateOfDecay2, null},
            {FGame.Prospect, null},
            {FGame.Indiana, null},
            {FGame.RogueCompany, null},
            {FGame.SwGame, null},
            {FGame.Platform, null},
            {FGame.BendGame, null},
            {FGame.TslGame, null},
            {FGame.PortalWars, null},
            {FGame.Gameface, null},
            {FGame.Athena, null},
            {FGame.PandaGame, null},
            {FGame.Hotta, null},
            {FGame.eFootball, null}
        };
        public IDictionary<FGame, List<FCustomVersion>> OverridedCustomVersions
        {
            get => _overridedCustomVersions;
            set => SetProperty(ref _overridedCustomVersions, value);
        }

        private IDictionary<FGame, Dictionary<string, bool>> _overridedOptions = new Dictionary<FGame, Dictionary<string, bool>>
        {
            {FGame.Unknown, null},
            {FGame.FortniteGame, null},
            {FGame.ShooterGame, null},
            {FGame.DeadByDaylight, null},
            {FGame.OakGame, null},
            {FGame.Dungeons, null},
            {FGame.WorldExplorers, null},
            {FGame.g3, null},
            {FGame.StateOfDecay2, null},
            {FGame.Prospect, null},
            {FGame.Indiana, null},
            {FGame.RogueCompany, null},
            {FGame.SwGame, null},
            {FGame.Platform, null},
            {FGame.BendGame, null},
            {FGame.TslGame, null},
            {FGame.PortalWars, null},
            {FGame.Gameface, null},
            {FGame.Athena, null},
            {FGame.PandaGame, null},
            {FGame.Hotta, null},
            {FGame.eFootball, null}
        };

        private IDictionary<FGame, Dictionary<string, KeyValuePair<string, string>>> _overridedMapStructTypes = new Dictionary<FGame, Dictionary<string, KeyValuePair<string, string>>>
        {
            {FGame.Unknown, null},
            {FGame.FortniteGame, null},
            {FGame.ShooterGame, null},
            {FGame.DeadByDaylight, null},
            {FGame.OakGame, null},
            {FGame.Dungeons, null},
            {FGame.WorldExplorers, null},
            {FGame.g3, null},
            {FGame.StateOfDecay2, null},
            {FGame.Prospect, null},
            {FGame.Indiana, null},
            {FGame.RogueCompany, null},
            {FGame.SwGame, null},
            {FGame.Platform, null},
            {FGame.BendGame, null},
            {FGame.TslGame, null},
            {FGame.PortalWars, null},
            {FGame.Gameface, null},
            {FGame.Athena, null},
            {FGame.PandaGame, null},
            {FGame.Hotta, null},
            {FGame.eFootball, null}
        };
        public IDictionary<FGame, Dictionary<string, bool>> OverridedOptions
        {
            get => _overridedOptions;
            set => SetProperty(ref _overridedOptions, value);
        }

        public IDictionary<FGame, Dictionary<string, KeyValuePair<string, string>>> OverridedMapStructTypes
        {
            get => _overridedMapStructTypes;
            set => SetProperty(ref _overridedMapStructTypes, value);
        }

        private IDictionary<FGame, FEndpoint[]> _customEndpoints = new Dictionary<FGame, FEndpoint[]>
        {
            {FGame.Unknown, new FEndpoint[]{new (), new ()}},
            {
                FGame.FortniteGame, new []
                {
                    new FEndpoint("https://fortnitecentral.genxgames.gg/api/v1/aes", "$.['mainKey','dynamicKeys']"),
                    new FEndpoint("https://fortnitecentral.genxgames.gg/api/v1/mappings", "$.[?(@.meta.compressionMethod=='Oodle')].['url','fileName']") //  && @.meta.platform=='Windows'
                }
            },
            {FGame.ShooterGame, new FEndpoint[]{new (), new ()}},
            {FGame.DeadByDaylight, new FEndpoint[]{new (), new ()}},
            {FGame.OakGame, new FEndpoint[]{new (), new ()}},
            {FGame.Dungeons, new FEndpoint[]{new (), new ()}},
            {FGame.WorldExplorers, new FEndpoint[]{new (), new ()}},
            {FGame.g3, new FEndpoint[]{new (), new ()}},
            {FGame.StateOfDecay2, new FEndpoint[]{new (), new ()}},
            {FGame.Prospect, new FEndpoint[]{new (), new ()}},
            {FGame.Indiana, new FEndpoint[]{new (), new ()}},
            {FGame.RogueCompany, new FEndpoint[]{new (), new ()}},
            {FGame.SwGame, new FEndpoint[]{new (), new ()}},
            {FGame.Platform, new FEndpoint[]{new (), new ()}},
            {FGame.BendGame, new FEndpoint[]{new (), new ()}},
            {FGame.TslGame, new FEndpoint[]{new (), new ()}},
            {FGame.PortalWars, new FEndpoint[]{new (), new ()}},
            {FGame.Gameface, new FEndpoint[]{new (), new ()}},
            {FGame.Athena, new FEndpoint[]{new (), new ()}},
            {FGame.PandaGame, new FEndpoint[]{new (), new ()}},
            {FGame.Hotta, new FEndpoint[]{new (), new ()}},
            {FGame.eFootball, new FEndpoint[]{new (), new ()}}
        };
        public IDictionary<FGame, FEndpoint[]> CustomEndpoints
        {
            get => _customEndpoints;
            set => SetProperty(ref _customEndpoints, value);
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
            {
                FGame.DeadByDaylight, new List<CustomDirectory>
                {
                    new("Audio", "DeadByDaylight/Content/WwiseAudio/Windows/"),
                    new("Characters", "DeadByDaylight/Content/Characters/"),
                    new("Icons", "DeadByDaylight/Content/UI/UMGAssets/Icons/"),
                    new("Strings", "DeadByDaylight/Content/Localization/")
                }
            },
            {FGame.OakGame, new List<CustomDirectory>()},
            {
                FGame.Dungeons, new List<CustomDirectory>
                {
                    new("Levels", "Dungeons/Content/data/Lovika/Levels"),
                    new("Friendlies", "Dungeons/Content/Actor/Characters/Friendlies"),
                    new("Skins", "Dungeons/Content/Actor/Characters/Player/Master/Skins"),
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
            {FGame.BendGame, new List<CustomDirectory>()},
            {FGame.TslGame, new List<CustomDirectory>()},
            {FGame.PortalWars, new List<CustomDirectory>()},
            {FGame.Gameface, new List<CustomDirectory>()},
            {FGame.Athena, new List<CustomDirectory>()},
            {FGame.PandaGame, new List<CustomDirectory>()},
            {FGame.Hotta, new List<CustomDirectory>()},
            {FGame.eFootball, new List<CustomDirectory>()}
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

        private bool _saveSkeletonAsMesh;
        public bool SaveSkeletonAsMesh
        {
            get => _saveSkeletonAsMesh;
            set => SetProperty(ref _saveSkeletonAsMesh, value);
        }
    }
}
