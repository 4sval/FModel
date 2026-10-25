using CUE4Parse.GameTypes.SJ.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using SkiaSharp;

namespace FModel.Creator.Bases.SJ;

public class BaseSpyjinxIcon : BaseIcon
{
    private static readonly SKColor[] RarityColors =
    {
        SKColor.Parse("868D99"), // Common
        SKColor.Parse("89B800"), // Uncommon
        SKColor.Parse("00AAFF"), // Rare
        SKColor.Parse("8E4ECF"), // Epic
        SKColor.Parse("FF8A00")  // Legendary
    };

    private ESpyjinxRarity CurrentRarity = ESpyjinxRarity.Common;

    public BaseSpyjinxIcon(UObject uObject, EIconStyle style)
        : base(uObject, style)
    {
        SetRarity(ESpyjinxRarity.Common);
    }

    public override void ParseForInfo()
    {
        // rarity
        ParseRarity();

        // preview
        if (Object.TryGetValue(out FPackageIndex icon, "ThumbnailTexture", "StoreTexture", "Icon", "IconTexture"))
            Preview = Utils.GetBitmap(icon);

        // display name
        if (Object.TryGetValue(out FText displayName, "Codename", "DisplayName", "PlayerFacingName"))
            DisplayName = displayName.Text;

        // description
        if (Object.TryGetValue(out FText description, "Description", "PlayerFacingDescription"))
            Description = description.Text;

        Description = Utils.RemoveHtmlTags(Description);
    }

    private void ParseRarity()
    {
        if (!Object.TryGetValue(out string rarity, "Rarity"))
            return;
        if (!byte.TryParse(rarity, out var rarityValue))
            return;
        SetRarity((ESpyjinxRarity) rarityValue);
    }

    private void SetRarity(ESpyjinxRarity rarity)
    {
        CurrentRarity = rarity;
        var color = RarityColors[(int) rarity];
        Background = new[] {color, color};
        Border = new[] {color, color};
    }

    private void DrawSpyjinxBackground(SKCanvas c)
    {
        var rarityIndex = (int) CurrentRarity;
        var color = RarityColors[rarityIndex];

        // Solid background
        c.DrawRect(
            new SKRect(0, 0, Width, Height),
            new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = color
            });
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);
        switch (Style)
        {
            case EIconStyle.NoBackground:
                DrawPreview(c);
                break;
            case EIconStyle.NoText:
                DrawSpyjinxBackground(c);
                DrawPreview(c);
                break;
            default:
                DrawSpyjinxBackground(c);
                DrawPreview(c);
                DrawTextBackground(c);
                DrawDisplayName(c);
                DrawDescription(c);
                break;
        }
        return new[] { ret };
    }
}
