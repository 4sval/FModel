using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN;

public class Page
{
    public int LevelsNeededForUnlock;
    public int RewardsNeededForUnlock;
    public Reward[] RewardEntryList;
}

public class BaseSeason : UCreator
{
    private Reward _firstWinReward;
    private Page[] _bookXpSchedule;
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
        _bookXpSchedule = Array.Empty<Page>();

        if (Object.TryGetValue(out FText displayName, "DisplayName", "ItemName"))
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

        if (Object.TryGetValue(out FPackageIndex[] additionalSeasonData, "AdditionalSeasonData"))
        {
            foreach (var data in additionalSeasonData)
            {
                if (!Utils.TryGetPackageIndexExport(data, out UObject packageIndex) ||
                    !packageIndex.TryGetValue(out FStructFallback[] pageList, "PageList")) continue;

                var i = 0;
                _bookXpSchedule = new Page[pageList.Length];
                foreach (var page in pageList)
                {
                    if (!page.TryGetValue(out int levelsNeededForUnlock, "LevelsNeededForUnlock") ||
                        !page.TryGetValue(out int rewardsNeededForUnlock, "RewardsNeededForUnlock") ||
                        !page.TryGetValue(out FPackageIndex[] rewardEntryList, "RewardEntryList"))
                        continue;

                    var p = new Page
                    {
                        LevelsNeededForUnlock = levelsNeededForUnlock,
                        RewardsNeededForUnlock = rewardsNeededForUnlock,
                        RewardEntryList = new Reward[rewardEntryList.Length]
                    };

                    for (var j = 0; j < p.RewardEntryList.Length; j++)
                    {
                        if (!Utils.TryGetPackageIndexExport(rewardEntryList[j], out packageIndex) ||
                            !packageIndex.TryGetValue(out FStructFallback battlePassOffer, "BattlePassOffer") ||
                            !battlePassOffer.TryGetValue(out FStructFallback rewardItem, "RewardItem") ||
                            !rewardItem.TryGetValue(out FSoftObjectPath itemDefinition, "ItemDefinition") ||
                            !Utils.TryLoadObject(itemDefinition.AssetPathName.Text, out UObject uObject)) continue;

                        p.RewardEntryList[j] = new Reward(uObject);
                    }

                    _bookXpSchedule[i++] = p;
                }

                break;
            }
        }

        Height += 100 * _bookXpSchedule.Sum(x => x.RewardEntryList.Length) / _bookXpSchedule.Length;
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var c = new SKCanvas(ret);

        DrawHeader(c);
        _firstWinReward?.DrawSeasonWin(c, _headerHeight);
        DrawBookSchedule(c);

        return new[] { ret };
    }

    private const int _DEFAULT_AREA_SIZE = 80;
    private readonly SKPaint _headerPaint = new()
    {
        IsAntialias = true, FilterQuality = SKFilterQuality.High,
        Typeface = Utils.Typefaces.Bundle, TextSize = 50,
        TextAlign = SKTextAlign.Center, Color = SKColor.Parse("#262630")
    };

    public void DrawHeader(SKCanvas c)
    {
        c.DrawRect(new SKRect(0, 0, Width, Height), _headerPaint);

        _headerPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, _headerHeight / 2), Width / 5 * 4,
            new[] { SKColors.SkyBlue.WithAlpha(50), SKColors.Blue.WithAlpha(50) }, SKShaderTileMode.Clamp);
        c.DrawRect(new SKRect(0, 0, Width, Height), _headerPaint);

        _headerPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(Width / 2, _headerHeight), new SKPoint(Width / 2, 75),
            new[] { SKColors.Black.WithAlpha(25), SKColors.Blue.WithAlpha(0) }, SKShaderTileMode.Clamp);
        c.DrawRect(new SKRect(0, 75, Width, _headerHeight), _headerPaint);

        _headerPaint.Shader = null;
        _headerPaint.Color = SKColors.White;
        while (_headerPaint.MeasureText(DisplayName) > Width)
        {
            _headerPaint.TextSize -= 1;
        }

        var shaper = new CustomSKShaper(_headerPaint.Typeface);
        c.DrawShapedText(shaper, DisplayName, Width / 2f, _headerHeight / 2f + _headerPaint.TextSize / 2 - 10, _headerPaint);
    }

    private void DrawBookSchedule(SKCanvas c)
    {
        var x = 20;
        var y = _headerHeight + 50;
        foreach (var page in _bookXpSchedule)
        {
            foreach (var reward in page.RewardEntryList)
            {
                reward.DrawSeason(c, x, y, _DEFAULT_AREA_SIZE);
                x += _DEFAULT_AREA_SIZE + 20;
            }

            y += _DEFAULT_AREA_SIZE + 20;
            x = 20;
        }
    }
}
