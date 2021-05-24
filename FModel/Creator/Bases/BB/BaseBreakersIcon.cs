using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using SkiaSharp;

namespace FModel.Creator.Bases.BB
{
    public class BaseBreakersIcon : BaseIcon
    {
        public BaseBreakersIcon(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            SeriesBackground = Utils.GetBitmap("WorldExplorers/Content/UMG/Materials/t_TextGradient.t_TextGradient");
            Background = new[] {SKColor.Parse("D0D0D0"), SKColor.Parse("636363")};
            Border = new[] {SKColor.Parse("D0D0D0"), SKColor.Parse("FFFFFF")};
        }

        public override void ParseForInfo()
        {
            if (Object.TryGetValue(out FSoftObjectPath iconTextureAssetData, "IconTextureAssetData", "UnlockPortraitGuideImage"))
                Preview = Utils.GetBitmap(iconTextureAssetData);

            if (Object.TryGetValue(out FText displayName, "DisplayName", "RegionDisplayName", "ZoneName"))
                DisplayName = displayName.Text;
            if (Object.TryGetValue(out FText description, "Description", "RegionShortName", "ZoneDescription"))
                Description = description.Text;
        }

        public override SKImage Draw()
        {
            using var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            DrawBackground(c);
            DrawPreview(c);
            DrawTextBackground(c);
            DrawDisplayName(c);
            DrawDescription(c);

            return SKImage.FromBitmap(ret);
        }
    }
}