using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using FModel.Framework;
using FModel.ViewModels;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.Views.Resources.Controls;
using SkiaSharp;

namespace FModel.Creator.Bases.VAL;


public class BasePlayableCharacter : UCreator
{
    public SKBitmap CharacterBackground { get; set; }

    protected readonly SKPaint BackgroundPaint = new()
    {
        IsAntialias = true,
        FilterQuality = SKFilterQuality.High
    };

    protected readonly SKPaint CharacterDescriptionPaint = new()
    {
        IsAntialias = true,
        FilterQuality = SKFilterQuality.High,
        Typeface = Utils.Typefaces.Description,
        TextSize = 20,
        Color = SKColors.White
    };

    public BasePlayableCharacter(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Background = new[] { SKColor.Parse("#262630"), SKColor.Parse("#1f1f26") };
        Border = new[] { SKColor.Parse("#262630"), SKColor.Parse("#1f1f26") };

        Width = 1024;
        Height = 930;

        StarterTextPos = 780;
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FSoftObjectPath UIDataPath, "UIData"))
        {
            var UIDataClass = UIDataPath.Load();
            var UIData = (UIDataClass as UBlueprintGeneratedClass).ClassDefaultObject.Load();

            if (UIData.TryGetValue(out FText displayName, "DisplayName"))
                DisplayName = displayName.Text;

            if (UIData.TryGetValue(out FText description, "Description"))
                Description = description.Text;

            if (UIData.TryGetValue(out FLinearColor BackgroundGradientColor1, "BackgroundGradientColor1")
                && UIData.TryGetValue(out FLinearColor BackgroundGradientColor2, "BackgroundGradientColor2")
                && UIData.TryGetValue(out FLinearColor BackgroundGradientColor3, "BackgroundGradientColor3")
                && UIData.TryGetValue(out FLinearColor BackgroundGradientColor4, "BackgroundGradientColor4"))
            {
                Background = new[] { SKColor.Parse(BackgroundGradientColor3.Hex), SKColor.Parse(BackgroundGradientColor4.Hex) };
                Border = new[] { SKColor.Parse(BackgroundGradientColor1.Hex), SKColor.Parse(BackgroundGradientColor2.Hex) };
            }
        }

        if (Object.TryGetValue(out FName DeveloperName, "DeveloperName"))
            DisplayName += $" ({DeveloperName})";

        if (Object.TryGetValue(out UObject FullPortrait, "FullPortrait"))
            Preview = Utils.GetBitmap(FullPortrait as UTexture2D);

        if (Object.TryGetValue(out UObject characterBackground, "CharacterBackground"))
            CharacterBackground = Utils.GetBitmap(characterBackground as UTexture2D);
    }

    protected void DrawCharacterBackground(SKCanvas c)
    {
        c.DrawBitmap(CharacterBackground ?? DefaultPreview, new SKRect(Margin, Margin, Width - Margin, Height - Margin), BackgroundPaint);
    }

    protected void DrawCharacterDescription(SKCanvas c)
    {
        if (string.IsNullOrWhiteSpace(Description))
            return;

        var maxLine = string.IsNullOrEmpty(DisplayName) ? 8 : 4;
        var side = SKTextAlign.Center;
        switch (Style)
        {
            case EIconStyle.Flat:
                side = SKTextAlign.Right;
                break;
        }

        Utils.DrawCenteredMultilineText(c, Description, maxLine, Width, Margin, side,
            new SKRect(Margin, string.IsNullOrEmpty(DisplayName) ? StarterTextPos : StarterTextPos + _NAME_TEXT_SIZE, Width - Margin, Height - _BOTTOM_TEXT_SIZE), CharacterDescriptionPaint);
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawCharacterBackground(c);
        DrawPreview(c);
        DrawTextBackground(c);
        DrawDisplayName(c);
        DrawCharacterDescription(c);

        return new[] { ret };
    }
}
