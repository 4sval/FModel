using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using SkiaSharp;

namespace FModel.Creator.Bases.MV;

public class BaseMultiVersusIcon : BaseIcon
{
    public BaseMultiVersusIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        DefaultPreview = Utils.GetBitmap("MultiVersus/Content/Panda_Main/UI/PreMatch/Images/DiamondPortraits/0010_Random.0010_Random");
    }

    public override void ParseForInfo()
    {
        var rarity = Object.GetOrDefault("Rarity", new FName("ERewardRarity::None"));
        Background = GetRarityBackground(rarity.ToString());
        Border = new[] { GetRarityBorder(rarity.ToString()) };

        if (Object.TryGetValue(out FSoftObjectPath rewardThumbnail, "RewardThumbnail", "DisplayTextureRef"))
            Preview = Utils.GetBitmap(rewardThumbnail);
        else if (Object.TryGetValue(out FPackageIndex icon, "Icon"))
            Preview = Utils.GetBitmap(icon);

        if (Object.TryGetValue(out FText displayName, "DisplayName"))
            DisplayName = displayName.Text;
        if (Object.TryGetValue(out FText description, "Description"))
            Description = Utils.RemoveHtmlTags(description.Text);

        if (Object.TryGetValue(out int xpValue, "XPValue"))
            DisplayName += $" (+{xpValue})";
    }

    // public override SKBitmap[] Draw()
    // {
    //     // dedicated design here, use : UCreator
    //     throw new System.NotImplementedException();
    // }

    private SKColor[] GetRarityBackground(string rarity)
    {
        switch (rarity.Split("::")[1]) // the colors here are the base color and brighter color that the game uses for rarities from the "Rarity to Color" blueprint function
        {
            case "Common":
                return new[]
                {
                    SKColor.Parse(new FLinearColor(0.068478f, 0.651406f, 0.016807f, 1.000000f).Hex),
                    SKColor.Parse(new FLinearColor(0.081422f, 1.000000f, 0.000000f, 1.000000f).Hex)
                };
            case "Rare":
                return new[]
                {
                    SKColor.Parse(new FLinearColor(0.035911f, 0.394246f, 0.900000f, 1.000000f).Hex),
                    SKColor.Parse(new FLinearColor(0.033333f, 0.434207f, 1.000000f, 1.000000f).Hex)
                };
            case "Epic":
                return new[]
                {
                    SKColor.Parse(new FLinearColor(0.530391f, 0.060502f, 0.900000f, 1.000000f).Hex),
                    SKColor.Parse(new FLinearColor(0.579907f, 0.045833f, 1.000000f, 1.000000f).Hex)
                };
            case "Legendary":
                return new[]
                {
                    SKColor.Parse(new FLinearColor(1.000000f, 0.223228f, 0.002428f, 1.000000f).Hex),
                    SKColor.Parse(new FLinearColor(1.000000f, 0.479320f, 0.030713f, 1.000000f).Hex)
                };
            case "None":
            default:
                return new[]
                {
                    SKColor.Parse(new FLinearColor(0.194618f, 0.651406f, 0.630757f, 1.000000f).Hex),
                    SKColor.Parse(new FLinearColor(0.273627f, 0.955208f, 0.914839f, 1.000000f).Hex)
                };
        }
    }

    private SKColor GetRarityBorder(string rarity)
    {
        switch (rarity.Split("::")[1]) // the colors here are the desaturated versions of the rarity colors
        {
            case "Common":
                return SKColor.Parse(new FLinearColor(0.172713f, 0.651406f, 0.130281f, 1.000000f).Hex);
            case "Rare":
                return SKColor.Parse(new FLinearColor(0.198220f, 0.527026f, 0.991102f, 1.000000f).Hex);
            case "Epic":
                return SKColor.Parse(new FLinearColor(0.642017f, 0.198220f, 0.991102f, 1.000000f).Hex);
            case "Legendary":
                return SKColor.Parse(new FLinearColor(1.000000f, 0.328434f, 0.200000f, 1.000000f).Hex);
            case "None":
            default:
                return SKColor.Parse(new FLinearColor(0.308843f, 0.571125f, 0.557810f, 1.000000f).Hex);
        }
    }
}
