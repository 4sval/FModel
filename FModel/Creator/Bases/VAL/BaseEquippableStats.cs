using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Extensions;
using FModel.Framework;
using FModel.Views.Resources.Controls;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.VAL;

public class BaseEquippableStats : UStatCreator
{
    private readonly float _health = 100.0f;
    private readonly float _genericrange = 10.0f;
    private readonly string[] _rangemap = new string[] { "Close", "Mid", "Far" };

    public BaseEquippableStats(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Width = 1024;
    }

    protected override void DrawDisplayName(SKCanvas c)
    {
        if (string.IsNullOrEmpty(DisplayName))
            return;

        _informationPaint.TextSize = 50;
        _informationPaint.Color = SKColors.White;
        _informationPaint.Typeface = Utils.Typefaces.DisplayName;
        while (_informationPaint.MeasureText(DisplayName) > Width - _headerHeight * 2)
        {
            _informationPaint.TextSize -= 1;
        }

        var shaper = new CustomSKShaper(_informationPaint.Typeface);
        c.DrawShapedText(shaper, DisplayName, _headerHeight + _headerHeight, _headerHeight / 2f + _informationPaint.TextSize / 3, _informationPaint);
    }

    protected override void DrawHeader(SKCanvas c)
    {
        var UsedBMP = (Preview ?? DefaultPreview);

        c.DrawRect(new SKRect(0, 0, Width, Height), _informationPaint);

        _informationPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, _headerHeight / 2), Width / 5 * 4,
            new[] { SKColor.Parse("#262630"), SKColor.Parse("#1f1f26") }, SKShaderTileMode.Clamp);
        c.DrawRect(new SKRect(0, _headerHeight, Width, Height), _informationPaint);

        _informationPaint.Shader = null;

        ImagePaint.BlendMode = _screenLayer ? SKBlendMode.Screen : Preview == null ? SKBlendMode.ColorBurn : SKBlendMode.SrcOver;

        c.DrawBitmap(UsedBMP.ResizeWithRatio((Preview.Height <= 220) ? 0.45f : 0.25f), ((Preview.Height <= 220) ? 10 : 35), _headerHeight * .25f, ImagePaint);
    }

    protected override void DrawStatistics(SKCanvas c)
    {
        var UsedBMP = (Preview ?? DefaultPreview);

        var outY = _headerHeight + 25f;
        if (!string.IsNullOrEmpty(Description))
        {
            _informationPaint.TextSize = 16;
            _informationPaint.Color = SKColors.White.WithAlpha(175);
            _informationPaint.Typeface = Utils.Typefaces.Description;
            Utils.DrawMultilineText(c, Description, Width - 40, 0, SKTextAlign.Center,
                new SKRect(20, outY, Width - 20, Height), _informationPaint, out outY);
            outY += 25;
        }

        foreach (var stat in _statistics)
        {
            stat.Draw(c, Border[0].WithAlpha(100), Width, _headerHeight, ref outY);
            outY += 50;
        }
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

            var Category = "";
            if (UIData.TryGetValue(out FName category, "Category"))
                Category = category.ToString().Split("::")[1];

            if (UIData.TryGetValue(out FStructFallback WeaponStats, "WeaponStats"))
            {
                if (WeaponStats.TryGetValue(out FStructFallback[] DamageRanges, "DamageRanges"))
                {

                    int Index = 0;
                    foreach (var DamageRange in DamageRanges)
                    {
                        DamageRange.TryGetValue(out float BodyDamage, "BodyDamage");
                        DamageRange.TryGetValue(out float HeadDamage, "HeadDamage");

                        DamageRange.TryGetValue(out float RangeStartMeters, "RangeStartMeters");
                        DamageRange.TryGetValue(out float RangeEndMeters, "RangeEndMeters");

                        _statistics.Add(new IconStat($"Damage {_rangemap[Index]}", BodyDamage, _health));
                        _statistics.Add(new IconStat($"HDamage {_rangemap[Index]}", HeadDamage, _health));
                        Index++;
                    }
                }

                if (WeaponStats.TryGetValue(out int MagazineSize, "MagazineSize"))
                    _statistics.Add(new IconStat("Magazine Size", MagazineSize, MagazineSize));

                if (WeaponStats.TryGetValue(out float FireRate, "FireRate"))
                    _statistics.Add(new IconStat("Fire Rate", FireRate, _genericrange));

                if (WeaponStats.TryGetValue(out float EquipTimeSeconds, "EquipTimeSeconds"))
                    _statistics.Add(new IconStat("Equip Time", EquipTimeSeconds, _genericrange));

                if (WeaponStats.TryGetValue(out float ReloadTimeSeconds, "ReloadTimeSeconds"))
                    _statistics.Add(new IconStat("Reload Time", ReloadTimeSeconds, _genericrange));

                if (WeaponStats.TryGetValue(out float RunSpeedMultiplier, "RunSpeedMultiplier"))
                    _statistics.Add(new IconStat("Speed Mult", RunSpeedMultiplier, 1));
            }
            else if ((_statistics.Count == 0) && DisplayName.ToUpper() == "MELEE")
            {
                _statistics.Add(new IconStat("Damage", 75, _health));
                _statistics.Add(new IconStat("Damage Behind", 100, _health));
            }
            else if ((_statistics.Count == 0) && Category == "Hidden")
            {
                _statistics.Add(new IconStat("Hidden", 1, 1));
            }
        }

        // Make sure this is always after the base call to override the fortnite rarity colour, sorry for this
        base.ParseForInfo();
        Background = new[] { SKColor.Parse("090d28"), SKColor.Parse("090d28") };
        Border = new[] { SKColor.Parse("d9cb75"), SKColor.Parse("46433c") };
    }
}
