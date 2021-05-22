using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports;
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
    public class MapViewerViewModel : ViewModel
    {
        private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

        private bool _showCities;
        public bool ShowCities
        {
            get => _showCities;
            set => SetProperty(ref _showCities, value, nameof(ShowCities));
        }

        private bool _showPatrolPaths;
        public bool ShowPatrolPaths
        {
            get => _showPatrolPaths;
            set => SetProperty(ref _showPatrolPaths, value, nameof(ShowPatrolPaths));
        }

        private BitmapImage _mapImage;
        public BitmapImage MapImage
        {
            get => _mapImage;
            set => SetProperty(ref _mapImage, value);
        }

        private BitmapImage _citiesImage;
        public BitmapImage CitiesImage
        {
            get => _citiesImage;
            set => SetProperty(ref _citiesImage, value);
        }

        private BitmapImage _patrolPathImage;
        public BitmapImage PatrolPathImage
        {
            get => _patrolPathImage;
            set => SetProperty(ref _patrolPathImage, value);
        }

        private readonly CUE4ParseViewModel _cue4Parse;

        public MapViewerViewModel(CUE4ParseViewModel cue4Parse)
        {
            _cue4Parse = cue4Parse;
        }

        public async void Initialize()
        {
            Utils.Typefaces ??= new Typefaces(_cue4Parse);
            if (MapImage == null && _mapBitmap == null)
            {
                await LoadMiniMap();
            }
        }

        public BitmapImage GetImageToSave()
        {
            var ret = new SKBitmap(_mapBitmap.Width, _mapBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            c.DrawBitmap(_mapBitmap, 0, 0);
            if (ShowCities)
                c.DrawBitmap(_citiesBitmap, 0, 0);
            if (ShowPatrolPaths)
                c.DrawBitmap(_patrolPathBitmap, 0, 0);

            return GetImageSource(ret);
        }

        protected override bool SetProperty<T>(ref T storage, T value, string propertyName = null)
        {
            var ret = base.SetProperty(ref storage, value, propertyName);
            CheckForStuffToDraw(propertyName);
            return ret;
        }

        private async void CheckForStuffToDraw(string propertyName = null)
        {
            switch (propertyName)
            {
                case nameof(ShowCities) when _citiesBitmap == null && _mapBitmap != null:
                {
                    await LoadCities();
                    break;
                }
                case nameof(ShowPatrolPaths) when _patrolPathBitmap == null && _mapBitmap != null:
                {
                    await LoadPatrolPaths();
                    break;
                }
            }
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

        private const int WorldRadius = 135000;
        private SKBitmap _mapBitmap;
        private SKBitmap _citiesBitmap;
        private SKBitmap _patrolPathBitmap;
        private readonly SKBitmap _pinBitmap =
            SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/pin.png"))?.Stream);
        private readonly SKBitmap _cityPinBitmap =
            SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/city_pin.png"))?.Stream);
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

        private FVector2D GetMapPosition(FVector vector)
        {
            var nx = (vector.Y + WorldRadius) / (WorldRadius * 2) * _mapBitmap.Width;
            var ny = (1 - (vector.X + WorldRadius) / (WorldRadius * 2)) * _mapBitmap.Height;
            return new FVector2D(nx, ny);
        }

        private async Task LoadMiniMap()
        {
            await _threadWorkerView.Begin(_ =>
            {
                if (!Utils.TryLoadObject("FortniteGame/Content/UI/IngameMap/UIMapManagerBR.Default__UIMapManagerBR_C", out UObject mapManager) ||
                    !mapManager.TryGetValue(out UObject mapMaterial, "MapMaterial") ||
                    !mapMaterial.TryGetValue(out FStructFallback cachedExpressionData, "CachedExpressionData") ||
                    !cachedExpressionData.TryGetValue(out FStructFallback parameters, "Parameters") ||
                    !parameters.TryGetValue(out UTexture2D[] textureValues, "TextureValues")) return;

                _imagePaint.Typeface = Utils.Typefaces.Bottom ?? Utils.Typefaces.DisplayName;
                _mapBitmap = Utils.GetBitmap(textureValues[0]);
                MapImage = GetImageSource(_mapBitmap);
            });
        }

        private async Task LoadCities()
        {
            await _threadWorkerView.Begin(_ =>
            {
                _citiesBitmap = new SKBitmap(_mapBitmap.Width, _mapBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var c = new SKCanvas(_citiesBitmap);
                if (Utils.TryLoadObject("FortniteGame/Content/Quests/QuestIndicatorData", out UObject indicatorData) &&
                    indicatorData.TryGetValue(out FStructFallback[] challengeMapPoiData, "ChallengeMapPoiData"))
                {
                    foreach (var poiData in challengeMapPoiData)
                    {
                        if (!poiData.TryGetValue(out FSoftObjectPath discoveryQuest, "DiscoveryQuest") ||
                            !poiData.TryGetValue(out FText text, "Text") ||
                            !poiData.TryGetValue(out FVector worldLocation, "WorldLocation") ||
                            discoveryQuest.AssetPathName.Text.Contains("Landmarks")) continue;
                        var shaper = new CustomSKShaper(_imagePaint.Typeface);
                        var shapedText = shaper.Shape(text.Text, _imagePaint);

                        var vector = GetMapPosition(worldLocation);
                        c.DrawPoint(vector.X, vector.Y, _pathPaint);
                        c.DrawBitmap(_cityPinBitmap, vector.X - 50, vector.Y - 90, _imagePaint);
                        c.DrawShapedText(shaper, text.Text, vector.X - shapedText.Points[^1].X / 2, vector.Y - 12.5F, _imagePaint);
                    }
                }

                CitiesImage = GetImageSource(_citiesBitmap);
            });
        }

        private async Task LoadPatrolPaths()
        {
            await _threadWorkerView.Begin(_ =>
            {
                _patrolPathBitmap = new SKBitmap(_mapBitmap.Width, _mapBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var c = new SKCanvas(_patrolPathBitmap);

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
                        if (!(export is UObject uObject)) continue;
                        if (!uObject.ExportType.Equals("FortAthenaPatrolPath", StringComparison.OrdinalIgnoreCase) ||
                            !uObject.TryGetValue(out FGameplayTagContainer gameplayTags, "GameplayTags") ||
                            !uObject.TryGetValue(out FPackageIndex[] patrolPoints, "PatrolPoints")) continue;

                        if (!Utils.TryGetPackageIndexExport(patrolPoints[0], out uObject) ||
                            !uObject.TryGetValue(out FPackageIndex rootComponent, "RootComponent") ||
                            !Utils.TryGetPackageIndexExport(rootComponent, out uObject) ||
                            !uObject.TryGetValue(out FVector relativeLocation, "RelativeLocation")) continue;

                        var path = new SKPath();
                        var vector = GetMapPosition(relativeLocation);
                        path.MoveTo(vector.X, vector.Y);

                        for (var i = 1; i < patrolPoints.Length; i++)
                        {
                            if (!Utils.TryGetPackageIndexExport(patrolPoints[i], out uObject) ||
                                !uObject.TryGetValue(out rootComponent, "RootComponent") ||
                                !Utils.TryGetPackageIndexExport(rootComponent, out uObject) ||
                                !uObject.TryGetValue(out relativeLocation, "RelativeLocation")) continue;

                            vector = GetMapPosition(relativeLocation);
                            path.LineTo(vector.X, vector.Y);
                        }

                        path.Close();
                        c.DrawPath(path, _pathPaint);
                        c.DrawBitmap(_pinBitmap, vector.X - 50, vector.Y - 90, _imagePaint);
                        c.DrawText(gameplayTags.GameplayTags[0].Text.SubstringAfterLast("."), vector.X, vector.Y - 12.5F, _imagePaint);
                    }
                }

                PatrolPathImage = GetImageSource(_patrolPathBitmap);
            });
        }
    }
}