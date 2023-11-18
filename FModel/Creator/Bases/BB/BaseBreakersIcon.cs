using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using FModel.ViewModels.ApiEndpoints.Models;
using SkiaSharp;

namespace FModel.Creator.Bases.BB;

public class BaseBreakersIcon : UCreator
{
    protected string ItemType { get; set; }
    protected string RarityName { get; set; }

    public BaseBreakersIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Background = new[] { SKColor.Parse("86886f"), SKColor.Parse("86886f") };
        Border = new[] { SKColor.Parse("abae8e"), SKColor.Parse("abae8e") };
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FSoftObjectPath iconTextureAssetData, "IconTextureAssetData", "UnlockPortraitGuideImage"))
            Preview = Utils.GetBitmap(iconTextureAssetData);

        if (Object.TryGetValue(out FText displayName, "DisplayName", "RegionDisplayName", "ZoneName"))
            DisplayName = displayName.Text;

        if (Object.TryGetValue(out FText description, "Description", "RegionShortName", "ZoneDescription"))
            Description = description.Text;

        if (Object.TryGetValue(out FName rarity, "Rarity"))
            RarityName = rarity.ToString().Split("::")[1];

        if (Object.TryGetValue(out FName itemType, "ItemType"))
            ItemType = itemType.ToString().Split("::")[1];
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawPreview(c);
        DrawTextBackground(c);
        DrawDisplayName(c);
        DrawDescription(c);
        DrawToBottom(c, SKTextAlign.Left, RarityName);
        DrawToBottom(c, SKTextAlign.Right, ItemType);

        return new[] { ret };
    }
}
