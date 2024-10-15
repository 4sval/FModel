using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.FN;

public class BasePaperSprite : BaseIcon
{
    public BasePaperSprite(UObject uObject, EIconStyle style) : base(uObject, style) {}

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out UTexture2D texture, "BakedSourceTexture")) Preview = Utils.GetBitmap(texture);
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawPreview(c);

        return new[] { ret };
    }
}