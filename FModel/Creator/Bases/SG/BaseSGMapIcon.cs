using System;
using System.Linq;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.Views.Resources.Controls;
using SkiaSharp;

namespace FModel.Creator.Bases.SG;

public class BaseSGMapIcon : UCreator
{
    public BaseSGMapIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Width = 960;
        Height = 540;

        Background = new[] { SKColor.Parse("1eb3d5"), SKColor.Parse("1eb3d5") };
        Border = new[] { SKColor.Parse("1eb3d5"), SKColor.Parse("1eb3d5") };
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FSoftObjectPath iconTextureAssetData, "DisplayImage"))
            Preview = Utils.GetBitmap(iconTextureAssetData);

        if (Object.TryGetValue(out FText displayName, "DisplayName"))
            DisplayName = displayName.Text;

        if (Object.TryGetValue(out FText description, "Description"))
            Description = description.Text;
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
