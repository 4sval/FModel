using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FModel.Creator.Bases.FN;
using SkiaSharp;

namespace FModel.Creator.Bases.SOD2
{
    public class BaseDecayIcon : BaseIcon
    {
        private int _maxStackCount;
        private readonly SKBitmap _backgroundOverlay;

        public BaseDecayIcon(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            Margin = 0;
            Background = new[] {SKColor.Parse("000000"), SKColor.Parse("B24E18")};
            _backgroundOverlay = Utils.GetBitmap("StateOfDecay2/Content/Art/UI/settings/icon_sod_eagle_09_square.icon_sod_eagle_09_square").Resize(512);
        }

        public override void ParseForInfo()
        {
            if (Object.TryGetValue(out FStructFallback stackingInfo, "StackingInfo") &&
                stackingInfo.TryGetValue(out int maxStackCount, "MaxStackCount"))
                _maxStackCount = maxStackCount;
            
            if (Object.Class.SuperStruct != null && Utils.TryGetPackageIndexExport(Object.Class.SuperStruct, out UObject t))
            {
                // TODO
            }
            
            if (Object.TryGetValue(out FStructFallback displayInfo, "DisplayInfo"))
            {
                if (displayInfo.TryGetValue(out FText displayName, "DisplayName"))
                    DisplayName = displayName.Text;
                if (displayInfo.TryGetValue(out FText displayDescription, "DisplayDescription"))
                    Description = displayDescription.Text;

                if (displayInfo.TryGetValue(out string iconFills, "IconFills"))
                    Preview = Utils.GetBitmap(iconFills);
            }
        }

        // TODO
        //
        // ExtraLargeBackpackBase - for extra large
        // SmallBackpackBase - for small
        // ExtraSmallBackpackBase - for extra small
        // MediumBackpackBase - for medium
        // LargeBackpackBase - for large

        public override SKImage Draw()
        {
            using var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            DrawBackground(c);
            DrawPreview(c);

            return SKImage.FromBitmap(ret);
        }

        private new void DrawPreview(SKCanvas c)
        {
            c.DrawBitmap(Preview ?? DefaultPreview, new SKRect(Margin, Margin, Width - Margin, Height - Margin), new SKPaint
            {
                // BlendMode = SKBlendMode.SrcATop -- Need Asval's assistance.
            });
        }

        private new void DrawBackground(SKCanvas c)
        {
            c.DrawRect(new SKRect(Margin, Margin, Width - Margin, Height - Margin),
                new SKPaint
                {
                    IsAntialias = true, FilterQuality = SKFilterQuality.High,
                    Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, Height / 2), Width / 5 * 2,
                        new[] {Background[0], Background[1]},
                        SKShaderTileMode.Clamp)
                });

            for (var i = 0; i < _backgroundOverlay.Width; i++)
            for (var j = 0; j < _backgroundOverlay.Height; j++)
                if (_backgroundOverlay.GetPixel(i, j) == SKColors.Black)
                {
                    _backgroundOverlay.SetPixel(i, j, SKColors.Transparent);
                }
            
            c.DrawBitmap(_backgroundOverlay, new SKRect(Margin, Margin, Width - Margin, Height - Margin), new SKPaint
            {
                IsAntialias = true,
                ColorFilter = SKColorFilter.CreateBlendMode(SKColors.Black.WithAlpha(150), SKBlendMode.DstIn),
                ImageFilter = SKImageFilter.CreateDropShadow(2, 2, 4, 4, new SKColor(0, 0, 0))
            });
            c.DrawColor(SKColors.Black.WithAlpha(125), SKBlendMode.DstIn);
        }
    }
}