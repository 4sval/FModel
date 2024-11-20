using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.FN;

public class BaseAssembledMesh : UCreator
{
    public BaseAssembledMesh(UObject uObject, EIconStyle style) : base(uObject, style)
    {

    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FInstancedStruct[] additionalData, "AdditionalData"))
        {
            foreach (var data in additionalData)
            {
                if (data.NonConstStruct?.TryGetValue(out FSoftObjectPath largePreview, "LargePreviewImage", "SmallPreviewImage") == true)
                {
                    Preview = Utils.GetBitmap(largePreview);
                }
            }
        }
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        switch (Style)
        {
            case EIconStyle.NoBackground:
                DrawPreview(c);
                break;
            default:
                DrawBackground(c);
                DrawPreview(c);
                break;
        }

        return new[] { ret };
    }
}
