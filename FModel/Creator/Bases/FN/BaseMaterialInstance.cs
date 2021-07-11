using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.FN
{
    public class BaseMaterialInstance : BaseIcon
    {
        public BaseMaterialInstance(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            Background = new[] {SKColor.Parse("4F4F69"), SKColor.Parse("4F4F69")};
            Border = new[] {SKColor.Parse("9092AB")};
        }

        public override void ParseForInfo()
        {
            if (Object is not UMaterialInstanceConstant material) return;

            texture_finding:
            foreach (var textureParameter in material.TextureParameterValues) // get texture from base material
            {
                if (textureParameter.ParameterValue is not UTexture2D texture || Preview != null) continue;
                switch (textureParameter.ParameterInfo.Name.Text)
                {
                    case "SeriesTexture":
                        GetSeries(texture);
                        break;
                    case "TextureA":
                    case "TextureB":
                    case "OfferImage":
                        Preview = Utils.GetBitmap(texture);
                        break;
                }
            }

            while (material.VectorParameterValues.Length == 0 || // try to get color from parent if not found here
                   material.VectorParameterValues.All(x => x.ParameterInfo.Name.Text.Equals("FallOff_Color"))) // use parent if it only contains FallOff_Color
            {
                if (material.TryGetValue(out FPackageIndex parent, "Parent"))
                    Utils.TryGetPackageIndexExport(parent, out material);
                else return;

                if (material == null) return;
            }

            if (Preview == null)
                goto texture_finding;

            foreach (var vectorParameter in material.VectorParameterValues)
            {
                if (vectorParameter.ParameterValue == null) continue;
                switch (vectorParameter.ParameterInfo.Name.Text)
                {
                    case "Background_Color_A":
                        Background[0] = SKColor.Parse(vectorParameter.ParameterValue.Value.Hex);
                        Border[0] = Background[0];
                        break;
                    case "Background_Color_B": // Border color can be defaulted here in some case where Background_Color_A should be taken from parent but Background_Color_B from base
                        Background[1] = SKColor.Parse(vectorParameter.ParameterValue.Value.Hex);
                        break;
                }
            }
        }

        public override SKImage Draw()
        {
            using var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            switch (Style)
            {
                case EIconStyle.NoBackground:
                    DrawPreview(c);
                    break;
                default:
                    DrawBackground(c);
                    DrawPreview(c);
                    break;
            }

            return SKImage.FromBitmap(ret);
        }
    }
}