using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using SkiaSharp;

namespace FModel.Creator.Bases.MV;

public class BaseMultiversusIcon : BaseIcon
{
    public BaseMultiversusIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue<FLinearColor>(out var backgroundColor, "BackgroundColor"))
        {
            var bgColor = SKColor.Parse(backgroundColor.Hex);
            Background = new[] { bgColor, bgColor };
        }
        else if (Object.TryGetValue<FName>(out var rarity, "Rarity"))
        {
            Background = GetRarityBackground(rarity.ToString());
            Border = new[] { GetRarityBorder(rarity.ToString()) };
        }

        if (Object.TryGetValue<FLinearColor>(out var nameColor, "DisplayNameColor"))
        {
            Border = new[] { SKColor.Parse(nameColor.Hex) };
        }

        if (Object.TryGetValue<FSoftObjectPath>(out var rewardThumbnail, "RewardThumbnail"))
            Preview = Utils.GetBitmap(rewardThumbnail);

        else if (Object.TryGetValue<FSoftObjectPath>(out var portaitPtr, "CollectionsPortraitMaterial", "CharacterSelectPortraitMaterial")
            && portaitPtr.TryLoad<UMaterialInstanceConstant>(out var portait))
            Preview = Utils.GetBitmap(portait);

        else if (Object.TryGetValue<FSoftObjectPath[]>(out var skins, "Skins") &&
            skins[0].TryLoad(out var defaultSkin) &&
            defaultSkin.TryGetValue<FSoftObjectPath>(out var skinRewardThumb, "RewardThumbnail"))
            Preview = Utils.GetBitmap(skinRewardThumb);

        else if (Object.TryGetValue<FSoftObjectPath>(out var eogPortrait, "EOGPortrait"))
            Preview = Utils.GetBitmap(eogPortrait);

        if (Object.TryGetValue<FText>(out var displayName, "DisplayName"))
            DisplayName = displayName.Text;

        if (Object.TryGetValue<FText>(out var description, "Overview", "Bio"))
            Description = description.Text;

        if (Object.TryGetValue<FText>(out var property, "Property"))
            ShortDescription = property.Text;

        if (Object.TryGetValue<FName>(out var unlockLocation, "UnlockLocation"))
            CosmeticSource = unlockLocation.ToString().Split("::")[1];
    }

    private static SKColor[] GetRarityBackground(string rarity)
    {
        string rarityName = rarity.Split("::")[1];

        switch (rarityName) // the colors here are the base color and brighter color that the game uses for rarities from the "Rarity to Color" blueprint function
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

    private static SKColor GetRarityBorder(string rarity)
    {
        string rarityName = rarity.Split("::")[1];

        switch (rarityName) // the colors here are the desaturated versions of the rarity colors
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
