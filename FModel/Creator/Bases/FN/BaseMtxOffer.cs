using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.FN;

public class BaseMtxOffer : UCreator
{
    public BaseMtxOffer(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Background = new[] { SKColor.Parse("4F4F69"), SKColor.Parse("4F4F69") };
        Border = new[] { SKColor.Parse("9092AB") };
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FSoftObjectPath image, "SoftDetailsImage", "SoftTileImage"))
        {
            Preview = Utils.GetBitmap(image);
        }

        if (Object.TryGetValue(out FStructFallback gradient, "Gradient") &&
            gradient.TryGetValue(out FLinearColor start, "Start") &&
            gradient.TryGetValue(out FLinearColor stop, "Stop"))
        {
            Background = new[] { SKColor.Parse(start.Hex), SKColor.Parse(stop.Hex) };
        }

        if (Object.TryGetValue(out FLinearColor background, "Background"))
            Border = new[] { SKColor.Parse(background.Hex) };
        if (Object.TryGetValue(out FText displayName, "DisplayName"))
            DisplayName = displayName.Text;
        if (Object.TryGetValue(out FText shortDescription, "ShortDescription"))
            Description = shortDescription.Text;

        if (Object.TryGetValue(out FStructFallback[] details, "DetailsAttributes"))
        {
            foreach (var detail in details)
            {
                if (detail.TryGetValue(out FText detailName, "Name"))
                {
                    Description += $"\n- {detailName.Text.TrimEnd()}";
                }

                if (detail.TryGetValue(out FText detailValue, "Value") && !string.IsNullOrEmpty(detailValue.Text))
                {
                    Description += $" ({detailValue.Text})";
                }
            }
        }

        Description = Utils.RemoveHtmlTags(Description);
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
            case EIconStyle.NoText:
                DrawBackground(c);
                DrawPreview(c);
                break;
            default:
                DrawBackground(c);
                DrawPreview(c);
                DrawTextBackground(c);
                DrawDisplayName(c);
                DrawDescription(c);
                break;
        }

        return new[] { ret };
    }
}
