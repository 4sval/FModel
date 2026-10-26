using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using SkiaSharp;

namespace FModel.Creator.Bases.BB;

public class BaseBreakersIcon : BaseIcon
{
    public BaseBreakersIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        SeriesBackground = Utils.GetBitmap("");
        Background = new[] { SKColor.Parse("1B569B"), SKColor.Parse("062447") };
        Border = new[] { SKColor.Parse("D0D0D0"), SKColor.Parse("FFFFFF") };
    }

    public override void ParseForInfo()
    {
        Preview = GetRoomArt();

        var eventBanner = GetEventBanner();
        if (eventBanner != null)
            Preview = eventBanner;

        if (Object.TryGetValue(out FSoftObjectPath iconTextureAssetData, "IconTextureAssetData", "UnlockPortraitGuideImage", "ImageAssetData") &&
            Preview == null)
        {
            Preview = Utils.GetBitmap(iconTextureAssetData);
        }

        if (Object.TryGetValue(out FText displayName, "DisplayName", "RegionDisplayName", "ZoneName", "EventName", "NickName", "Title"))
            DisplayName = displayName.Text;

        if (Object.TryGetValue(out FText description, "Description", "RegionShortName", "ZoneDescription", "EventDescription"))
            Description = description.Text;
    }

    private SKBitmap GetTextureFromMaterial(UMaterialInstanceConstant material)
    {
        foreach (var parameter in material.TextureParameterValues)
        {
            if (!parameter.ParameterInfo.Name.Text.Equals(
                    "SkyTexture",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            if (!parameter.ParameterValue.TryLoad<UTexture2D>(
                    out var texture))
                return null;
            return Utils.GetBitmap(texture);
        }
        return null;
    }

    private SKBitmap GetTextureFromMaterial2(FSoftObjectPath materialPath)
    {
        if (!Utils.TryLoadObject(
                materialPath.AssetPathName.Text,
                out UMaterialInstanceConstant material))
            return null;

        foreach (var parameter in material.TextureParameterValues)
        {
            if (!parameter.ParameterInfo.Name.Text.Equals(
                    "TexturePack",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            if (parameter.ParameterValue.TryLoad<UTexture2D>(out var texture))
                return Utils.GetBitmap(texture);
        }
        return null;
    }

    private SKBitmap GetEventBanner()
    {
        FSoftObjectPath bannerPath;
        FSoftObjectPath borderPath;
        if (Object.TryGetValue(out FSoftObjectPath directBanner, "BannerMat") &&
            Object.TryGetValue(out FSoftObjectPath directBorder, "BorderMat"))
        {
            bannerPath = directBanner;
            borderPath = directBorder;
        }
        else if (Object.TryGetValue(out FStructFallback overrides, "Overrides") &&
                 overrides.TryGetValue(out FSoftObjectPath overrideBanner, "BannerMat") &&
                 overrides.TryGetValue(out FSoftObjectPath overrideBorder, "BorderMat"))
        {
            bannerPath = overrideBanner;
            borderPath = overrideBorder;
        }
        else
        {
            return null;
        }
        var bannerBitmap = GetTextureFromMaterial2(bannerPath);
        var borderBitmap = GetTextureFromMaterial2(borderPath);

        if (bannerBitmap == null || borderBitmap == null)
            return null;
        var result = new SKBitmap(
            bannerBitmap.Width,
            bannerBitmap.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(bannerBitmap, 0, 0);

        using var paint = new SKPaint
        {
            BlendMode = SKBlendMode.SrcOver,
            IsAntialias = true
        };
        canvas.DrawBitmap(borderBitmap, 0, 0, paint);
        return result;
    }

    private SKBitmap ComposeRoomArt(SKBitmap foreground, SKBitmap background)
    {
    int halfHeight = background.Height / 2;
    int splicedWidth = background.Width * 2;
    float scale = (float)foreground.Width / splicedWidth;
    int splicedHeight = (int)(halfHeight * scale);
    var result = new SKBitmap(
        foreground.Width,
        foreground.Height,
        SKColorType.Rgba8888,
        SKAlphaType.Premul);

    using var canvas = new SKCanvas(result);
    canvas.Clear(SKColors.Transparent);
    const float backgroundY = 0;
    float halfWidth = foreground.Width / 2f;

    canvas.DrawBitmap(
        background,
        new SKRect(
            0,
            0,
            background.Width,
            halfHeight),
        new SKRect(
            0,
            backgroundY,
            halfWidth,
            backgroundY + splicedHeight));

    canvas.DrawBitmap(
        background,
        new SKRect(
            0,
            halfHeight,
            background.Width,
            background.Height),
        new SKRect(
            halfWidth,
            backgroundY,
            foreground.Width,
            backgroundY + splicedHeight));

        const float foregroundX = 0;
        const float foregroundY = 10;
        canvas.DrawBitmap(
            foreground,
            foregroundX,
            foregroundY);

        return result;
    }

    private SKBitmap GetRoomArt()
    {
        FStructFallback art;
        if (Object.TryGetValue(out FStructFallback[] roomArts, "RoomArt") &&
            roomArts.Length > 0)
        {
            art = roomArts[0];
        }
        else if (Object.TryGetValue(out FStructFallback treasureArt, "TreasureArt"))
        {
            art = treasureArt;
        }
        else
        {
            return null;
        }
        SKBitmap foreground = null;
        SKBitmap background = null;

        if (art.TryGetValue(
                out FPackageIndex foregroundIndex,
                "ForegroundTexture") &&
            Utils.TryGetPackageIndexExport(
                foregroundIndex,
                out UTexture2D foregroundTexture))
        {
            foreground = Utils.GetBitmap(foregroundTexture);
        }

        if (art.TryGetValue(
                out FPackageIndex backgroundIndex,
                "BackgroundMat") &&
            Utils.TryGetPackageIndexExport(
                backgroundIndex,
                out UMaterialInstanceConstant backgroundMaterial))
        {
            background = GetTextureFromMaterial(backgroundMaterial);
        }

        if (foreground != null && background != null)
            return ComposeRoomArt(foreground, background);

        if (foreground != null)
            return foreground;

        if (background != null)
            return background;

        return null;
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

        return new[] { ret };
    }
}
