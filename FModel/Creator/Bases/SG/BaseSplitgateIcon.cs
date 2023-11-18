using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.Views.Resources.Controls;
using SkiaSharp;

namespace FModel.Creator.Bases.SG;

public enum ECustomizationAvailability
{
    Normal,
    Default,
    Challenge,
    Reward,
    Store,
    Partner,
    Streamer,
    DLC,
    VIP,
    CreatorCode,
    BattlePass,
    ReferralPass,
    Reserved,
    Developer,
    Decommissioned,
};

public enum ECustomizationRarity
{
    None,
    Common,
    Rare,
    Epic,
    Legendary
}

public class BaseSplitgateIcon : UCreator
{
    protected ECustomizationAvailability Availability {  get; set; }
    protected string AvailabilityInfo {  get; set; }

    public BaseSplitgateIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
    }

    public void GetRarityScheme(ECustomizationRarity r)
    {
        switch (r)
        {
            case ECustomizationRarity.None:
                Background = new[] { SKColor.Parse("86886f"), SKColor.Parse("86886f") };
                Border = new[] { SKColor.Parse("abae8e"), SKColor.Parse("abae8e") };
                break;

            case ECustomizationRarity.Common:
                Background = new[] { SKColor.Parse("b4cbd8"), SKColor.Parse("b4cbd8") };
                Border = new[] { SKColor.Parse("fdfdfd"), SKColor.Parse("fdfdfd") };
                break;

            case ECustomizationRarity.Rare:
                Background = new[] { SKColor.Parse("43c5e3"), SKColor.Parse("43c5e3") };
                Border = new[] { SKColor.Parse("9fdadd"), SKColor.Parse("9fdadd") };
                break;

            case ECustomizationRarity.Epic:
                Background = new[] { SKColor.Parse("7b2c87"), SKColor.Parse("7b2c87") };
                Border = new[] { SKColor.Parse("9e5aa0"), SKColor.Parse("9e5aa0") };
                break;

            case ECustomizationRarity.Legendary:
                Background = new[] { SKColor.Parse("f4792d"), SKColor.Parse("f4792d") };
                Border = new[] { SKColor.Parse("fac310"), SKColor.Parse("fac310") };
                break;

            default:
                Background = new[] { SKColor.Parse("86886f"), SKColor.Parse("86886f") };
                Border = new[] { SKColor.Parse("abae8e"), SKColor.Parse("abae8e") };
                break;
        }
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FSoftObjectPath iconTextureAssetData, "DisplayImage"))
            Preview = Utils.GetBitmap(iconTextureAssetData);

        if (Object.TryGetValue(out FText displayName, "DisplayName"))
            DisplayName = displayName.Text;

        GetRarityScheme(Object.GetOrDefault("Rarity", ECustomizationRarity.None));

        Availability = Object.GetOrDefault("Availability", ECustomizationAvailability.Normal);

        if (Object.TryGetValue(out string availabilityInfo, "AvailabilityInfo"))
            AvailabilityInfo = availabilityInfo;
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawPreview(c);
        DrawTextBackground(c);
        DrawDisplayName(c);
        DrawToBottom(c, SKTextAlign.Left, Availability.ToString());
        DrawToBottom(c, SKTextAlign.Right, AvailabilityInfo);

        return new[] { ret };
    }
}
