using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using FModel.Creator;
using FModel.Framework;
using FModel.Services;
using SkiaSharp;

namespace FModel.ViewModels
{
    public class MapViewerViewModel : ViewModel
    {
        private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

        private int _mapIndex = -1;
        public int MapIndex // 0 is BR, 1 is PR
        {
            get => _mapIndex;
            set
            {
                switch (value)
                {
                    case 0:
                        _mapRadius = 135000;
                        _mapImage = _brMiniMapImage;
                        break;
                    case 1:
                        _mapRadius = 51000;
                        _mapImage = _prMiniMapImage;
                        break;
                }

                SetProperty(ref _mapIndex, value);
            }
        }

        private int _mapRadius;
        private BitmapImage _brMiniMapImage;
        private BitmapImage _prMiniMapImage;
        private BitmapImage _mapImage;
        public BitmapImage MapImage
        {
            get => _mapImage;
            set => SetProperty(ref _mapImage, value);
        }

        private readonly List<SKBitmap>[] _bitmaps; // first bitmap is the displayed map, others are overlays of the map
        private readonly CUE4ParseViewModel _cue4Parse;

        public MapViewerViewModel(CUE4ParseViewModel cue4Parse)
        {
            _bitmaps = new[] {new List<SKBitmap>(), new List<SKBitmap>()};
            _cue4Parse = cue4Parse;
        }

        public async void Initialize()
        {
            Utils.Typefaces ??= new Typefaces(_cue4Parse);
            await LoadBrMiniMap();
            await LoadPrMiniMap();
            MapIndex = 1; // don't forget br is selected by default
            MapIndex = 0; // this will trigger the br map to be shown
        }

        public BitmapImage GetImageToSave()
        {
            var ret = new SKBitmap(_bitmaps[MapIndex][0].Width, _bitmaps[MapIndex][0].Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            c.DrawBitmap(_bitmaps[MapIndex][0], 0, 0);

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
        
        private readonly SKPaint _imagePaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High,
            Color = SKColors.White, TextAlign = SKTextAlign.Center, TextSize = 25,
            ImageFilter = SKImageFilter.CreateDropShadow(4, 4, 8, 8, SKColors.Black)
        };
        private readonly SKPaint _pathPaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High, IsStroke = true,
            Style = SKPaintStyle.Stroke, StrokeWidth = 10, Color = SKColors.Red,
            ImageFilter = SKImageFilter.CreateDropShadow(4, 4, 8, 8, SKColors.Black)
        };

        private FVector2D GetMapPosition(FVector vector)
        {
            var nx = (vector.Y + _mapRadius) / (_mapRadius * 2) * _bitmaps[MapIndex][0].Width;
            var ny = (1 - (vector.X + _mapRadius) / (_mapRadius * 2)) * _bitmaps[MapIndex][0].Height;
            return new FVector2D(nx, ny);
        }
        
        private async Task LoadBrMiniMap()
        {
            if (_bitmaps[0].Count > 0) return;
            await _threadWorkerView.Begin(_ =>
            {
                if (!Utils.TryLoadObject("FortniteGame/Content/UI/IngameMap/UIMapManagerBR.Default__UIMapManagerBR_C", out UObject mapManager) ||
                    !mapManager.TryGetValue(out UObject mapMaterial, "MapMaterial") ||
                    !mapMaterial.TryGetValue(out FStructFallback cachedExpressionData, "CachedExpressionData") ||
                    !cachedExpressionData.TryGetValue(out FStructFallback parameters, "Parameters") ||
                    !parameters.TryGetValue(out UTexture2D[] textureValues, "TextureValues")) return;
                
                _bitmaps[0].Add(Utils.GetBitmap(textureValues[0]));
                _brMiniMapImage = GetImageSource(_bitmaps[0][0]);
            });
        }

        private async Task LoadPrMiniMap()
        {
            if (_bitmaps[1].Count > 0) return;
            await _threadWorkerView.Begin(_ =>
            {
                if (!Utils.TryLoadObject("FortniteGame/Content/UI/IngameMap/UIMapManagerPapaya.Default__UIMapManagerPapaya_C", out UObject mapManager) ||
                    !mapManager.TryGetValue(out UMaterial mapMaterial, "MapMaterial") ||
                    mapMaterial.ReferencedTextures.Count < 1) return;

                _bitmaps[1].Add(Utils.GetBitmap(mapMaterial.ReferencedTextures[0] as UTexture2D));
                _prMiniMapImage = GetImageSource(_bitmaps[1][0]);
            });
        }
    }
}