using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN;

public class Reward
{
    private string _rewardQuantity;
    private BaseIcon _theReward;

    public bool HasReward() => _theReward != null;

    public Reward()
    {
        _rewardQuantity = "x0";
    }

    public Reward(int quantity, FSoftObjectPath softObjectPath) : this()
    {
        _rewardQuantity = $"{quantity / 1000f}k";

        if (softObjectPath.TryLoad(out UObject d))
        {
            _theReward = new BaseIcon(d, EIconStyle.Default);
            _theReward.ParseForReward(false);
            _theReward.Border[0] = SKColors.White;
        }
    }

    public Reward(int quantity, FName primaryAssetName) : this(quantity, primaryAssetName.Text)
    {
    }

    public Reward(int quantity, string assetName) : this()
    {
        _rewardQuantity = $"x{quantity:###,###,###}".Trim();

        if (assetName.Contains(':'))
        {
            var parts = assetName.Split(':');

            if (parts[0].Equals("HomebaseBannerIcon", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!Utils.TryLoadObject($"FortniteGame/Plugins/GameFeatures/BRCosmetics/Content/Items/BannerIcons/{parts[1]}.{parts[1]}", out UObject p))
                    return;

                _theReward = new BaseIcon(p, EIconStyle.Default);
                _theReward.ParseForReward(false);
                _theReward.Border[0] = SKColors.White;
                _rewardQuantity = _theReward.DisplayName;
            }
            else GetReward(parts[1]);
        }
        else GetReward(assetName);
    }

    public Reward(UObject uObject)
    {
        _theReward = new BaseIcon(uObject, EIconStyle.Default);
        _theReward.ParseForReward(false);
        _theReward.Border[0] = SKColors.White;
    }

    private readonly SKPaint _rewardPaint = new()
    {
        IsAntialias = true, FilterQuality = SKFilterQuality.High
    };

    public void DrawQuest(SKCanvas c, SKRect rect)
    {
        _rewardPaint.TextSize = 25;
        if (HasReward())
        {
            var origin = new SKPoint(rect.Left, rect.Top);
            if (!string.IsNullOrEmpty(_rewardQuantity))
            {
                origin.Y -= _rewardPaint.TextSize / 2;
            }

            c.DrawBitmap((_theReward.Preview ?? _theReward.DefaultPreview).Resize((int) rect.Height), origin, _rewardPaint);
            if (string.IsNullOrEmpty(_rewardQuantity)) return;

            _rewardPaint.TextAlign = SKTextAlign.Center;
            _rewardPaint.FakeBoldText = true;
            _rewardPaint.Color = SKColors.White;
            var shaper = new CustomSKShaper(Utils.Typefaces.BundleNumber);
            c.DrawShapedText(shaper, _rewardQuantity, rect.Left + rect.Width * 0.5f, rect.Bottom, _rewardPaint);
        }
        // else
        // {
        //     _rewardPaint.Color = SKColors.White;
        //     _rewardPaint.Typeface = Utils.Typefaces.BundleNumber;
        //     c.DrawText("No Reward", new SKPoint(rect.Left, rect.MidY + 20), _rewardPaint);
        // }
    }

    public void DrawSeasonWin(SKCanvas c, int size)
    {
        if (!HasReward()) return;
        c.DrawBitmap((_theReward.Preview ?? _theReward.DefaultPreview).Resize(size), new SKPoint(0, 0), _rewardPaint);
    }

    public void DrawSeason(SKCanvas c, int x, int y, int areaSize)
    {
        if (!HasReward()) return;

        // area + icon
        _rewardPaint.Color = SKColor.Parse("#0F5CAF");
        c.DrawRect(new SKRect(x, y, x + areaSize, y + areaSize), _rewardPaint);
        c.DrawBitmap(_theReward.Preview.Resize(areaSize), new SKPoint(x, y), _rewardPaint);

        // rarity color
        _rewardPaint.Color = _theReward.Background[0];
        var pathBottom = new SKPath {FillType = SKPathFillType.EvenOdd};
        pathBottom.MoveTo(x, y + areaSize);
        pathBottom.LineTo(x, y + areaSize - areaSize / 25 * 2.5f);
        pathBottom.LineTo(x + areaSize, y + areaSize - areaSize / 25 * 4.5f);
        pathBottom.LineTo(x + areaSize, y + areaSize);
        pathBottom.Close();
        c.DrawPath(pathBottom, _rewardPaint);
    }

    private void GetReward(string trigger)
    {
        switch (trigger.ToLower())
        {
            // case "athenabattlestar":
            //     _theReward = new BaseIcon(null, EIconStyle.Default);
            //     _theReward.Border[0] = SKColor.Parse("FFDB67");
            //     _theReward.Background[0] = SKColor.Parse("8F4A20");
            //     _theReward.Preview = Utils.GetBitmap("FortniteGame/Content/Athena/UI/Frontend/Art/T_UI_BP_BattleStar_L.T_UI_BP_BattleStar_L");
            //     break;
            // case "athenaseasonalxp":
            //     _theReward = new BaseIcon(null, EIconStyle.Default);
            //     _theReward.Border[0] = SKColor.Parse("E6FDB1");
            //     _theReward.Background[0] = SKColor.Parse("51830F");
            //     _theReward.Preview = Utils.GetBitmap("FortniteGame/Content/UI/Foundation/Textures/Icons/Items/T-FNBR-XPUncommon-L.T-FNBR-XPUncommon-L");
            //     break;
            // case "mtxgiveaway":
            //     _theReward = new BaseIcon(null, EIconStyle.Default);
            //     _theReward.Border[0] = SKColor.Parse("DCE6FF");
            //     _theReward.Background[0] = SKColor.Parse("64A0AF");
            //     _theReward.Preview = Utils.GetBitmap("FortniteGame/Content/UI/Foundation/Textures/Icons/Items/T-Items-MTX.T-Items-MTX");
            //     break;
            default:
            {
                var path = Utils.GetFullPath($"FortniteGame/(?:Content/Athena|Content/Items|Plugins/GameFeatures)/.*?/{trigger}.uasset"); // path has no objectname and its needed so we push the trigger again as the objectname
                if (!string.IsNullOrWhiteSpace(path) && Utils.TryLoadObject(path.Replace("uasset", trigger), out UObject d))
                {
                    _theReward = new BaseIcon(d, EIconStyle.Default);
                    _theReward.ParseForReward(false);
                    _theReward.Border[0] = SKColors.White;
                    _rewardQuantity = $"{_theReward.DisplayName} ({_rewardQuantity})";
                }

                break;
            }
        }
    }
}
