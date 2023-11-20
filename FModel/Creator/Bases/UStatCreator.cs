using System;
using System.Collections.Generic;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Creator.Bases.FN;
using FModel.Extensions;
using FModel.Framework;
using FModel.Views.Resources.Controls;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases;

// I know inheriting BaseIcon fortnite is annoying but i didnt want to move all of it into basestat for fn again bc its a hassle, this isnt too much trouble being here
public class UStatCreator : BaseIcon
{
    protected IList<IconStat> _statistics;
    protected const int _headerHeight = 128;
    protected bool _screenLayer;

    public UStatCreator(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Width = 1024;
        Height = _headerHeight;
        Margin = 0;
        _statistics = new List<IconStat>();
    }

    public override void ParseForInfo()
    {
        base.ParseForInfo();

        DisplayName = DisplayName.ToUpperInvariant();

        if (!string.IsNullOrEmpty(Description))
            Height += 40 + (int) _informationPaint.TextSize * Utils.SplitLines(Description, _informationPaint, Width - 20).Count;
        Height += 50 * _statistics.Count;
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var c = new SKCanvas(ret);

        DrawHeader(c);
        DrawDisplayName(c);
        DrawStatistics(c);

        return new[] { ret };
    }

    protected SKPaint _informationPaint = new()
    {
        IsAntialias = true,
        FilterQuality = SKFilterQuality.High,
        Color = SKColor.Parse("#262630"),
        TextSize = 16,
        Typeface = Utils.Typefaces.Description
    };

    protected virtual void DrawHeader(SKCanvas c)
    {
        var UsedBMP = (Preview ?? DefaultPreview);

        c.DrawRect(new SKRect(0, 0, Width, Height), _informationPaint);

        _informationPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, _headerHeight / 2), Width / 5 * 4,
            new[] { Background[0].WithAlpha(180), Background[1].WithAlpha(220) }, SKShaderTileMode.Clamp);
        c.DrawRect(new SKRect(_headerHeight, 0, Width, _headerHeight), _informationPaint);

        _informationPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, _headerHeight / 2), Width / 5 * 4,
            new[] { SKColor.Parse("#262630"), SKColor.Parse("#1f1f26") }, SKShaderTileMode.Clamp);
        c.DrawRect(new SKRect(0, _headerHeight, Width, Height), _informationPaint);

        _informationPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(Width / 2, _headerHeight), new SKPoint(Width / 2, 75),
            new[] { SKColors.Black.WithAlpha(25), Background[1].WithAlpha(0) }, SKShaderTileMode.Clamp);
        c.DrawRect(new SKRect(0, 75, Width, _headerHeight), _informationPaint);

        _informationPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(Width / 2, _headerHeight / 2), Width / 5 * 4, Background, SKShaderTileMode.Clamp);
        using var rect = new SKPath { FillType = SKPathFillType.EvenOdd };
        rect.MoveTo(0, 0);
        rect.LineTo(_headerHeight + _headerHeight / 3, 0);
        rect.LineTo(_headerHeight, _headerHeight);
        rect.LineTo(0, _headerHeight);
        rect.Close();
        c.DrawPath(rect, _informationPaint);

        _informationPaint.Shader = SKShader.CreateLinearGradient(new SKPoint(_headerHeight / 2, _headerHeight / 2), new SKPoint(_headerHeight / 2 + 100, _headerHeight / 2),
            new[] { SKColors.Black.WithAlpha(25), Background[1].WithAlpha(0) }, SKShaderTileMode.Clamp);
        c.DrawPath(rect, _informationPaint);

        _informationPaint.Shader = null;

        ImagePaint.BlendMode = _screenLayer ? SKBlendMode.Screen : Preview == null ? SKBlendMode.ColorBurn : SKBlendMode.SrcOver;

        c.DrawBitmap(UsedBMP.Resize(_headerHeight, _headerHeight), 0, 0, ImagePaint);
    }

    protected new virtual void DrawDisplayName(SKCanvas c)
    {
        if (string.IsNullOrEmpty(DisplayName))
            return;

        _informationPaint.TextSize = 50;
        _informationPaint.Color = SKColors.White;
        _informationPaint.Typeface = Utils.Typefaces.DisplayName;
        while (_informationPaint.MeasureText(DisplayName) > Width - _headerHeight * 2)
        {
            _informationPaint.TextSize -= 1;
        }

        var shaper = new CustomSKShaper(_informationPaint.Typeface);
        c.DrawShapedText(shaper, DisplayName, _headerHeight + _headerHeight / 3 + 10, _headerHeight / 2f + _informationPaint.TextSize / 3, _informationPaint);
    }

    protected virtual void DrawStatistics(SKCanvas c)
    {
        var outY = _headerHeight + 25f;
        if (!string.IsNullOrEmpty(Description))
        {
            _informationPaint.TextSize = 16;
            _informationPaint.Color = SKColors.White.WithAlpha(175);
            _informationPaint.Typeface = Utils.Typefaces.Description;
            Utils.DrawMultilineText(c, Description, Width - 40, 0, SKTextAlign.Center,
                new SKRect(20, outY, Width - 20, Height), _informationPaint, out outY);
            outY += 25;
        }

        foreach (var stat in _statistics)
        {
            stat.Draw(c, Border[0].WithAlpha(100), Width, _headerHeight, ref outY);
            outY += 50;
        }
    }

    public static bool TryGetCurveTableStat(FStructFallback property, out float statValue)
    {
        if (property.TryGetValue(out FStructFallback curve, "Curve") &&
            curve.TryGetValue(out FName rowName, "RowName") &&
            curve.TryGetValue(out UCurveTable curveTable, "CurveTable") &&
            curveTable.TryFindCurve(rowName, out var rowValue) &&
            rowValue is FSimpleCurve s && s.Keys.Length > 0)
        {
            statValue = s.Keys[0].Value;
            return true;
        }

        statValue = 0F;
        return false;
    }
}

