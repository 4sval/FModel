using System;
using System.Collections.Generic;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
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

namespace FModel.Creator.Bases.BB;

public class BaseHammerIcon : UStatCreator
{
    public BaseHammerIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Width = 1024;
        Margin = 0;
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FSoftObjectPath iconTextureAssetData, "IconTextureAssetData", "UnlockPortraitGuideImage"))
            Preview = Utils.GetBitmap(iconTextureAssetData);

        if (Object.TryGetValue(out FText displayName, "DisplayName", "RegionDisplayName", "ZoneName"))
            DisplayName = displayName.Text;

        if (Object.TryGetValue(out FText description, "Description", "RegionShortName", "ZoneDescription"))
            Description = description.Text;

        if (Object.TryGetValue(out int TapsToBreak, "TapsToBreak"))
            _statistics.Add(new IconStat("Taps To Break", TapsToBreak, 99));

        // Make sure this is always after the base call to override the fortnite rarity colour, sorry for this
        base.ParseForInfo();
        Background = new[] { SKColor.Parse("86886f"), SKColor.Parse("86886f") };
        Border = new[] { SKColor.Parse("abae8e"), SKColor.Parse("abae8e") };
    }
}
