using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Fortnite.Enums;
using FModel.Extensions;
using FModel.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN
{
    public class BaseIconStats : BaseIcon
    {
        private readonly IList<IconStat> _statistics;
        private const int _headerHeight = 128;
        private bool _screenLayer;

        public BaseIconStats(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            Width = 1024;
            Height = _headerHeight;
            Margin = 0;
            _statistics = new List<IconStat>();
            _screenLayer = uObject.ExportType.Equals("FortAccoladeItemDefinition", StringComparison.OrdinalIgnoreCase);
            DefaultPreview = Utils.GetBitmap("FortniteGame/Content/Athena/HUD/Quests/Art/T_NPC_Default.T_NPC_Default");
        }

        public override void ParseForInfo()
        {
            base.ParseForInfo();
            DisplayName = DisplayName.ToUpperInvariant();

            if (Object.TryGetValue(out FName accoladeType, "AccoladeType") &&
                accoladeType.Text.Equals("EFortAccoladeType::Medal", StringComparison.OrdinalIgnoreCase))
            {
                _screenLayer = false;
            }

            if (Object.TryGetValue(out FGameplayTagContainer poiLocations, "POILocations") &&
                Utils.TryLoadObject("FortniteGame/Content/Quests/QuestIndicatorData.QuestIndicatorData", out UObject uObject) &&
                uObject.TryGetValue(out FStructFallback[] challengeMapPoiData, "ChallengeMapPoiData"))
            {
                foreach (var location in poiLocations)
                {
                    var locationName = "Unknown";
                    foreach (var poi in challengeMapPoiData)
                    {
                        if (!poi.TryGetValue(out FStructFallback locationTag, "LocationTag") || !locationTag.TryGetValue(out FName tagName, "TagName") ||
                            !tagName.Text.Equals(location.Text, StringComparison.OrdinalIgnoreCase) || !poi.TryGetValue(out FText text, "Text")) continue;

                        locationName = text.Text;
                        break;
                    }

                    _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "B0C091D7409B1657423C5F97E9CF4C77", "LOCATION NAME"), locationName.ToUpper()));
                }
            }

            if (Object.TryGetValue(out FStructFallback maxStackSize, "MaxStackSize"))
            {
                if (maxStackSize.TryGetValue(out float v, "Value") && v > -1)
                {
                    _statistics.Add(new IconStat("Max Stack", v));
                }
                else if (TryGetCurveTableStat(maxStackSize, out var s))
                {
                    _statistics.Add(new IconStat("Max Stack", s));
                }
            }

            if (Object.TryGetValue(out FStructFallback xpRewardAmount, "XpRewardAmount") && TryGetCurveTableStat(xpRewardAmount, out var x))
            {
                _statistics.Add(new IconStat("XP Amount", x));
            }

            if (Object.TryGetValue(out FStructFallback weaponStatHandle, "WeaponStatHandle") &&
                weaponStatHandle.TryGetValue(out FName weaponRowName, "RowName") &&
                weaponStatHandle.TryGetValue(out UDataTable dataTable, "DataTable") &&
                dataTable.TryGetDataTableRow(weaponRowName.Text, StringComparison.OrdinalIgnoreCase, out var weaponRowValue))
            {
                if (weaponRowValue.TryGetValue(out float dmgPb, "DmgPB") && dmgPb != 0f && weaponRowValue.TryGetValue(out int bpc , "BulletsPerCartridge")&& bpc != 0f)
                {
                    _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "BF7E3CF34A9ACFF52E95EAAD4F09F133", "Damage to Player"), dmgPb * bpc, 200));
                }

                if (weaponRowValue.TryGetValue(out int clipSize, "ClipSize") && clipSize != 0)
                {
                    _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "068239DD4327B36124498C9C5F61C038", "Magazine Size"), clipSize, 50));
                }

                if (weaponRowValue.TryGetValue(out float firingRate, "FiringRate") && firingRate != 0f)
                {
                    _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "27B80BA44805ABD5A2D2BAB2902B250C", "Fire Rate"), firingRate, 15));
                }

                if (weaponRowValue.TryGetValue(out float armTime, "ArmTime") && armTime != 0f)
                {
                    _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "3BFEB8BD41A677CC5F45B9A90D6EAD6F", "Arming Delay"), armTime, 125));
                }

                if (weaponRowValue.TryGetValue(out float reloadTime, "ReloadTime") && reloadTime != 0f)
                {
                    _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "6EA26D1A4252034FBD869A90F9A6E49A", "Reload Time"), reloadTime, 15));
                }

                if ((Object.ExportType.Equals("FortContextTrapItemDefinition", StringComparison.OrdinalIgnoreCase) ||
                     Object.ExportType.Equals("FortTrapItemDefinition", StringComparison.OrdinalIgnoreCase)) &&
                    weaponRowValue.TryGetValue(out UDataTable durabilityTable, "Durability") &&
                    weaponRowValue.TryGetValue(out FName durabilityRowName, "DurabilityRowName") &&
                    durabilityTable.TryGetDataTableRow(durabilityRowName.Text, StringComparison.OrdinalIgnoreCase, out var durability) &&
                    durability.TryGetValue(out int duraByRarity, GetRarityName(Object.GetOrDefault<FName>("Rarity"))))
                {
                    _statistics.Add(new IconStat(Utils.GetLocalizedResource("", "6FA2882140CB69DE32FD73A392F0585B", "Durability"), duraByRarity, 20));
                }
            }

            if (!string.IsNullOrEmpty(Description))
                Height += 40 + (int) _informationPaint.TextSize * Utils.SplitLines(Description, _informationPaint, Width - 20).Count;
            Height += 50 * _statistics.Count;
        }

        public override SKImage Draw()
        {
            using var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var c = new SKCanvas(ret);

            DrawHeader(c);
            DrawDisplayName(c);
            DrawStatistics(c);

            return SKImage.FromBitmap(ret);
        }

        private bool TryGetCurveTableStat(FStructFallback property, out float statValue)
        {
            if (property.TryGetValue(out FStructFallback curve, "Curve") &&
                curve.TryGetValue(out FName rowName, "RowName") &&
                curve.TryGetValue(out UCurveTable curveTable, "CurveTable") &&
                curveTable.TryGetCurveTableRow(rowName.Text, StringComparison.OrdinalIgnoreCase, out var rowValue) &&
                rowValue.TryGetValue(out FSimpleCurveKey[] keys, "Keys") && keys.Length > 0)
            {
                statValue = keys[0].KeyValue;
                return true;
            }

            statValue = 0F;
            return false;
        }

        private string GetRarityName(FName r)
        {
            var rarity = EFortRarity.Uncommon;
            switch (r.Text)
            {
                case "EFortRarity::Common":
                case "EFortRarity::Handmade":
                    rarity = EFortRarity.Common;
                    break;
                case "EFortRarity::Rare":
                case "EFortRarity::Sturdy":
                    rarity = EFortRarity.Rare;
                    break;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    rarity = EFortRarity.Epic;
                    break;
                case "EFortRarity::Legendary":
                case "EFortRarity::Fine":
                    rarity = EFortRarity.Legendary;
                    break;
                case "EFortRarity::Mythic":
                case "EFortRarity::Elegant":
                    rarity = EFortRarity.Mythic;
                    break;
                case "EFortRarity::Transcendent":
                case "EFortRarity::Masterwork":
                    rarity = EFortRarity.Transcendent;
                    break;
                case "EFortRarity::Unattainable":
                case "EFortRarity::Badass":
                    rarity = EFortRarity.Unattainable;
                    break;
            }

            return rarity.GetDescription();
        }

        private readonly SKPaint _informationPaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High,
            Color = SKColor.Parse("#262630"), TextSize = 16,
            Typeface = Utils.Typefaces.Description
        };

        private void DrawHeader(SKCanvas c)
        {
            c.DrawRect(new SKRect(0, 0, Width, Height), _informationPaint);

            _informationPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, _headerHeight / 2), Width / 5 * 4,
                new[] {Background[0].WithAlpha(180), Background[1].WithAlpha(220)}, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(_headerHeight, 0, Width, _headerHeight), _informationPaint);

            _informationPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, _headerHeight / 2), Width / 5 * 4,
                new[] {SKColor.Parse("#262630"), SKColor.Parse("#1f1f26")}, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(0, _headerHeight, Width, Height), _informationPaint);

            _informationPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(Width / 2, _headerHeight), new SKPoint(Width / 2, 75),
                new[] {SKColors.Black.WithAlpha(25), Background[1].WithAlpha(0)}, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(0, 75, Width, _headerHeight), _informationPaint);

            _informationPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, _headerHeight / 2), Width / 5 * 4, Background, SKShaderTileMode.Clamp);
            using var rect = new SKPath {FillType = SKPathFillType.EvenOdd};
            rect.MoveTo(0, 0);
            rect.LineTo(_headerHeight + _headerHeight / 3, 0);
            rect.LineTo(_headerHeight, _headerHeight);
            rect.LineTo(0, _headerHeight);
            rect.Close();
            c.DrawPath(rect, _informationPaint);

            _informationPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(_headerHeight / 2, _headerHeight / 2), new SKPoint(_headerHeight / 2 + 100, _headerHeight / 2),
                new[] {SKColors.Black.WithAlpha(25), Background[1].WithAlpha(0)}, SKShaderTileMode.Clamp);
            c.DrawPath(rect, _informationPaint);

            _informationPaint.Shader = null;

            ImagePaint.BlendMode = _screenLayer ? SKBlendMode.Screen : Preview == null ? SKBlendMode.ColorBurn : SKBlendMode.SrcOver;
            c.DrawBitmap((Preview ?? DefaultPreview).Resize(_headerHeight), 0, 0, ImagePaint);
        }

        private new void DrawDisplayName(SKCanvas c)
        {
            if (string.IsNullOrEmpty(DisplayName)) return;

            _informationPaint.TextSize = 50;
            _informationPaint.Color = SKColors.White;
            _informationPaint.Typeface = Utils.Typefaces.Bundle;
            while (_informationPaint.MeasureText(DisplayName) > Width - _headerHeight * 2)
            {
                _informationPaint.TextSize -= 1;
            }

            var shaper = new CustomSKShaper(_informationPaint.Typeface);
            shaper.Shape(DisplayName, _informationPaint);
            c.DrawShapedText(shaper, DisplayName, _headerHeight + _headerHeight / 3 + 10, _headerHeight / 2 + _informationPaint.TextSize / 3, _informationPaint);
        }

        private void DrawStatistics(SKCanvas c)
        {
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
    }

    public class IconStat
    {
        private readonly string _statName;
        private readonly object _value;
        private readonly float _maxValue;

        public IconStat(string statName, object value, float maxValue = 0)
        {
            _statName = statName.ToUpperInvariant();
            _value = value;
            _maxValue = maxValue;
        }

        private readonly SKPaint _statPaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High,
            TextSize = 25, Typeface = Utils.Typefaces.DisplayName,
            Color = SKColors.White
        };

        public void Draw(SKCanvas c, SKColor sliderColor, int width, int height, ref float y)
        {
            while (_statPaint.MeasureText(_statName) > height * 2)
            {
                _statPaint.TextSize -= 1;
            }

            var shaper = new CustomSKShaper(_statPaint.Typeface);
            shaper.Shape(_statName, _statPaint);
            c.DrawShapedText(shaper, _statName, 50, y + 10, _statPaint);

            _statPaint.TextAlign = SKTextAlign.Right;
            _statPaint.Typeface = Utils.Typefaces.BundleNumber;
            _statPaint.Color = sliderColor;
            var sliderRight = width - 100 - _statPaint.MeasureText(_value.ToString());
            c.DrawRect(new SKRect(height * 2, y, Math.Min(width - height, sliderRight), y + 5), _statPaint);

            _statPaint.Color = SKColors.White;
            c.DrawText(_value.ToString(), new SKPoint(width - 50, y + 10), _statPaint);

            if (_maxValue < 1 || !float.TryParse(_value.ToString(), out var floatValue)) return;
            var sliderWidth = (sliderRight - height * 2) * (floatValue / _maxValue);
            c.DrawRect(new SKRect(height * 2, y, Math.Min(height * 2 + sliderWidth, sliderRight), y + 5), _statPaint);
        }
    }
}