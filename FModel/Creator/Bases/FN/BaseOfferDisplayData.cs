using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.FN;

public class BaseOfferDisplayData : UCreator
{
    private readonly List<BaseMaterialInstance> _offerImages;
    private readonly List<SKBitmap> _renderImages;

    public BaseOfferDisplayData(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        _offerImages = new List<BaseMaterialInstance>();
        _renderImages = new List<SKBitmap>();
    }

    public override void ParseForInfo()
    {
        if (!Object.TryGetValue(out FStructFallback[] contextualPresentations, "ContextualPresentations"))
            return;

        for (var i = 0; i < contextualPresentations.Length; i++)
        {
            if (contextualPresentations[i].TryGetValue(out FSoftObjectPath material, "Material") &&
                material.TryLoad(out UMaterialInterface presentation))
            {
                var offerImage = new BaseMaterialInstance(presentation, Style);
                offerImage.ParseForInfo();
                _offerImages.Add(offerImage);
            }
            if (contextualPresentations[i].TryGetValue(out FSoftObjectPath renderImage, "RenderImage") &&
                renderImage.TryLoad(out UTexture2D texture))
            {
                    var skBitmap = Utils.GetBitmap(texture);
                    _renderImages.Add(skBitmap);
            }
        }
    }

    public override SKBitmap[] Draw()
    {
        var ret = new List<SKBitmap>();

        foreach (var offerImage in _offerImages)
        {
            var images = offerImage?.Draw();
            if (images != null && images.Length > 0)
            {
                ret.AddRange(images);
            }
        }
        ret.AddRange(_renderImages);

        return ret.ToArray();
    }
}