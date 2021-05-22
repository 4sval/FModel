using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN
{
    public class BaseSeason : UCreator
    {
        private Reward _firstWinReward;
        private Dictionary<int, List<Reward>> _bookXpSchedule;
        private const int _headerHeight = 150;
        // keep the list because rewards are ordered by least to most important
        // we only care about the most but we also have filters so we can't just take the last reward

        public BaseSeason(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            Width = 1024;
            Height = _headerHeight + 50;
            Margin = 0;
        }

        public override void ParseForInfo()
        {
            _bookXpSchedule = new Dictionary<int, List<Reward>>();

            if (Object.TryGetValue(out FText displayName, "DisplayName"))
                DisplayName = displayName.Text.ToUpperInvariant();

            if (Object.TryGetValue(out FStructFallback seasonFirstWinRewards, "SeasonFirstWinRewards") &&
                seasonFirstWinRewards.TryGetValue(out FStructFallback[] rewards, "Rewards"))
            {
                foreach (var reward in rewards)
                {
                    if (!reward.TryGetValue(out FSoftObjectPath itemDefinition, "ItemDefinition") ||
                        !Utils.TryLoadObject(itemDefinition.AssetPathName.Text, out UObject uObject)) continue;

                    _firstWinReward = new Reward(uObject);
                    break;
                }
            }

            var freeLevels = Array.Empty<FStructFallback>();
            var paidLevels = Array.Empty<FStructFallback>();
            if (Object.TryGetValue(out FPackageIndex[] additionalSeasonData, "AdditionalSeasonData") &&
                additionalSeasonData.Length > 0 && Utils.TryGetPackageIndexExport(additionalSeasonData[0], out UObject data) &&
                data.TryGetValue(out FStructFallback battlePassXpScheduleFree, "BattlePassXpScheduleFree") &&
                battlePassXpScheduleFree.TryGetValue(out freeLevels, "Levels") &&
                data.TryGetValue(out FStructFallback battlePassXpSchedulePaid, "BattlePassXpSchedulePaid") &&
                battlePassXpSchedulePaid.TryGetValue(out paidLevels, "Levels"))
            {
                // we got them boys
            }
            else if (Object.TryGetValue(out FStructFallback bookXpScheduleFree, "BookXpScheduleFree") &&
                     bookXpScheduleFree.TryGetValue(out freeLevels, "Levels") &&
                     Object.TryGetValue(out FStructFallback bookXpSchedulePaid, "BookXpSchedulePaid") &&
                     bookXpSchedulePaid.TryGetValue(out paidLevels, "Levels"))
            {
                // we got them boys
            }

            for (var i = 0; i < freeLevels.Length; i++)
            {
                _bookXpSchedule[i] = new List<Reward>();
                if (!freeLevels[i].TryGetValue(out rewards, "Rewards")) continue;

                foreach (var reward in rewards)
                {
                    if (!reward.TryGetValue(out FSoftObjectPath itemDefinition, "ItemDefinition") ||
                        itemDefinition.AssetPathName.Text.Contains("/Items/Tokens/") ||
                        !Utils.TryLoadObject(itemDefinition.AssetPathName.Text, out UObject uObject)) continue;
                    
                    _bookXpSchedule[i].Add(new Reward(uObject));
                    break;
                }
            }

            for (var i = 0; i < paidLevels.Length; i++)
            {
                if (!paidLevels[i].TryGetValue(out rewards, "Rewards")) continue;

                foreach (var reward in rewards)
                {
                    if (!reward.TryGetValue(out FSoftObjectPath itemDefinition, "ItemDefinition") ||
                        !Utils.TryLoadObject(itemDefinition.AssetPathName.Text, out UObject uObject)) continue;
                    
                    _bookXpSchedule[i].Add(new Reward(uObject));
                    break;
                }
            }

            Height += 100 * _bookXpSchedule.Count / 10;
        }

        public override SKImage Draw()
        {
            using var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var c = new SKCanvas(ret);

            DrawHeader(c);
            _firstWinReward?.DrawSeasonWin(c, _headerHeight);
            DrawBookSchedule(c);

            return SKImage.FromBitmap(ret);
        }

        private const int _DEFAULT_AREA_SIZE = 80;
        private readonly SKPaint _headerPaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High,
            Typeface = Utils.Typefaces.Bundle, TextSize = 50,
            TextAlign = SKTextAlign.Center, Color = SKColor.Parse("#262630")
        };
        private readonly SKPaint _bookPaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High,
            Typeface = Utils.Typefaces.Bottom ?? Utils.Typefaces.BundleNumber,
            Color = SKColors.White, TextAlign = SKTextAlign.Center, TextSize = 15
        };

        public void DrawHeader(SKCanvas c)
        {
            c.DrawRect(new SKRect(0, 0, Width, Height), _headerPaint);

            _headerPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, _headerHeight / 2), Width / 5 * 4,
                new[] {SKColors.SkyBlue.WithAlpha(50), SKColors.Blue.WithAlpha(50)}, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(0, 0, Width, Height), _headerPaint);

            _headerPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(Width / 2, _headerHeight), new SKPoint(Width / 2, 75),
                new[] {SKColors.Black.WithAlpha(25), SKColors.Blue.WithAlpha(0)}, SKShaderTileMode.Clamp);
            c.DrawRect(new SKRect(0, 75, Width, _headerHeight), _headerPaint);

            _headerPaint.Shader = null;
            _headerPaint.Color = SKColors.White;
            while (_headerPaint.MeasureText(DisplayName) > Width)
            {
                _headerPaint.TextSize -= 1;
            }

            var shaper = new CustomSKShaper(_headerPaint.Typeface);
            var shapedText = shaper.Shape(DisplayName, _headerPaint);
            c.DrawShapedText(shaper, DisplayName, (Width - shapedText.Points[^1].X) / 2, _headerHeight / 2 + _headerPaint.TextSize / 2 - 10, _headerPaint);
        }

        private void DrawBookSchedule(SKCanvas c)
        {
            var x = 20;
            var y = _headerHeight + 50;
            foreach (var (index, reward) in _bookXpSchedule)
            {
                if (index == 0 || reward.Count == 0 || !reward[0].HasReward())
                    continue;

                c.DrawText(index.ToString(), new SKPoint(x + _DEFAULT_AREA_SIZE / 2, y - 5), _bookPaint);
                reward[0].DrawSeason(c, x, y, _DEFAULT_AREA_SIZE);

                if (index != 1 && index % 10 == 0)
                {
                    y += _DEFAULT_AREA_SIZE + 20;
                    x = 20;
                }
                else
                {
                    x += _DEFAULT_AREA_SIZE + 20;
                }
            }
        }
    }
}