using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.SB
{
    public class BaseLeague : UCreator
    {
        private int _promotionXp, _xpLostPerMatch;

        public BaseLeague(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            _promotionXp = 0;
            _xpLostPerMatch = 0;
        }

        public override void ParseForInfo()
        {
            if (Object.TryGetValue(out int promotionXp, "PromotionXP"))
                _promotionXp = promotionXp;
            if (Object.TryGetValue(out int xpLostPerMatch, "XPLostPerMatch"))
                _xpLostPerMatch = xpLostPerMatch;

            if (Object.TryGetValue(out FPackageIndex division, "Division") &&
                Utils.TryGetPackageIndexExport(division, out UObject div))
            {
                var d = new BaseDivision(div, Style);
                d.ParseForInfo();
                Preview = d.Preview;
                Background = d.Background;
                Border = d.Border;
            }

            if (Object.TryGetValue(out FText displayName, "DisplayName"))
                DisplayName = displayName.Text;
        }

        public override SKBitmap Draw()
        {
            var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var c = new SKCanvas(ret);

            DrawBackground(c);
            DrawPreview(c);
            DrawTextBackground(c);
            DrawDisplayName(c);

            DrawToBottom(c, SKTextAlign.Left, $"PromotionXP: {_promotionXp}");
            DrawToBottom(c, SKTextAlign.Right, $"XPLostPerMatch: {_xpLostPerMatch}");

            return ret;
        }
    }
}
