using System;
using System.Collections.Generic;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Extensions;
using FModel.Framework;
using FModel.Views.Resources.Controls;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.SG;

public class BaseBadgeStat : UStatCreator
{
    public BaseBadgeStat(UObject uObject, EIconStyle style) : base(uObject, style)
    {
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FText displayName, "DisplayName"))
            DisplayName = displayName.Text;

        if (Object.TryGetValue(out FStructFallback[] Tiers, "Tiers"))
        {
            foreach (var TiersEntry in Tiers)
            {
                TiersEntry.TryGetValue(out FText DisplayName, "DisplayName");

                if (Preview == null)
                {
                    if (TiersEntry.TryGetValue(out UMaterialInstanceConstant BadgeMaterial, "BadgeMaterial"))
                    {
                        foreach (var TexParam in BadgeMaterial.TextureParameterValues)
                        {
                            Preview = Utils.GetBitmap(TexParam.ParameterValue);
                            break;
                        }
                    }
                }

                var Stat = new IconStat(DisplayName.Text, null, 0, false);

                _statistics.Add(Stat);
            }
        }

        // Make sure this is always after the base call to override the fortnite rarity colour, sorry for this
        base.ParseForInfo();
        Background = new[] { SKColor.Parse("090d28"), SKColor.Parse("090d28") };
        Border = new[] { SKColor.Parse("d9cb75"), SKColor.Parse("46433c") };
    }
}
