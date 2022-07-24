using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using SkiaSharp;

namespace FModel.Creator.Bases.SB;

public class BaseSpellIcon : BaseIcon
{
    private SKBitmap _seriesBackground2;

    private readonly SKPaint _overlayPaint = new()
    {
        FilterQuality = SKFilterQuality.High,
        IsAntialias = true,
        Color = SKColors.Transparent.WithAlpha(75)
    };

    public BaseSpellIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Background = new[] { SKColor.Parse("FFFFFF"), SKColor.Parse("636363") };
        Border = new[] { SKColor.Parse("D0D0D0"), SKColor.Parse("FFFFFF") };
        Width = Object.ExportType.StartsWith("GCosmeticCard") ? 1536 : 512;
        Height = Object.ExportType.StartsWith("GCosmeticCard") ? 450 : 512;
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FName rarity, "Rarity"))
            GetRarity(rarity);

        if (Object.TryGetValue(out FSoftObjectPath preview, "IconTexture", "OfferTexture", "PortraitTexture"))
            Preview = Utils.GetBitmap(preview);
        else if (Object.TryGetValue(out FPackageIndex icon, "IconTexture", "OfferTexture", "PortraitTexture"))
            Preview = Utils.GetBitmap(icon);

        if (Object.TryGetValue(out FText displayName, "DisplayName", "Title", "Name"))
            DisplayName = displayName.Text;
        if (Object.TryGetValue(out FText description, "Description"))
            Description = description.Text;

        SeriesBackground = Utils.GetBitmap("g3/Content/UI/Textures/assets/HUDAccentFillBox.HUDAccentFillBox");
        _seriesBackground2 = Utils.GetBitmap("g3/Content/UI/Textures/assets/store/ItemBGStatic_UIT.ItemBGStatic_UIT");
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackgrounds(c);
        DrawBackground(c);
        DrawPreview(c);
        DrawTextBackground(c);
        DrawDisplayName(c);
        DrawDescription(c);

        return new[] { ret };
    }

    private void DrawBackgrounds(SKCanvas c)
    {
        if (SeriesBackground != null)
            c.DrawBitmap(SeriesBackground, new SKRect(0, 0, Width, Height), ImagePaint);
        if (_seriesBackground2 != null)
            c.DrawBitmap(_seriesBackground2, new SKRect(0, 0, Width, Height), _overlayPaint);

        var x = Margin * (int) 2.5;
        const int radi = 15;
        c.DrawCircle(x + radi, x + radi, radi, new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(radi, radi), radi * 2 / 5 * 4,
                Background, SKShaderTileMode.Clamp)
        });
    }

    private void GetRarity(FName n)
    {
        if (!Utils.TryLoadObject("g3/Content/UI/UIKit/DT_RarityColors.DT_RarityColors", out UDataTable rarity)) return;

        if (rarity.TryGetDataTableRow(n.Text["EXRarity::".Length..], StringComparison.Ordinal, out var row))
        {
            if (row.TryGetValue(out FLinearColor[] colors, "Colors"))
            {
                Background = new[] { SKColor.Parse(colors[0].Hex), SKColor.Parse(colors[2].Hex) };
                Border = new[] { SKColor.Parse(colors[1].Hex), SKColor.Parse(colors[0].Hex) };
            }
        }
    }
}