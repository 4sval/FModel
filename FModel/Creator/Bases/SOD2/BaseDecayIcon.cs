using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FModel.Creator.Bases.FN;
using FModel.Extensions;
using SkiaSharp;

namespace FModel.Creator.Bases.SOD2
{
    public class BaseDecayIcon : BaseIcon
    {
        public BaseDecayIcon(UObject uObject, EIconStyle style) : base(uObject, style)
        {
        }

        public override void ParseForInfo()
        {
            if (Object.TryGetValue(out FStructFallback displayInfo, "DisplayInfo"))
            {
                if (displayInfo.TryGetValue(out FText displayName, "DisplayName"))
                    DisplayName = displayName.Text;
                if (displayInfo.TryGetValue(out FText displayDescription, "DisplayDescription"))
                    Description = displayDescription.Text;

                if (displayInfo.TryGetValue(out string iconFills, "IconFills"))
                {
                    if (Utils.TryLoadObject("StateOfDecay2/Content/Art/UI/common_art_assets/common_weapons_ammo/icon_weapon_ammo_DLC4_bloater_gas_round_fills.icon_weapon_ammo_DLC4_bloater_gas_round_fills", out UObject a)) // UTexture2D fails too
                    {
                        // FAILS
                    }
                    
                    Preview = Utils.GetBitmap(iconFills.Replace("/Game/", "StateOfDecay2/Content/")); // FAILS
                }
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

            DrawPreview(c);

            return SKImage.FromBitmap(ret);
        }
    }
}