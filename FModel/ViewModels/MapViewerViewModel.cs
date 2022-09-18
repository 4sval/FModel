using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
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

namespace FModel.ViewModels;

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
    private DiscordHandler _discordHandler => DiscordService.DiscordHandler;

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

    private bool _brTagsLocation;
    public bool BrTagsLocation
    {
        get => _brTagsLocation;
        set => SetProperty(ref _brTagsLocation, value, "ApolloGameplay_TagsLocation");
    }

    private bool _brPatrolsPath;
    public bool BrPatrolsPath
    {
        get => _brPatrolsPath;
        set => SetProperty(ref _brPatrolsPath, value, "ApolloGameplay_PatrolsPath");
    }

    private bool _brUpgradeBenches;
    public bool BrUpgradeBenches
    {
        get => _brUpgradeBenches;
        set => SetProperty(ref _brUpgradeBenches, value, "ApolloGameplay_UpgradeBenches");
    }

    private bool _brPhonebooths;
    public bool BrPhonebooths
    {
        get => _brPhonebooths;
        set => SetProperty(ref _brPhonebooths, value, "ApolloGameplay_Phonebooths");
    }

    private bool _brVendingMachines;
    public bool BrVendingMachines
    {
        get => _brVendingMachines;
        set => SetProperty(ref _brVendingMachines, value, "ApolloGameplay_VendingMachines");
    }

    private bool _brBountyBoards;
    public bool BrBountyBoards
    {
        get => _brBountyBoards;
        set => SetProperty(ref _brBountyBoards, value, "ApolloGameplay_BountyBoards");
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
    private const int _brRadius = 131000;
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
        _textPaint.Typeface = _fillPaint.Typeface = Utils.Typefaces.Bottom ?? Utils.Typefaces.DisplayName;
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
            if (!value.IsEnabled || !withMap && key == _FIRST_BITMAP)
                continue;

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
                case "ApolloGameplay_TagsLocation":
                    await LoadTagsLocation();
                    break;
                case "ApolloGameplay_PatrolsPath":
                    await LoadPatrolsPath();
                    break;
                case "ApolloGameplay_UpgradeBenches":
                    await LoadUpgradeBenches();
                    break;
                case "ApolloGameplay_Phonebooths":
                    await LoadPhonebooths();
                    break;
                case "ApolloGameplay_VendingMachines":
                    await LoadBrVendingMachines();
                    break;
                case "ApolloGameplay_BountyBoards":
                    await LoadBountyBoards();
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
                    await LoadPrVendingMachines();
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
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = data.AsStream();
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
        var layerCount = _bitmaps[_mapIndex].Count(x => x.Value.IsEnabled);
        var layerString = $"{layerCount} Layer{(layerCount > 1 ? "s" : "")}";
        switch (_mapIndex)
        {
            case 0:
                _discordHandler.UpdateButDontSavePresence(null, $"Map Viewer: Battle Royale ({layerString})");
                _mapImage = _brMiniMapImage;
                _layerImage = _brLayerImage;
                break;
            case 1:
                _discordHandler.UpdateButDontSavePresence(null, $"Map Viewer: Party Royale ({layerString})");
                _mapImage = _prMiniMapImage;
                _layerImage = _prLayerImage;
                break;
        }

        RaisePropertyChanged(nameof(MapImage));
        RaisePropertyChanged(nameof(LayerImage));
    }

    private readonly SKPaint _textPaint = new()
    {
        IsAntialias = true, FilterQuality = SKFilterQuality.High,
        Color = SKColors.White, TextAlign = SKTextAlign.Center, TextSize = 26
    };

    private readonly SKPaint _fillPaint = new()
    {
        IsAntialias = true, FilterQuality = SKFilterQuality.High,
        IsStroke = true, Color = SKColors.Black, TextSize = 26,
        TextAlign = SKTextAlign.Center
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
                !mapManager.TryGetValue(out UMaterial mapMaterial, "MapMaterial")) return;
            var midTex = mapMaterial.GetFirstTexture();
            if ((midTex?.Name ?? string.Empty).Contains("Mask"))
                midTex = mapMaterial.GetTextureAtIndex(1);

            if (midTex is not UTexture2D tex) return;
            _bitmaps[0][_FIRST_BITMAP] = new MapLayer { Layer = Utils.GetBitmap(tex), IsEnabled = true };
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
                !mapManager.TryGetValue(out UMaterial mapMaterial, "MapMaterial") || mapMaterial.GetFirstTexture() is not UTexture2D tex) return;

            _bitmaps[1][_FIRST_BITMAP] = new MapLayer { Layer = Utils.GetBitmap(tex), IsEnabled = true };
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

                    var shaper = new CustomSKShaper(_textPaint.Typeface);
                    var shapedText = shaper.Shape(text.Text, _textPaint);

                    if (discoverBackend.Text.Contains("papaya", StringComparison.OrdinalIgnoreCase))
                    {
                        _fillPaint.StrokeWidth = 5;
                        var vector = GetMapPosition(worldLocation, _prRadius);
                        prLandmarks.DrawPoint(vector.X, vector.Y, _pathPaint);
                        prLandmarks.DrawShapedText(shaper, text.Text, vector.X - shapedText.Points[^1].X / 2, vector.Y - 12.5F, _fillPaint);
                        prLandmarks.DrawShapedText(shaper, text.Text, vector.X - shapedText.Points[^1].X / 2, vector.Y - 12.5F, _textPaint);
                    }
                    else if (discoveryQuest.AssetPathName.Text.Contains("landmarks", StringComparison.OrdinalIgnoreCase))
                    {
                        _fillPaint.StrokeWidth = 5;
                        var vector = GetMapPosition(worldLocation, _brRadius);
                        brLandmarks.DrawPoint(vector.X, vector.Y, _pathPaint);
                        brLandmarks.DrawShapedText(shaper, text.Text, vector.X - shapedText.Points[^1].X / 2, vector.Y - 12.5F, _fillPaint);
                        brLandmarks.DrawShapedText(shaper, text.Text, vector.X - shapedText.Points[^1].X / 2, vector.Y - 12.5F, _textPaint);
                    }
                    else
                    {
                        _fillPaint.StrokeWidth = 10;
                        var vector = GetMapPosition(worldLocation, _brRadius);
                        pois.DrawShapedText(shaper, text.Text.ToUpperInvariant(), vector.X - shapedText.Points[^1].X / 2, vector.Y, _fillPaint);
                        pois.DrawShapedText(shaper, text.Text.ToUpperInvariant(), vector.X - shapedText.Points[^1].X / 2, vector.Y, _textPaint);
                    }
                }
            }

            _bitmaps[0]["ApolloGameplay_MapPois"] = new MapLayer { Layer = poisBitmap, IsEnabled = false };
            _bitmaps[0]["ApolloGameplay_MapLandmarks"] = new MapLayer { Layer = brLandmarksBitmap, IsEnabled = false };
            _bitmaps[1]["PapayaGameplay_MapLandmarks"] = new MapLayer { Layer = prLandmarksBitmap, IsEnabled = false };
        });
    }

    private async Task LoadPatrolsPath()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            var patrolsPathBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(patrolsPathBitmap);

            var exports = Utils.LoadExports("/NPCLibrary/LevelOverlays/Artemis_Overlay_S22_NPCLibrary");
            foreach (var export in exports)
            {
                if (!export.ExportType.Equals("FortAthenaPatrolPath", StringComparison.OrdinalIgnoreCase) ||
                    !export.TryGetValue(out FPackageIndex[] patrolPoints, "PatrolPoints")) continue;

                var displayName = export.Name["FortAthenaPatrolPath_".Length..];
                if (export.TryGetValue(out FGameplayTagContainer gameplayTags, "GameplayTags") && gameplayTags.GameplayTags.Length > 0)
                    displayName = gameplayTags.GameplayTags[0].Text["Athena.AI.SpawnLocation.Tandem.".Length..];

                if (!Utils.TryGetPackageIndexExport(patrolPoints[0], out UObject uObject) ||
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

                c.DrawPath(path, _pathPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[0]["ApolloGameplay_PatrolsPath"] = new MapLayer { Layer = patrolsPathBitmap, IsEnabled = false };
        });
    }

    private async Task LoadCannonballGame()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            var cannonballBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(cannonballBitmap);

            var exports = Utils.LoadExports("/PapayaGameplay/LevelOverlays/PapayaGameplay_CannonballGame");
            foreach (var export in exports)
            {
                if (!export.ExportType.Equals("BP_CannonballGame_Target_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("CannonballGame_VehicleSpawner_C", StringComparison.OrdinalIgnoreCase)) continue;

                if (!export.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                    !Utils.TryGetPackageIndexExport(rootComponent, out UObject uObject) ||
                    !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                var displayName = Utils.GetLocalizedResource("", "D998BEF44F051E0885C6C58565934BEA", "Cannonball");
                var vector = GetMapPosition(relativeLocation, _prRadius);

                c.DrawPoint(vector.X, vector.Y, _pathPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[1]["PapayaGameplay_CannonballGame"] = new MapLayer { Layer = cannonballBitmap, IsEnabled = false };
        });
    }

    private async Task LoadSkydiveGame()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            var skydiveBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(skydiveBitmap);

            var exports = Utils.LoadExports("/PapayaGameplay/LevelOverlays/PapayaGameplay_SkydiveGame");
            foreach (var export in exports)
            {
                if (!export.ExportType.Equals("BP_Waypoint_Papaya_Skydive_Start_C", StringComparison.OrdinalIgnoreCase)) continue;

                if (!export.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                    !export.TryGetValue(out FText minigameActivityName, "MinigameActivityName") ||
                    !Utils.TryGetPackageIndexExport(rootComponent, out UObject uObject) ||
                    !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                var vector = GetMapPosition(relativeLocation, _prRadius);

                c.DrawPoint(vector.X, vector.Y, _pathPaint);
                c.DrawText(minigameActivityName.Text, vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText(minigameActivityName.Text, vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[1]["PapayaGameplay_SkydiveGame"] = new MapLayer { Layer = skydiveBitmap, IsEnabled = false };
        });
    }

    private async Task LoadShootingTargets()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            var shootingTargetsBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(shootingTargetsBitmap);

            var bDone = false;
            var exports = Utils.LoadExports("/PapayaGameplay/LevelOverlays/PapayaGameplay_ShootingTargets");
            foreach (var export in exports)
            {
                if (!export.ExportType.Equals("PapayaShootingTarget_C", StringComparison.OrdinalIgnoreCase)) continue;

                if (!export.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                    !Utils.TryGetPackageIndexExport(rootComponent, out UObject uObject) ||
                    !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                var vector = GetMapPosition(relativeLocation, _prRadius);
                c.DrawPoint(vector.X, vector.Y, _pathPaint);
                if (bDone) continue;

                bDone = true;
                c.DrawText("Shooting Target", vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText("Shooting Target", vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[1]["PapayaGameplay_ShootingTargets"] = new MapLayer { Layer = shootingTargetsBitmap, IsEnabled = false };
        });
    }

    private async Task LoadWaypoint(EWaypointType type)
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
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
                if (!export.ExportType.Equals("BP_Waypoint_Parent_Papaya_C", StringComparison.OrdinalIgnoreCase)) continue;

                if (!export.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                    !Utils.TryGetPackageIndexExport(rootComponent, out UObject root) ||
                    !root.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                var vector = GetMapPosition(relativeLocation, _prRadius);
                if (path.IsEmpty || export.TryGetValue(out bool startsTrial, "StartsTrial") && startsTrial)
                {
                    path.MoveTo(vector.X, vector.Y);
                    c.DrawText(name, vector.X, vector.Y - 12.5F, _fillPaint);
                    c.DrawText(name, vector.X, vector.Y - 12.5F, _textPaint);
                }
                else if (export.TryGetValue(out bool endsTrial, "EndsTrial") && endsTrial)
                {
                    path.LineTo(vector.X, vector.Y);
                    c.DrawPath(path, _pathPaint);
                    path = new SKPath();
                }
                else path.LineTo(vector.X, vector.Y);
            }

            _bitmaps[1][file] = new MapLayer { Layer = waypointBitmap, IsEnabled = false };
        });
    }

    private async Task LoadPrVendingMachines()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            var set = new HashSet<string>();
            var timeTrialsBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(timeTrialsBitmap);

            var exports = Utils.LoadExports("/PapayaGameplay/LevelOverlays/PapayaGameplay_VendingMachines");
            foreach (var export in exports)
            {
                if (!export.ExportType.Equals("B_Papaya_VendingMachine_Boat_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_BoogieBomb_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_Burger_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_CrashPad_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_FishingPole_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_Grappler_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_Jetpack_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_PaintGrenade_Blue_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_PaintGrenade_Red_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_PaintLauncher_Blue_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_PaintLauncher_Red_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_PlungerBow_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_Quad_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Papaya_VendingMachine_Tomato_C", StringComparison.OrdinalIgnoreCase)) continue;

                if (!export.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                    !Utils.TryGetPackageIndexExport(rootComponent, out UObject root) ||
                    !root.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                var name = export.ExportType.SubstringAfter("B_Papaya_VendingMachine_").SubstringBeforeLast("_C");
                var vector = GetMapPosition(relativeLocation, _prRadius);
                c.DrawPoint(vector.X, vector.Y, _pathPaint);
                if (!set.Add(name)) continue;

                c.DrawText(name, vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText(name, vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[1]["PapayaGameplay_VendingMachines"] = new MapLayer { Layer = timeTrialsBitmap, IsEnabled = false };
        });
    }

    private async Task LoadMusicBlocks()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            var shootingTargetsBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(shootingTargetsBitmap);

            var bDone = false;
            var exports = Utils.LoadExports("/PapayaGameplay/LevelOverlays/PapayaGameplay_MusicBlocks");
            foreach (var export in exports)
            {
                if (!export.ExportType.Equals("MusicBlock_Piano3_Papaya_C", StringComparison.OrdinalIgnoreCase)) continue;

                if (!export.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                    !Utils.TryGetPackageIndexExport(rootComponent, out UObject uObject) ||
                    !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                var vector = GetMapPosition(relativeLocation, _prRadius);
                c.DrawPoint(vector.X, vector.Y, _pathPaint);
                if (bDone) continue;

                bDone = true;
                c.DrawText("Music Blocks", vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText("Music Blocks", vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[1]["PapayaGameplay_MusicBlocks"] = new MapLayer { Layer = shootingTargetsBitmap, IsEnabled = false };
        });
    }

    private async Task LoadUpgradeBenches()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            var upgradeBenchesBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(upgradeBenchesBitmap);

            var exports = Utils.LoadExports("/NPCLibrary/LevelOverlays/Artemis_Overlay_S19_UpgradeBenches");
            foreach (var export in exports)
            {
                if (!export.ExportType.Equals("B_Athena_Spawner_UpgradeStation_C", StringComparison.OrdinalIgnoreCase)) continue;
                var displayName = export.Name["B_Athena_Spawner_".Length..];

                if (!export.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                    !Utils.TryGetPackageIndexExport(rootComponent, out UObject uObject) ||
                    !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                var vector = GetMapPosition(relativeLocation, _brRadius);
                c.DrawPoint(vector.X, vector.Y, _pathPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[0]["ApolloGameplay_UpgradeBenches"] = new MapLayer { Layer = upgradeBenchesBitmap, IsEnabled = false };
        });
    }

    private async Task LoadPhonebooths()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            var phoneboothsBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(phoneboothsBitmap);

            var exports = Utils.LoadExports("/NPCLibrary/LevelOverlays/Apollo_Terrain_NPCLibrary_Stations_Phonebooths");
            foreach (var export in exports)
            {
                if (!export.ExportType.Equals("B_Athena_Spawner_Payphone_C", StringComparison.OrdinalIgnoreCase)) continue;
                var displayName = export.Name["B_Athena_Spawner_".Length..];

                if (!export.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                    !Utils.TryGetPackageIndexExport(rootComponent, out UObject uObject) ||
                    !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                var vector = GetMapPosition(relativeLocation, _brRadius);
                c.DrawPoint(vector.X, vector.Y, _pathPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[0]["ApolloGameplay_Phonebooths"] = new MapLayer { Layer = phoneboothsBitmap, IsEnabled = false };
        });
    }

    private async Task LoadBrVendingMachines()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            var vendingMachinesBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(vendingMachinesBitmap);

            var exports = Utils.LoadExports("/NPCLibrary/LevelOverlays/Artemis_Overlay_S19_ServiceStations");
            foreach (var export in exports)
            {
                if (!export.ExportType.Equals("B_Athena_Spawner_VendingMachine_MendingOnly_C", StringComparison.OrdinalIgnoreCase) &&
                    !export.ExportType.Equals("B_Athena_Spawner_VendingMachine_Random_C", StringComparison.OrdinalIgnoreCase)) continue;
                var displayName = $"{(export.ExportType.Contains("Mending") ? "MM" : "WOM")}_{export.Name["B_Athena_Spawner_VendingMachine_Random".Length..]}";

                if (!export.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                    !Utils.TryGetPackageIndexExport(rootComponent, out UObject uObject) ||
                    !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                var vector = GetMapPosition(relativeLocation, _brRadius);
                c.DrawPoint(vector.X, vector.Y, _pathPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[0]["ApolloGameplay_VendingMachines"] = new MapLayer { Layer = vendingMachinesBitmap, IsEnabled = false };
        });
    }

    private async Task LoadBountyBoards()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            var bountyBoardsBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(bountyBoardsBitmap);

            var exports = Utils.LoadExports("/Bounties/Maps/BB_Overlay_ServiceStations");
            foreach (var export in exports)
            {
                if (!export.ExportType.Equals("B_Bounties_Spawner_BountyBoard_C", StringComparison.OrdinalIgnoreCase)) continue;
                var displayName = $"BountyBoard_{export.Name["BP_BountyBoard_C_".Length..]}";

                if (!export.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                    !Utils.TryGetPackageIndexExport(rootComponent, out UObject uObject) ||
                    !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                var vector = GetMapPosition(relativeLocation, _brRadius);
                c.DrawPoint(vector.X, vector.Y, _pathPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[0]["ApolloGameplay_BountyBoards"] = new MapLayer { Layer = bountyBoardsBitmap, IsEnabled = false };
        });
    }

    private async Task LoadTagsLocation()
    {
        await _threadWorkerView.Begin(_ =>
        {
            _fillPaint.StrokeWidth = 5;
            if (!Utils.TryLoadObject("FortniteGame/Content/Quests/QuestTagToLocationDataRows.QuestTagToLocationDataRows", out UDataTable locationData))
                return;

            var tagsLocationBitmap = new SKBitmap(_widthHeight, _widthHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(tagsLocationBitmap);

            foreach (var (key, uObject) in locationData.RowMap)
            {
                if (key.Text.StartsWith("Athena.Location.POI", StringComparison.OrdinalIgnoreCase) ||
                    key.Text.StartsWith("Athena.Location.Unnamed", StringComparison.OrdinalIgnoreCase) ||
                    key.Text.Contains(".Tandem.", StringComparison.OrdinalIgnoreCase) ||
                    !uObject.TryGetValue(out FVector worldLocation, "WorldLocation")) continue;

                var parts = key.Text.Split('.');
                var displayName = parts[^2];
                if (!int.TryParse(parts[^1], out var _))
                    displayName += " " + parts[^1];

                var vector = GetMapPosition(worldLocation, _brRadius);
                c.DrawPoint(vector.X, vector.Y, _pathPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _fillPaint);
                c.DrawText(displayName, vector.X, vector.Y - 12.5F, _textPaint);
            }

            _bitmaps[0]["ApolloGameplay_TagsLocation"] = new MapLayer { Layer = tagsLocationBitmap, IsEnabled = false };
        });
    }
}
