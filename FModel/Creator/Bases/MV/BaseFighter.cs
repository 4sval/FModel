using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.Creator.Bases.MV;

public class BaseFighter : UCreator
{
    private float _xOffset;
    private float _yOffset;
    private float _zoom;
    private SKBitmap _pattern;

    public BaseFighter(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        // https://cdn.discordapp.com/attachments/715640455068385422/1003052259917168700/unknown.png
        Width = 1024;
        DisplayNamePaint.TextAlign = SKTextAlign.Left;
        DisplayNamePaint.TextSize = 100;
        DefaultPreview = Utils.GetBitmap("MultiVersus/Content/Panda_Main/UI/PreMatch/Images/DiamondPortraits/0010_Random.0010_Random");
        _pattern = Utils.GetBitmap("MultiVersus/Content/Panda_Main/UI/Assets/UI_Textures/halftone_jagged.halftone_jagged");
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FLinearColor backgroundColor, "BackgroundColor"))
            Background = new[] { SKColor.Parse(backgroundColor.Hex) };

        if (Object.TryGetValue(out FSoftObjectPath portraitMaterial, "CollectionsPortraitMaterial") &&
            portraitMaterial.TryLoad(out UMaterialInstanceConstant portrait))
        {
            _xOffset = Math.Abs(portrait.ScalarParameterValues.FirstOrDefault(x => x.ParameterInfo.Name.Text == "XOffset")?.ParameterValue ?? 1f);
            _yOffset = Math.Abs(portrait.ScalarParameterValues.FirstOrDefault(x => x.ParameterInfo.Name.Text == "YOffset")?.ParameterValue / 10 ?? 1f);
            _zoom = Math.Clamp(portrait.ScalarParameterValues.FirstOrDefault(x => x.ParameterInfo.Name.Text == "Zoom")?.ParameterValue ?? 1f, 0, 1);
            Preview = Utils.GetBitmap(portrait);
        }

        if (Object.TryGetValue(out FText displayName, "DisplayName"))
            DisplayName = displayName.Text;
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        DrawBackground(c);
        DrawPreview(c);
        DrawDisplayName(c);

        return new[] { ret };
    }

    private new void DrawBackground(SKCanvas c)
    {
        c.DrawRect(new SKRect(0, 0, Width, Height),
            new SKPaint
            {
                IsAntialias = true, FilterQuality = SKFilterQuality.High, Color = Background[0]
            });

        if (!string.IsNullOrWhiteSpace(DisplayName))
        {
            c.DrawText(DisplayName, -50, 125, new()
            {
                IsAntialias = true, FilterQuality = SKFilterQuality.High,
                Typeface = Utils.Typefaces.DisplayName, TextSize = 200,
                TextScaleX = .95f, TextSkewX = -0.25f, Color = SKColors.Black.WithAlpha(25)
            });
        }

        c.DrawBitmap(_pattern, new SKRect(0, 256, Width, 512), new SKPaint
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High, BlendMode = SKBlendMode.SoftLight
        });

        var path = new SKPath { FillType = SKPathFillType.EvenOdd };
        path.MoveTo(0, 512);
        path.LineTo(0, 492);
        path.LineTo(Width, 452);
        path.LineTo(Width, 512);
        path.Close();
        c.DrawPath(path, new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Color = SKColor.Parse("#141629")
        });
    }

    private new void DrawPreview(SKCanvas c)
    {
        var img = (Preview ?? DefaultPreview).ResizeWithRatio(_zoom);
        var x_offset = img.Width * _xOffset;
        var y_offset = img.Height * -_yOffset;
        c.DrawBitmap(img, new SKRect(Width + x_offset - img.Width, y_offset, Width + x_offset, img.Height + y_offset), ImagePaint);
    }

    private new void DrawDisplayName(SKCanvas c)
    {
        if (string.IsNullOrWhiteSpace(DisplayName)) return;
        c.DrawText(DisplayName.ToUpper(), 50, 100, DisplayNamePaint);
    }
}
