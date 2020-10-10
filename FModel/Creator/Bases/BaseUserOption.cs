using FModel.Creator.Texts;
using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System.Collections.Generic;

namespace FModel.Creator.Bases
{
    public class Options
    {
        public string Option;
        public SKColor Color = SKColor.Parse("55C5FC").WithAlpha(150);
    }

    public class BaseUserOption
    {
        private readonly SKPaint descriptionPaint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Typeface = Text.TypeFaces.DisplayNameTypeface,
            TextSize = 25,
            Color = SKColor.Parse("88DBFF"),
        };

        public string OptionDisplayName;
        public string OptionDescription;
        public List<Options> OptionValues = new List<Options>();
        public int Width = 512;
        public int Height = 128;
        public int Margin = 32;

        public BaseUserOption(IUExport export)
        {
            if (export.GetExport<TextProperty>("OptionDisplayName") is TextProperty optionDisplayName)
                OptionDisplayName = Text.GetTextPropertyBase(optionDisplayName).ToUpperInvariant();
            if (export.GetExport<TextProperty>("OptionDescription") is TextProperty optionDescription)
            {
                OptionDescription = Text.GetTextPropertyBase(optionDescription);
                if (!string.IsNullOrEmpty(OptionDescription))
                {
                    Height += (int)descriptionPaint.TextSize * Helper.SplitLines(OptionDescription, descriptionPaint, Width - Margin).Count;
                    Height += (int)descriptionPaint.TextSize;
                }
            }

            if (export.GetExport<ArrayProperty>("OptionValues") is ArrayProperty optionValues)
            {
                OptionValues = new List<Options>(optionValues.Value.Length);
                for (int i = 0; i < OptionValues.Capacity; i++)
                {
                    if (optionValues.Value[i] is StructProperty s && s.Value is UObject option)
                    {
                        if (option.TryGetValue("DisplayName", out var v1) && v1 is TextProperty displayName)
                        {
                            var opt = new Options { Option = Text.GetTextPropertyBase(displayName).ToUpperInvariant() };
                            if (option.TryGetValue("Value", out var v) && v is StructProperty value && value.Value is FLinearColor color)
                                opt.Color = SKColor.Parse(color.Hex).WithAlpha(150);
                            OptionValues.Add(opt);
                        }
                        else if (option.TryGetValue("PrimaryAssetName", out var v2) && v2 is NameProperty primaryAssetName)
                            OptionValues.Add(new Options { Option = primaryAssetName.Value.String });
                    }
                }
            }

            if (export.GetExport<TextProperty>("OptionOnText") is TextProperty optionOnText)
                OptionValues.Add(new Options { Option = Text.GetTextPropertyBase(optionOnText).ToUpperInvariant() });
            if (export.GetExport<TextProperty>("OptionOffText") is TextProperty optionOffText)
                OptionValues.Add(new Options { Option = Text.GetTextPropertyBase(optionOffText).ToUpperInvariant() });

            if (export.GetExport<IntProperty>("Min", "DefaultValue") is IntProperty iMin &&
                export.GetExport<IntProperty>("Max") is IntProperty iMax)
            {
                int increment = iMin.Value;
                if (export.GetExport<IntProperty>("IncrementValue") is IntProperty incrementValue)
                    increment = incrementValue.Value;

                for (int i = iMin.Value; i <= iMax.Value; i += increment)
                {
                    OptionValues.Add(new Options { Option = i.ToString() });
                }
            }

            if (export.GetExport<FloatProperty>("Min") is FloatProperty fMin &&
                export.GetExport<FloatProperty>("Max") is FloatProperty fMax)
            {
                float increment = fMin.Value;
                if (export.GetExport<FloatProperty>("IncrementValue") is FloatProperty incrementValue)
                    increment = incrementValue.Value;

                for (float i = fMin.Value; i <= fMax.Value; i += increment)
                {
                    OptionValues.Add(new Options { Option = i.ToString() });
                }
            }

            Height += Margin;
            Height += 35 * OptionValues.Count;
        }

        public void Draw(SKCanvas c)
        {
            c.DrawRect(new SKRect(0, 0, Width, Height),
                new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    Shader = SKShader.CreateLinearGradient(
                        new SKPoint(Width / 2, Height),
                        new SKPoint(Width, Height / 4),
                        new SKColor[2] { SKColor.Parse("01369C"), SKColor.Parse("1273C8") },
                        SKShaderTileMode.Clamp)
                });

            int textSize = 45;
            SKPaint namePaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = Text.TypeFaces.DisplayNameTypeface,
                TextSize = textSize,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Left
            };

            // resize if too long
            while (namePaint.MeasureText(OptionDisplayName) > (Width - (Margin * 2)))
            {
                namePaint.TextSize = textSize -= 2;
            }
            int y = Margin + textSize;
            c.DrawText(OptionDisplayName, Margin, y, namePaint);
            y += (int)descriptionPaint.TextSize + (Margin / 2);

            // wrap if too long
            Helper.DrawMultilineText(c, OptionDescription, Width, Margin, ETextSide.Left,
                new SKRect(Margin, y, Width - Margin, 256), descriptionPaint, out int top);

            int height = 30;
            int space = 5;
            foreach (Options option in OptionValues)
            {
                c.DrawRect(new SKRect(Margin, top, Width - Margin, top + height),
                    new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High,
                        Color = option.Color
                    });

                int ts = 20;
                c.DrawText(option.Option, Margin + (space * 2), top + (ts * 1.1f),
                    new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High,
                        Typeface = Text.TypeFaces.DisplayNameTypeface,
                        TextSize = ts,
                        Color = SKColor.Parse("EEFFFF"),
                        TextAlign = SKTextAlign.Left
                    });

                top += height + space;
            }
        }
    }
}
