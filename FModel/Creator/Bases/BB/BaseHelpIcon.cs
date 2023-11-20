using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using FModel.ViewModels.ApiEndpoints.Models;
using SkiaSharp;

namespace FModel.Creator.Bases.BB;

public class BaseHelpIcon : UCreator
{
    public BaseHelpIcon(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Background = new[] { SKColor.Parse("86886f"), SKColor.Parse("86886f") };
        Border = new[] { SKColor.Parse("abae8e"), SKColor.Parse("abae8e") };
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FStructFallback HelpSection, "HelpSection"))
        {
            if (HelpSection.TryGetValue(out FText Title, "Title"))
                DisplayName = Title.Text;

            if (HelpSection.TryGetValue(out FText description, "Description"))
                Description = description.Text;

            if (HelpSection.TryGetValue(out FSoftObjectPath ImageAssetData, "ImageAssetData"))
                Preview = Utils.GetBitmap(ImageAssetData);
        }
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawPreview(c);
        DrawTextBackground(c);
        DrawDisplayName(c);
        DrawDescription(c);

        return new[] { ret };
    }
}
