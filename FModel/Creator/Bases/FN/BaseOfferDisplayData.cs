using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.FN;

public class BaseOfferDisplayData : UCreator
{
    private BaseMaterialInstance[] _offerImages;

    public BaseOfferDisplayData(UObject uObject, EIconStyle style) : base(uObject, style)
    {
    }

    public override void ParseForInfo()
    {
        if (!Object.TryGetValue(out FStructFallback[] contextualPresentations, "ContextualPresentations"))
            return;

        _offerImages = new BaseMaterialInstance[contextualPresentations.Length];
        for (var i = 0; i < _offerImages.Length; i++)
        {
            if (!contextualPresentations[i].TryGetValue(out FSoftObjectPath material, "Material") ||
                !material.TryLoad(out UMaterialInterface presentation)) continue;

            var offerImage = new BaseMaterialInstance(presentation, Style);
            offerImage.ParseForInfo();
            _offerImages[i] = offerImage;
        }
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap[_offerImages.Length];
        for (var i = 0; i < ret.Length; i++)
        {
            ret[i] = _offerImages[i].Draw()[0];
        }

        return ret;
    }
}
