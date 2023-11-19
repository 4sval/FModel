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


public class BaseEquippableCharm : UCreator
{
    public BaseEquippableCharm(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Background = new[] { SKColor.Parse("#262630"), SKColor.Parse("#1f1f26") };
        Border = new[] { SKColor.Parse("#262630"), SKColor.Parse("#1f1f26") };

        Width = 512;
        Height = 512;
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
    }

    protected override void DrawPreview(SKCanvas c)
        => c.DrawBitmap(Preview ?? DefaultPreview, new SKRect(ImageMargin, ImageMargin, Width - ImageMargin, Height - ImageMargin), ImagePaint);

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawPreview(c);
        DrawTextBackground(c);
        DrawDisplayName(c);

        return new[] { ret };
    }
}
