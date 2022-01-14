using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.SB
{
    public class BaseDivision : UCreator
    {
        public BaseDivision(UObject uObject, EIconStyle style) : base(uObject, style)
        {
        }

        public override void ParseForInfo()
        {
            if (Object.TryGetValue(out FPackageIndex icon, "Icon", "IconNoTier"))
            {
                Preview = Utils.GetBitmap(icon);
            }

            if (Object.TryGetValue(out FLinearColor lightColor, "UILightColor") &&
                Object.TryGetValue(out FLinearColor mediumColor, "UIMediumColor") &&
                Object.TryGetValue(out FLinearColor darkColor, "UIDarkColor") &&
                Object.TryGetValue(out FLinearColor cardColor, "UICardColor"))
            {
                Background = new[] {SKColor.Parse(lightColor.Hex), SKColor.Parse(cardColor.Hex)};
                Border = new[] {SKColor.Parse(mediumColor.Hex), SKColor.Parse(darkColor.Hex)};
            }

            if (Object.TryGetValue(out FText displayName, "DisplayName"))
                DisplayName = displayName.Text;
        }

        public override SKBitmap Draw()
        {
            var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            DrawBackground(c);
            DrawPreview(c);
            DrawTextBackground(c);
            DrawDisplayName(c);

            return ret;
        }
    }
}
