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

public class BaseBannerIcon : BaseSplitgateIcon
{
    public BaseBannerIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Width = 1152;
        Height = 232;

        StarterTextPos = 0;
    }

    public override void ParseForInfo()
    {
        base.ParseForInfo();
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawPreview(c);
        DrawDisplayName(c);

        return new[] { ret };
    }
}
