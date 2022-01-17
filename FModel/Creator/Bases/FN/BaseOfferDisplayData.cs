using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using SkiaSharp;

namespace FModel.Creator.Bases.FN
{
    public class BaseOfferDisplayData : UCreator
    {
        private BaseMaterialInstance[] _offerImages;

        public BaseOfferDisplayData(UObject uObject, EIconStyle style) : base(uObject, style)
        {
        }

        public override void ParseForInfo()
        {
            if (!Object.TryGetValue(out UMaterialInterface[] presentations, "Presentations"))
                return;

            _offerImages = new BaseMaterialInstance[presentations.Length];
            for (int i = 0; i < _offerImages.Length; i++)
            {
                var offerImage = new BaseMaterialInstance(presentations[i], Style);
                offerImage.ParseForInfo();
                _offerImages[i] = offerImage;
            }
        }

        public override SKBitmap[] Draw()
        {
            var ret = new SKBitmap[_offerImages.Length];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = _offerImages[i].Draw()[0];
            }

            return ret;
        }
    }
}