public class IconStat
{
    private readonly string _statName;
    private readonly object _value;
    private readonly float _maxValue;
    private readonly bool _slider;

    public IconStat(string statName, object value, float maxValue = 0, bool slider = true)
    {
        _statName = statName.ToUpperInvariant();
        _value = value;
        _maxValue = maxValue;
        _slider = slider;
    }

    public string GetName() => _statName;
    public object GetValue() => _value;

    private SKPaint _statPaint = new()
    {
        IsAntialias = true,
        FilterQuality = SKFilterQuality.High,
        TextSize = 25,
        Typeface = Utils.Typefaces.Description,
        Color = SKColors.White
    };

    public void Draw(SKCanvas c, SKColor sliderColor, int width, int height, ref float y)
    {
        while (_statPaint.MeasureText(_statName) > height * 2 + 400) // was (- 40)
        {
            _statPaint.TextSize -= 1;
        }

        var shaper = new CustomSKShaper(_statPaint.Typeface);
        c.DrawShapedText(shaper, _statName, 50, y + 10, _statPaint);

        if (_slider)
        {
            _statPaint.TextAlign = SKTextAlign.Right;
            _statPaint.Typeface = Utils.Typefaces.Description;
            _statPaint.Color = sliderColor;
            var sliderRight = width - 100 - _statPaint.MeasureText(_value.ToString());
            c.DrawRect(new SKRect(height * 2, y, Math.Min(width - height, sliderRight), y + 5), _statPaint);

            _statPaint.Color = SKColors.White;
            c.DrawText(_value.ToString(), new SKPoint(width - 50, y + 10), _statPaint);

            if (_maxValue < 1 || !float.TryParse(_value.ToString(), out var floatValue))
                return;
            if (floatValue < 0)
                floatValue = 0;
            var sliderWidth = (sliderRight - height * 2) * (floatValue / _maxValue);
            c.DrawRect(new SKRect(height * 2, y, Math.Min(height * 2 + sliderWidth, sliderRight), y + 5), _statPaint);
        }
    }
}
