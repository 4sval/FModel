using System;
using System.Linq;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using FModel.Framework;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.Views.Resources.Controls;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.SG;

public class BaseNameTag : UCreator
{
    private SKColor NameTagColour { get; set; }

    public BaseNameTag(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Width = 960;
        Height = 256;
        StarterTextPos = 100;
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FText displayName, "DisplayName"))
            DisplayName = displayName.Text;

        if (Object.TryGetValue(out string TextColorString, "TextColorString"))
        {
            TextColorString = TextColorString.Remove(TextColorString.Length - 1);
            TextColorString =TextColorString.Substring(1);

            var SplitColours = TextColorString.Split(",");

            float Red = float.Parse(SplitColours[0].Split("=")[1]);
            float Green = float.Parse(SplitColours[1].Split("=")[1]);
            float Blue = float.Parse(SplitColours[2].Split("=")[1]);
            float Alpha = float.Parse(SplitColours[3].Split("=")[1]);

            DisplayNamePaint.ColorF = new SKColorF(Red, Green, Blue, Alpha);
        }
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawDisplayName(c);

        return new[] { ret };
    }
}
