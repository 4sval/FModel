using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using SkiaSharp;

namespace FModel.Creator.Bases.SB
{
    public class BaseGameModeInfo : UCreator
    {
        private SKBitmap _icon;

        public BaseGameModeInfo(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            Width = 738;
            Height = 1024;
        }

        public override void ParseForInfo()
        {
            if (Object.TryGetValue(out FText displayName, "DisplayName"))
                DisplayName = displayName.Text;
            if (Object.TryGetValue(out FText description, "Description"))
                Description = description.Text;
            if (Object.TryGetValue(out UMaterialInstanceConstant portrait, "Portrait"))
                Preview = Utils.GetBitmap(portrait);
            if (Object.TryGetValue(out UTexture2D icon, "Icon"))
                _icon = Utils.GetBitmap(icon).Resize(25);
        }

        public override SKBitmap[] Draw()
        {
            var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            DrawPreview(c);
            DrawTextBackground(c);
            DrawDisplayName(c);
            DrawIcon(c);

            return new []{ret};
        }

        private void DrawIcon(SKCanvas c)
        {
            if (_icon == null) return;
            c.DrawBitmap(_icon, new SKPoint(5, 5), ImagePaint);
        }
    }
}
