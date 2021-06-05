using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.ViewModels
{
    public class MapLayer
    {
        public SKBitmap Layer;
        public bool IsEnabled;
    }

    public enum EWaypointType
    {
        Parkour,
        TimeTrials
    }
    
    public class MapViewerViewModel : ViewModel
    {
        private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

        #region BINDINGS
        
        private bool _brPois;
        public bool BrPois
        {
            get => _brPois;
            set => SetProperty(ref _brPois, value, "ApolloGameplay_MapPois");
        }
        
        private bool _brLandmarks;
        public bool BrLandmarks
        {
            get => _brLandmarks;
            set => SetProperty(ref _brLandmarks, value, "ApolloGameplay_MapLandmarks");
        }
        
        private bool _brPatrolsPath;
        public bool BrPatrolsPath
        {
            get => _brPatrolsPath;
            set => SetProperty(ref _brPatrolsPath, value, "ApolloGameplay_PatrolsPath");
        }
        
        private bool _prLandmarks;
        public bool PrLandmarks
        {
            get => _prLandmarks;
            set => SetProperty(ref _prLandmarks, value, "PapayaGameplay_MapLandmarks");
        }
        
        private bool _prCannonball;
        public bool PrCannonball
        {
            get => _prCannonball;
            set => SetProperty(ref _prCannonball, value, "PapayaGameplay_CannonballGame");
        }
        
        private bool _prSkydive;
        public bool PrSkydive
        {
            get => _prSkydive;
            set => SetProperty(ref _prSkydive, value, "PapayaGameplay_SkydiveGame");
        }
        
        private bool _prShootingTargets;
        public bool PrShootingTargets
        {
            get => _prShootingTargets;
            set => SetProperty(ref _prShootingTargets, value, "PapayaGameplay_ShootingTargets");
        }
        
        private bool _prParkour;
        public bool PrParkour
        {
            get => _prParkour;
            set => SetProperty(ref _prParkour, value, "PapayaGameplay_ParkourGame");
        }
        
        private bool _prTimeTrials;
        public bool PrTimeTrials
        {
            get => _prTimeTrials;
            set => SetProperty(ref _prTimeTrials, value, "PapayaGameplay_TimeTrials");
        }
        
        private bool _prVendingMachines;
        public bool PrVendingMachines
        {
            get => _prVendingMachines;
            set => SetProperty(ref _prVendingMachines, value, "PapayaGameplay_VendingMachines");
        }
        
        private bool _prMusicBlocks;
        public bool PrMusicBlocks
        {
            get => _prMusicBlocks;
            set => SetProperty(ref _prMusicBlocks, value, "PapayaGameplay_MusicBlocks");
        }

        #endregion

        #region BITMAP IMAGES

        private BitmapImage _brMiniMapImage;
        private BitmapImage _prMiniMapImage;
        private BitmapImage _mapImage;
        public BitmapImage MapImage
        {
            get => _mapImage;
            set => SetProperty(ref _mapImage, value);
        }
        
        private BitmapImage _brLayerImage;
        private BitmapImage _prLayerImage;
        private BitmapImage _layerImage;
        public BitmapImage LayerImage
        {
            get => _layerImage;
            set => SetProperty(ref _layerImage, value);
        }

        private const int _widthHeight = 2048;
        private const int _brRadius = 135000;
        private const int _prRadius = 51000;
        private int _mapIndex;
        public int MapIndex // 0 is BR, 1 is PR
        {
            get => _mapIndex;
            set
            {
                SetProperty(ref _mapIndex, value);
                TriggerChange();
            }
        }

        #endregion

        private const string _FIRST_BITMAP = "MapCheck";
        private readonly Dictionary<string, MapLayer>[] _bitmaps; // first bitmap is the displayed map, others are overlays of the map
        private readonly CUE4ParseViewModel _cue4Parse;

        public MapViewerViewModel(CUE4ParseViewModel cue4Parse)
        {
            _bitmaps = new[]
            {
                new Dictionary<string, MapLayer>(),
                new Dictionary<string, MapLayer>()
            };
            _cue4Parse = cue4Parse;
        }

        public async void Initialize()
        {
            Utils.Typefaces ??= new Typefaces(_cue4Parse);
            _imagePaint.Typeface = Utils.Typefaces.Bottom ?? Utils.Typefaces.DisplayName;
            await LoadBrMiniMap();
            await LoadPrMiniMap();
            TriggerChange();
        }

        public BitmapImage GetImageToSave() => GetImageSource(GetLayerBitmap(true));
        private SKBitmap GetLayerBitmap(bool withMap)
        {
            var ret = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            foreach (var (key, value) in _bitmaps[MapIndex])
            {
                if (!value.IsEnabled || !withMap && key == _FIRST_BITMAP) continue;
                c.DrawBitmap(value.Layer, new SKRect(0, 0, _widthHeight, _widthHeight));
            }

            return ret;
        }

        protected override bool SetProperty<T>(ref T storage, T value, string propertyName = null) // don't delete, else nothing will update for some reason
        {
            var ret = base.SetProperty(ref storage, value, propertyName);
            if (bool.TryParse(value.ToString(), out var b)) GenericToggle(propertyName, b);
            return ret;
        }
        
        private async void GenericToggle(string key, bool enabled)
        {
            if (_bitmaps[MapIndex].TryGetValue(key, out var layer) && layer.Layer != null)
            {
                layer.IsEnabled = enabled;
            }
            else if (enabled) // load layer
            {
                switch (key)
                {
                    case "ApolloGameplay_MapPois":
                    case "ApolloGameplay_MapLandmarks":
                    case "PapayaGameplay_MapLandmarks":
                        await LoadQuestIndicatorData();
                        break;
                    case "ApolloGameplay_PatrolsPath":
                        await LoadPatrolsPath();
                        break;
                    case "PapayaGameplay_CannonballGame":
                        await LoadCannonballGame();
                        break;
                    case "PapayaGameplay_SkydiveGame":
                        await LoadSkydiveGame();
                        break;
                    case "PapayaGameplay_ShootingTargets":
                        await LoadShootingTargets();
                        break;
                    case "PapayaGameplay_ParkourGame":
                        await LoadWaypoint(EWaypointType.Parkour);
                        break;
                    case "PapayaGameplay_TimeTrials":
                        await LoadWaypoint(EWaypointType.TimeTrials);
                        break;
                    case "PapayaGameplay_VendingMachines":
                        await LoadVendingMachines();
                        break;
                    case "PapayaGameplay_MusicBlocks":
                        await LoadMusicBlocks();
                        break;
                }
                _bitmaps[MapIndex][key].IsEnabled = true;
            }

            switch (MapIndex)
            {
                case 0:
                    _brLayerImage = GetImageSource(GetLayerBitmap(false));
                    break;
                case 1:
                    _prLayerImage = GetImageSource(GetLayerBitmap(false));
                    break;
            }
            TriggerChange();
        }

        private BitmapImage GetImageSource(SKBitmap bitmap)
        {
            if (bitmap == null) return null;
            using var stream = SKImage.FromBitmap(bitmap).Encode().AsStream();
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private void TriggerChange()
        {
            switch (_mapIndex)
            {
                case 0:
                    _mapImage = _brMiniMapImage;
                    _layerImage = _brLayerImage;
                    break;
                case 1:
                    _mapImage = _prMiniMapImage;
                    _layerImage = _prLayerImage;
                    break;
            }
                
            RaisePropertyChanged(nameof(MapImage));
            RaisePropertyChanged(nameof(LayerImage));
        }
        
        private readonly SKPaint _imagePaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High,
            Color = SKColors.White, TextAlign = SKTextAlign.Center, TextSize = 25,
            ImageFilter = SKImageFilter.CreateDropShadow(4, 4, 8, 8, SKColors.Black)
        };
        private readonly SKPaint _pathPaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High, IsStroke = true,
            Style = SKPaintStyle.Stroke, StrokeWidth = 5, Color = SKColors.Red,
            ImageFilter = SKImageFilter.CreateDropShadow(4, 4, 8, 8, SKColors.Black)
        };

        private FVector2D GetMapPosition(FVector vector, int mapRadius)
        {
            var nx = (vector.Y + mapRadius) / (mapRadius * 2) * _widthHeight;
            var ny = (1 - (vector.X + mapRadius) / (mapRadius * 2)) * _widthHeight;
            return new FVector2D(nx, ny);
        }
        
        private async Task LoadBrMiniMap()
        {
            if (_bitmaps[0].TryGetValue(_FIRST_BITMAP, out var brMap) && brMap.Layer != null)
                return; // if map already loaded
            
            await _threadWorkerView.Begin(_ =>
            {
                if (!Utils.TryLoadObject("FortniteGame/Content/UI/IngameMap/UIMapManagerBR.Default__UIMapManagerBR_C", out UObject mapManager) ||
                    !mapManager.TryGetValue(out UObject mapMaterial, "MapMaterial") ||
                    !mapMaterial.TryGetValue(out FStructFallback cachedExpressionData, "CachedExpressionData") ||
                    !cachedExpressionData.TryGetValue(out FStructFallback parameters, "Parameters") ||
                    !parameters.TryGetValue(out UTexture2D[] textureValues, "TextureValues")) return;
                
                _bitmaps[0][_FIRST_BITMAP] = new MapLayer{Layer = Utils.GetBitmap(textureValues[0]), IsEnabled = true};
                _brMiniMapImage = GetImageSource(_bitmaps[0][_FIRST_BITMAP].Layer);
            });
        }

        private async Task LoadPrMiniMap()
        {
            if (_bitmaps[1].TryGetValue(_FIRST_BITMAP, out var prMap) && prMap.Layer != null)
                return; // if map already loaded
            
            await _threadWorkerView.Begin(_ =>
            {
                if (!Utils.TryLoadObject("FortniteGame/Content/UI/IngameMap/UIMapManagerPapaya.Default__UIMapManagerPapaya_C", out UObject mapManager) ||
                    !mapManager.TryGetValue(out UMaterial mapMaterial, "MapMaterial") ||
                    mapMaterial.ReferencedTextures.Count < 1) return;

                _bitmaps[1][_FIRST_BITMAP] = new MapLayer{Layer = Utils.GetBitmap(mapMaterial.ReferencedTextures[0] as UTexture2D), IsEnabled = true};
                _prMiniMapImage = GetImageSource(_bitmaps[1][_FIRST_BITMAP].Layer);
            });
        }
        
        private async Task LoadQuestIndicatorData()
        {
            await _threadWorkerView.Begin(_ =>
            {
                var poisBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                var brLandmarksBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                var prLandmarksBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var pois = new SKCanvas(poisBitmap);
                using var brLandmarks = new SKCanvas(brLandmarksBitmap);
                using var prLandmarks = new SKCanvas(prLandmarksBitmap);
                
                if (Utils.TryLoadObject("FortniteGame/Content/Quests/QuestIndicatorData", out UObject indicatorData) &&
                    indicatorData.TryGetValue(out FStructFallback[] challengeMapPoiData, "ChallengeMapPoiData"))
                {
                    foreach (var poiData in challengeMapPoiData)
                    {
                        if (!poiData.TryGetValue(out FSoftObjectPath discoveryQuest, "DiscoveryQuest") ||
                            !poiData.TryGetValue(out FText text, "Text") || string.IsNullOrEmpty(text.Text) ||
                            !poiData.TryGetValue(out FVector worldLocation, "WorldLocation") ||
                            !poiData.TryGetValue(out FName discoverBackend, "DiscoverObjectiveBackendName")) continue;
                        
                        var shaper = new CustomSKShaper(_imagePaint.Typeface);
                        var shapedText = shaper.Shape(text.Text, _imagePaint);

                        if (discoverBackend.Text.Contains("papaya", StringComparison.OrdinalIgnoreCase))
                        {
                            var vector = GetMapPosition(worldLocation, _prRadius);
                            prLandmarks.DrawPoint(vector.X, vector.Y, _pathPaint);
                            prLandmarks.DrawShapedText(shaper, text.Text, vector.X - shapedText.Points[^1].X / 2, vector.Y - 12.5F, _imagePaint);
                        }
                        else if (discoveryQuest.AssetPathName.Text.Contains("landmarks", StringComparison.OrdinalIgnoreCase))
                        {
                            var vector = GetMapPosition(worldLocation, _brRadius);
                            brLandmarks.DrawPoint(vector.X, vector.Y, _pathPaint);
                            brLandmarks.DrawShapedText(shaper, text.Text, vector.X - shapedText.Points[^1].X / 2, vector.Y - 12.5F, _imagePaint);
                        }
                        else
                        {
                            var vector = GetMapPosition(worldLocation, _brRadius);
                            pois.DrawPoint(vector.X, vector.Y, _pathPaint);
                            pois.DrawShapedText(shaper, text.Text, vector.X - shapedText.Points[^1].X / 2, vector.Y - 12.5F, _imagePaint);
                        }
                    }
                }

                _bitmaps[0]["ApolloGameplay_MapPois"] = new MapLayer {Layer = poisBitmap, IsEnabled = false};
                _bitmaps[0]["ApolloGameplay_MapLandmarks"] = new MapLayer {Layer = brLandmarksBitmap, IsEnabled = false};
                _bitmaps[1]["PapayaGameplay_MapLandmarks"] = new MapLayer {Layer = prLandmarksBitmap, IsEnabled = false};
            });
        }
        
        private async Task LoadPatrolsPath()
        {
            await _threadWorkerView.Begin(_ =>
            {
                var patrolsPathBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var c = new SKCanvas(patrolsPathBitmap);

                if (!Utils.TryLoadObject("FortniteGame/Plugins/GameFeatures/NPCLibrary/Content/GameFeatureData.GameFeatureData", out UObject gameFeatureData) ||
                    !gameFeatureData.TryGetValue(out FPackageIndex levelOverlayConfig, "LevelOverlayConfig") ||
                    !Utils.TryGetPackageIndexExport(levelOverlayConfig, out UObject npcLibrary) ||
                    !npcLibrary.TryGetValue(out FStructFallback[] overlayList, "OverlayList"))
                    return;

                foreach (var overlay in overlayList)
                {
                    if (!overlay.TryGetValue(out FSoftObjectPath overlayWorld, "OverlayWorld"))
                        continue;

                    var exports = Utils.LoadExports(overlayWorld.AssetPathName.Text.SubstringBeforeLast("."));
                    foreach (var export in exports)
                    {
                        if (export is not { } uObject) continue;
                        if (!uObject.ExportType.Equals("FortAthenaPatrolPath", StringComparison.OrdinalIgnoreCase) ||
                            !uObject.TryGetValue(out FGameplayTagContainer gameplayTags, "GameplayTags") ||
                            !uObject.TryGetValue(out FPackageIndex[] patrolPoints, "PatrolPoints")) continue;

                        if (!Utils.TryGetPackageIndexExport(patrolPoints[0], out uObject) ||
                            !uObject.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                            !Utils.TryGetPackageIndexExport(rootComponent, out uObject) ||
                            !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                        var path = new SKPath();
                        var vector = GetMapPosition(relativeLocation, _brRadius);
                        path.MoveTo(vector.X, vector.Y);

                        for (var i = 1; i < patrolPoints.Length; i++)
                        {
                            if (!Utils.TryGetPackageIndexExport(patrolPoints[i], out uObject) ||
                                !uObject.TryGetValue(out rootComponent, "RootComponent") ||
                                !Utils.TryGetPackageIndexExport(rootComponent, out uObject) ||
                                !uObject.TryGetValue(out relativeLocation, "RelativeLocation")) continue;

                            vector = GetMapPosition(relativeLocation, _brRadius);
                            path.LineTo(vector.X, vector.Y);
                        }

                        path.Close();
                        c.DrawPath(path, _pathPaint);
                        c.DrawText(gameplayTags.GameplayTags[0].Text.SubstringAfterLast("."), vector.X, vector.Y - 12.5F, _imagePaint);
                    }
                }

                _bitmaps[0]["ApolloGameplay_PatrolsPath"] = new MapLayer {Layer = patrolsPathBitmap, IsEnabled = false};
            });
        }

        private async Task LoadCannonballGame()
        {
            await _threadWorkerView.Begin(_ =>
            {
                var cannonballBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var c = new SKCanvas(cannonballBitmap);

                var exports = Utils.LoadExports("/PapayaGameplay/LevelOverlays/PapayaGameplay_CannonballGame");
                foreach (var export in exports)
                {
                    if (export is not { } uObject) continue;
                    if (!uObject.ExportType.Equals("BP_CannonballGame_Target_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("CannonballGame_VehicleSpawner_C", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!uObject.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                        !Utils.TryGetPackageIndexExport(rootComponent, out uObject) ||
                        !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                    var vector = GetMapPosition(relativeLocation, _prRadius);
                    c.DrawPoint(vector.X, vector.Y, _pathPaint);
                    c.DrawText(Utils.GetLocalizedResource("", "D998BEF44F051E0885C6C58565934BEA", "Cannonball"), vector.X, vector.Y - 12.5F, _imagePaint);
                }

                _bitmaps[1]["PapayaGameplay_CannonballGame"] = new MapLayer {Layer = cannonballBitmap, IsEnabled = false};
            });
        }

        private async Task LoadSkydiveGame()
        {
            await _threadWorkerView.Begin(_ =>
            {
                var skydiveBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var c = new SKCanvas(skydiveBitmap);

                var exports = Utils.LoadExports("/PapayaGameplay/LevelOverlays/PapayaGameplay_SkydiveGame");
                foreach (var export in exports)
                {
                    if (export is not { } uObject) continue;
                    if (!uObject.ExportType.Equals("BP_Waypoint_Papaya_Skydive_Start_C", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!uObject.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                        !uObject.TryGetValue(out FText minigameActivityName, "MinigameActivityName") ||
                        !Utils.TryGetPackageIndexExport(rootComponent, out uObject) ||
                        !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                    var vector = GetMapPosition(relativeLocation, _prRadius);
                    c.DrawPoint(vector.X, vector.Y, _pathPaint);
                    c.DrawText(minigameActivityName.Text, vector.X, vector.Y - 12.5F, _imagePaint);
                }

                _bitmaps[1]["PapayaGameplay_SkydiveGame"] = new MapLayer {Layer = skydiveBitmap, IsEnabled = false};
            });
        }
        
        private async Task LoadShootingTargets()
        {
            await _threadWorkerView.Begin(_ =>
            {
                var shootingTargetsBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var c = new SKCanvas(shootingTargetsBitmap);

                var bDone = false;
                var exports = Utils.LoadExports("/PapayaGameplay/LevelOverlays/PapayaGameplay_ShootingTargets");
                foreach (var export in exports)
                {
                    if (export is not { } uObject) continue;
                    if (!uObject.ExportType.Equals("PapayaShootingTarget_C", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!uObject.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                        !Utils.TryGetPackageIndexExport(rootComponent, out uObject) ||
                        !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                    var vector = GetMapPosition(relativeLocation, _prRadius);
                    c.DrawPoint(vector.X, vector.Y, _pathPaint);
                    if (bDone) continue;
                    
                    bDone = true;
                    c.DrawText("Shooting Target", vector.X, vector.Y - 12.5F, _imagePaint);
                }

                _bitmaps[1]["PapayaGameplay_ShootingTargets"] = new MapLayer {Layer = shootingTargetsBitmap, IsEnabled = false};
            });
        }

        private async Task LoadWaypoint(EWaypointType type)
        {
            await _threadWorkerView.Begin(_ =>
            {
                var waypointBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var c = new SKCanvas(waypointBitmap);

                string file;
                string name;
                switch (type)
                {
                    case EWaypointType.Parkour:
                        file = "PapayaGameplay_ParkourGame";
                        name = "Parkour";
                        break;
                    case EWaypointType.TimeTrials:
                        file = "PapayaGameplay_TimeTrials";
                        name = "Time Trials";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
                
                var path = new SKPath();
                var exports = Utils.LoadExports($"/PapayaGameplay/LevelOverlays/{file}");
                foreach (var export in exports)
                {
                    if (export is not { } uObject) continue;
                    if (!uObject.ExportType.Equals("BP_Waypoint_Parent_Papaya_C", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!uObject.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                        !Utils.TryGetPackageIndexExport(rootComponent, out uObject) ||
                        !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                    var vector = GetMapPosition(relativeLocation, _prRadius);
                    if (path.IsEmpty)
                    {
                        path.MoveTo(vector.X, vector.Y);
                        c.DrawText(name, vector.X, vector.Y - 12.5F, _imagePaint);
                    }
                    else path.LineTo(vector.X, vector.Y);
                }
                path.Close();
                c.DrawPath(path, _pathPaint);

                _bitmaps[1][file] = new MapLayer {Layer = waypointBitmap, IsEnabled = false};
            });
        }

        private async Task LoadVendingMachines()
        {
            await _threadWorkerView.Begin(_ =>
            {
                var set = new HashSet<string>();
                var timeTrialsBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var c = new SKCanvas(timeTrialsBitmap);

                var exports = Utils.LoadExports("/PapayaGameplay/LevelOverlays/PapayaGameplay_VendingMachines");
                foreach (var export in exports)
                {
                    if (export is not { } uObject) continue;
                    if (!uObject.ExportType.Equals("B_Papaya_VendingMachine_Boat_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_BoogieBomb_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_Burger_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_CrashPad_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_FishingPole_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_Grappler_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_Jetpack_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_PaintGrenade_Blue_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_PaintGrenade_Red_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_PaintLauncher_Blue_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_PaintLauncher_Red_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_PlungerBow_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_Quad_C", StringComparison.OrdinalIgnoreCase) &&
                        !uObject.ExportType.Equals("B_Papaya_VendingMachine_Tomato_C", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!uObject.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                        !Utils.TryGetPackageIndexExport(rootComponent, out UObject root) ||
                        !root.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                    var name = uObject.ExportType.SubstringAfter("B_Papaya_VendingMachine_").SubstringBeforeLast("_C");
                    var vector = GetMapPosition(relativeLocation, _prRadius);
                    c.DrawPoint(vector.X, vector.Y, _pathPaint);
                    if (!set.Add(name)) continue;
                    
                    c.DrawText(name, vector.X, vector.Y - 12.5F, _imagePaint);
                }

                _bitmaps[1]["PapayaGameplay_VendingMachines"] = new MapLayer {Layer = timeTrialsBitmap, IsEnabled = false};
            });
        }
        
        private async Task LoadMusicBlocks()
        {
            await _threadWorkerView.Begin(_ =>
            {
                var shootingTargetsBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var c = new SKCanvas(shootingTargetsBitmap);

                var bDone = false;
                var exports = Utils.LoadExports("/PapayaGameplay/LevelOverlays/PapayaGameplay_MusicBlocks");
                foreach (var export in exports)
                {
                    if (export is not { } uObject) continue;
                    if (!uObject.ExportType.Equals("MusicBlock_Piano3_Papaya_C", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!uObject.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                        !Utils.TryGetPackageIndexExport(rootComponent, out uObject) ||
                        !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                    var vector = GetMapPosition(relativeLocation, _prRadius);
                    c.DrawPoint(vector.X, vector.Y, _pathPaint);
                    if (bDone) continue;
                    
                    bDone = true;
                    c.DrawText("Music Blocks", vector.X, vector.Y - 12.5F, _imagePaint);
                }

                _bitmaps[1]["PapayaGameplay_MusicBlocks"] = new MapLayer {Layer = shootingTargetsBitmap, IsEnabled = false};
            });
        }
    }
}