using FModel.Creator.Stats;
using FModel.Creator.Texts;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace FModel.Creator.Bases
{
    public class BaseUIData
    {
        private readonly SKPaint descriptionPaint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Typeface = Text.TypeFaces.DescriptionTypeface,
            TextSize = 19.5f,
            Color = SKColors.White,
        };

        public SKBitmap IconImage;
        public string DisplayName;
        public string Description;
        public List<Statistic> Abilities;
        public int Width = 768; // keep it 512 (or a multiple of 512) if you don't want blurry icons
        public int AdditionalWidth = 0;
        public int Height = 96;
        public int Margin = 3;

        public BaseUIData()
        {
            IconImage = null;
            DisplayName = "";
            Description = "";
            Abilities = new List<Statistic>();
        }

        public BaseUIData(IUExport[] exports, int baseIndex) : this()
        {
            if (exports[baseIndex].GetExport<TextProperty>("DisplayName") is TextProperty displayName)
                DisplayName = Text.GetTextPropertyBase(displayName) ?? "";
            if (exports[baseIndex].GetExport<TextProperty>("Description") is TextProperty description)
            {
                Description = Text.GetTextPropertyBase(description) ?? "";
                if (Description.Equals(DisplayName)) Description = string.Empty;
                if (!string.IsNullOrEmpty(Description))
                {
                    Height += (int)descriptionPaint.TextSize * Helper.SplitLines(Description, descriptionPaint, Width - Margin).Length;
                    Height += (int)descriptionPaint.TextSize;
                }
            }

            if (exports[baseIndex].GetExport<ObjectProperty>("StoreFeaturedImage", "FullRender", "VerticalPromoImage", "LargeIcon", "DisplayIcon2", "DisplayIcon") is ObjectProperty icon)
            {
                SKBitmap raw = Utils.GetObjectTexture(icon);
                if (raw != null)
                {
                    float coef = (float)Width / (float)raw.Width;
                    int sizeX = (int)(raw.Width * coef);
                    int sizeY = (int)(raw.Height * coef);
                    Height += sizeY;
                    IconImage = raw.Resize(sizeX, sizeY);
                }
            }

            if (exports[baseIndex].GetExport<MapProperty>("Abilities") is MapProperty abilities)
            {
                AdditionalWidth = 768;
                foreach (var (_, value) in abilities.Value)
                {
                    if (value is ObjectProperty o && o.Value.Resource == null && o.Value.Index > 0)
                    {
                        Statistic s = new Statistic();
                        if (exports[o.Value.Index - 1].GetExport<TextProperty>("DisplayName") is TextProperty aDisplayName)
                            s.DisplayName = Text.GetTextPropertyBase(aDisplayName) ?? "";
                        if (exports[o.Value.Index - 1].GetExport<TextProperty>("Description") is TextProperty aDescription)
                        {
                            s.Description = Text.GetTextPropertyBase(aDescription) ?? "";
                            if (!string.IsNullOrEmpty(Description))
                            {
                                s.Height += (int)descriptionPaint.TextSize * Helper.SplitLines(s.Description, descriptionPaint, Width - Margin).Length;
                                s.Height += (int)descriptionPaint.TextSize * 3;
                            }
                        }
                        if (exports[o.Value.Index - 1].GetExport<ObjectProperty>("DisplayIcon") is ObjectProperty displayIcon)
                        {
                            SKBitmap raw = Utils.GetObjectTexture(displayIcon);
                            if (raw != null) s.Icon = raw.Resize(128, 128);
                        }
                        Abilities.Add(s);
                    }
                }
            }
        }

        public void Draw(SKCanvas c)
        {
            float textSize = 67.5f;
            SKPaint namePaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = Text.TypeFaces.DisplayNameTypeface,
                TextSize = textSize,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Left,
            };

            // resize if too long
            while (namePaint.MeasureText(DisplayName) > Width)
            {
                namePaint.TextSize = textSize -= 2;
            }

            c.DrawText(DisplayName, Margin, Margin + textSize, namePaint);

            // wrap if too long
            Helper.DrawMultilineText(c, Description, Width, Margin, ETextSide.Left,
                new SKRect(Margin, textSize + 37.5f, Width - Margin, Height - 37.5f), descriptionPaint, out var yPos);

            if (IconImage != null)
                c.DrawBitmap(IconImage, new SKRect(0, yPos, Width, Height),
                    new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

            int yaPos = 0;
            foreach (Statistic ability in Abilities)
            {
                int xToAdd = ability.Icon != null ? ability.Icon.Width : 0;
                textSize = 42.5f;
                namePaint = new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    Typeface = Text.TypeFaces.DisplayNameTypeface,
                    TextSize = textSize,
                    Color = SKColors.White,
                    TextAlign = SKTextAlign.Left,
                };

                // resize if too long
                while (namePaint.MeasureText(ability.DisplayName) > Width - 128)
                {
                    namePaint.TextSize = textSize -= 2;
                }

                c.DrawText(ability.DisplayName, Width + Margin + xToAdd + 10, yaPos + Margin + textSize, namePaint);

                Helper.DrawMultilineText(c, ability.Description, Width, Width + Margin + xToAdd + 10, ETextSide.Left,
                    new SKRect(Margin, textSize + yaPos + 27.5f, Width + AdditionalWidth - Margin, Height - 27.5f), descriptionPaint, out var _);

                if (ability.Icon != null)
                    c.DrawBitmap(ability.Icon, new SKRect(Width + Margin, yaPos, Width + Margin + ability.Icon.Width, yaPos + ability.Icon.Height),
                        new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

                yaPos += ability.Height + 48 + (Margin * 2);
            }
        }
    }
}
