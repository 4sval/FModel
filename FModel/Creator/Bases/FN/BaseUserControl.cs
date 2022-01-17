using System.Collections.Generic;
using System.Globalization;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace FModel.Creator.Bases.FN
{
    public class BaseUserControl : UCreator
    {
        private List<Options> _optionValues = new();

        private readonly SKPaint _displayNamePaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High,
            Typeface = Utils.Typefaces.DisplayName, TextSize = 45,
            Color = SKColors.White, TextAlign = SKTextAlign.Left
        };
        private readonly SKPaint _descriptionPaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High,
            Typeface = Utils.Typefaces.DisplayName, TextSize = 25,
            Color = SKColor.Parse("88DBFF"), TextAlign = SKTextAlign.Left
        };

        public BaseUserControl(UObject uObject, EIconStyle style) : base(uObject, style)
        {
            Width = 512;
            Height = 128;
            Margin = 32;
        }

        public override void ParseForInfo()
        {
            if (Object.TryGetValue(out FText optionDisplayName, "OptionDisplayName"))
                DisplayName = optionDisplayName.Text.ToUpperInvariant();
            if (Object.TryGetValue(out FText optionDescription, "OptionDescription"))
            {
                Description = optionDescription.Text;

                if (string.IsNullOrWhiteSpace(Description)) return;
                Height += (int) _descriptionPaint.TextSize * Utils.SplitLines(Description, _descriptionPaint, Width - Margin).Count;
                Height += (int) _descriptionPaint.TextSize;
            }

            if (Object.TryGetValue(out FStructFallback[] optionValues, "OptionValues"))
            {
                _optionValues = new List<Options>();
                foreach (var option in optionValues)
                {
                    if (option.TryGetValue(out FText displayName, "DisplayName"))
                    {
                        var opt = new Options {Option = displayName.Text.ToUpperInvariant()};
                        if (option.TryGetValue(out FLinearColor color, "Value"))
                            opt.Color = SKColor.Parse(color.Hex).WithAlpha(150);

                        _optionValues.Add(opt);
                    }
                    else if (option.TryGetValue(out FName primaryAssetName, "PrimaryAssetName"))
                    {
                        _optionValues.Add(new Options {Option = primaryAssetName.Text});
                    }
                }
            }

            if (Object.TryGetValue(out FText optionOnText, "OptionOnText"))
                _optionValues.Add(new Options {Option = optionOnText.Text});
            if (Object.TryGetValue(out FText optionOffText, "OptionOffText"))
                _optionValues.Add(new Options {Option = optionOffText.Text});

            if (Object.TryGetValue(out int iMin, "Min", "DefaultValue") &&
                Object.TryGetValue(out int iMax, "Max"))
            {
                var increment = iMin;
                if (Object.TryGetValue(out int incrementValue, "IncrementValue"))
                    increment = incrementValue;

                var format = "{0}";
                if (Object.TryGetValue(out FText unitName, "UnitName"))
                    format = unitName.Text;

                for (var i = iMin; i <= iMax; i += increment)
                {
                    _optionValues.Add(new Options {Option = string.Format(format, i)});
                }
            }

            if (Object.TryGetValue(out float fMin, "Min", "DefaultValue") &&
                Object.TryGetValue(out float fMax, "Max"))
            {
                var increment = fMin;
                if (Object.TryGetValue(out float incrementValue, "IncrementValue"))
                    increment = incrementValue;

                var format = "{0}";
                if (Object.TryGetValue(out FText unitName, "UnitName"))
                    format = unitName.Text;

                for (var i = fMin; i <= fMax; i += increment)
                {
                    _optionValues.Add(new Options {Option = string.Format(format, i)});
                }
            }

            Height += Margin;
            Height += 35 * _optionValues.Count;
        }

        public override SKBitmap[] Draw()
        {
            var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var c = new SKCanvas(ret);

            DrawBackground(c);
            DrawInformation(c);

            return new []{ret};
        }

        private new void DrawBackground(SKCanvas c)
        {
            c.DrawRect(new SKRect(0, 0, Width, Height),
                new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    Shader = SKShader.CreateLinearGradient(
                        new SKPoint(Width / 2, Height),
                        new SKPoint(Width, Height / 4),
                        new[] {SKColor.Parse("01369C"), SKColor.Parse("1273C8")},
                        SKShaderTileMode.Clamp)
                });
        }

        private void DrawInformation(SKCanvas c)
        {
            // display name
            while (_displayNamePaint.MeasureText(DisplayName) > Width - Margin * 2)
            {
                _displayNamePaint.TextSize -= 2;
            }

            var shaper = new CustomSKShaper(_displayNamePaint.Typeface);
            shaper.Shape(DisplayName, _displayNamePaint);
            c.DrawShapedText(shaper, DisplayName, Margin, Margin + _displayNamePaint.TextSize, _displayNamePaint);
#if DEBUG
            c.DrawRect(new SKRect(Margin, Margin, Width - Margin, Margin + _displayNamePaint.TextSize), new SKPaint {Color = SKColors.Blue, IsStroke = true});
#endif

            // description
            float y = Margin;
            if (!string.IsNullOrEmpty(DisplayName)) y += _displayNamePaint.TextSize;
            if (!string.IsNullOrEmpty(Description)) y += _descriptionPaint.TextSize + Margin / 2F;

            Utils.DrawMultilineText(c, Description, Width, Margin, SKTextAlign.Left,
                new SKRect(Margin, y, Width - Margin, 256), _descriptionPaint, out var top);

            // options
            foreach (var option in _optionValues)
            {
                option.Draw(c, Margin, Width, ref top);
            }
        }
    }

    public class Options
    {
        private const int _SPACE = 5;
        private const int _HEIGHT = 30;

        private readonly SKPaint _optionPaint = new()
        {
            IsAntialias = true, FilterQuality = SKFilterQuality.High,
            Typeface = Utils.Typefaces.DisplayName, TextSize = 20,
            Color = SKColor.Parse("EEFFFF"), TextAlign = SKTextAlign.Left
        };

        public string Option;
        public SKColor Color = SKColor.Parse("55C5FC").WithAlpha(150);

        public void Draw(SKCanvas c, int margin, int width, ref float top)
        {
            c.DrawRect(new SKRect(margin, top, width - margin, top + _HEIGHT), new SKPaint {IsAntialias = true, FilterQuality = SKFilterQuality.High, Color = Color});
            c.DrawText(Option, margin + _SPACE * 2, top + 20 * 1.1f, _optionPaint);
            top += _HEIGHT + _SPACE;
        }
    }
}
