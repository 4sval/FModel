using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using FModel.Framework;
using FModel.ViewModels;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.Views.Resources.Controls;
using SkiaSharp;

namespace FModel.Creator.Bases.VAL;


public class BaseEquippableSkin : UCreator
{
    protected SKBitmap Wallpaper { get; set; }

    public BaseEquippableSkin(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Background = new[] { SKColor.Parse("#262630"), SKColor.Parse("#1f1f26") };
        Border = new[] { SKColor.Parse("#262630"), SKColor.Parse("#1f1f26") };

        Width = 640;
        Height = 360;

        StarterTextPos = 270;
        ImageMargin = 64;
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FSoftObjectPath UIDataPath, "UIData"))
        {
            var UIDataClass = UIDataPath.Load();
            var UIData = (UIDataClass as UBlueprintGeneratedClass).ClassDefaultObject.Load();

            if (UIData.TryGetValue(out FText displayName, "DisplayName"))
                DisplayName = displayName.Text;

            if (UIData.TryGetValue(out UObject iconTextureAssetData, "DisplayIcon"))
                Preview = Utils.GetBitmap(iconTextureAssetData as UTexture2D);
        }

        if (Object.TryGetValue(out FSoftObjectPath ThemePath, "Theme"))
        {
            var ThemeClass = ThemePath.Load();
            var Theme = (ThemeClass as UBlueprintGeneratedClass).ClassDefaultObject.Load();

            if (Theme.TryGetValue(out FSoftObjectPath ThemeUIDataPath, "UIData"))
            {
                var UIDataClass = ThemeUIDataPath.Load();
                var UIData = (UIDataClass as UBlueprintGeneratedClass).ClassDefaultObject.Load();

                if (UIData.TryGetValue(out FText displayName, "DisplayName"))
                    Description = displayName.Text;
            }
        }

        if (Object.TryGetValue(out FSoftObjectPath WallpaperPath, "Wallpaper"))
            Wallpaper = Utils.GetBitmap(WallpaperPath);
    }

    protected void DrawItemPreview(SKCanvas c)
    {
        if (Preview != null)
            c.DrawBitmap(Preview, (Width - Preview.Width) / 2, (Height - Preview.Height) / 2, ImagePaint);
    }

    protected void DrawWallpaper(SKCanvas c)
    {
        if (Wallpaper != null)
            c.DrawBitmap(Wallpaper, new SKRect(Margin, Margin, Width - Margin, Height - Margin), ImagePaint);
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawWallpaper(c);
        DrawItemPreview(c);
        DrawTextBackground(c);
        DrawDisplayName(c);
        DrawDescription(c);

        return new[] { ret };
    }
}
