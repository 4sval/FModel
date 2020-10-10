using FModel.Creator.Stats;
using FModel.Creator.Texts;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System.Collections.Generic;

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
            Color = SKColor.Parse("939498"),
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
                    Height += (int)descriptionPaint.TextSize * Helper.SplitLines(Description, descriptionPaint, Width - Margin).Count;
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
                                s.Height += (int)descriptionPaint.TextSize * Helper.SplitLines(s.Description, descriptionPaint, Width - Margin).Count;
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
            DrawCenteredTitle(c, DisplayName, 67.5f, out var textSize);

            Helper.DrawMultilineText(c, Description, Width, Margin, ETextSide.Center,
                new SKRect(Margin, textSize + 56.25f, Width - Margin, Height - 37.5f), descriptionPaint, out var yPos);

            if (IconImage != null)
                c.DrawBitmap(IconImage, new SKRect(0, yPos, Width, Height),
                    new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

            int yaPos = 0;
            foreach (Statistic ability in Abilities)
            {
                int xToAdd = ability.Icon != null ? ability.Icon.Width : 0;
                textSize = 42.5f;
                var namePaint = new SKPaint
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
                    new SKRect(Width + Margin + xToAdd + 10, textSize + yaPos + 27.5f, Width + AdditionalWidth - Margin, Height - 27.5f), descriptionPaint, out var _);

                if (ability.Icon != null)
                    c.DrawBitmap(ability.Icon, new SKRect(Width + Margin, yaPos, Width + Margin + ability.Icon.Width, yaPos + ability.Icon.Height),
                        new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

                yaPos += ability.Height + 48 + (Margin * 2);
            }
        }

        private void DrawCenteredTitle(SKCanvas c, string title, float textSize, out float outTextSize)
        {
            SKPaint namePaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = Text.TypeFaces.DisplayNameTypeface,
                TextSize = textSize,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Center,
            };
            float textWidth = namePaint.MeasureText(title);
            while (textWidth > Width) // resize if too long
            {
                namePaint.TextSize = textSize -= 2;
                textWidth = namePaint.MeasureText(title);
            }
            outTextSize = textSize;

            float x1 = (Width / 2 - (textWidth / 2)) - 20;
            float x2 = (x1 + textWidth) + 40;
            float y1 = Margin + 5;
            float y2 = Margin + namePaint.TextSize + 10;

            c.DrawLine(new SKPoint(30, y1 + 5 + (namePaint.TextSize / 2)), new SKPoint(x1 - 30, y1 + 5 + (namePaint.TextSize / 2)), new SKPaint { Color = SKColor.Parse("E2E8E6") });
            c.DrawLine(new SKPoint(x2 + 30, y1 + 5 + (namePaint.TextSize / 2)), new SKPoint(Width - 30, y1 + 5 + (namePaint.TextSize / 2)), new SKPaint { Color = SKColor.Parse("E2E8E6") });

            c.DrawLine(new SKPoint(x1, y1), new SKPoint(x2, y1), new SKPaint { Color = SKColor.Parse("E2E8E6") }); // top
            c.DrawLine(new SKPoint(x1, y2 + 5), new SKPoint(x2, y2 + 5), new SKPaint { Color = SKColor.Parse("E2E8E6") }); // bottom
            c.DrawLine(new SKPoint(x1, y1), new SKPoint(x1, y2 + 5), new SKPaint { Color = SKColor.Parse("E2E8E6") }); // left
            c.DrawLine(new SKPoint(x2, y1), new SKPoint(x2, y2 + 5), new SKPaint { Color = SKColor.Parse("E2E8E6") }); // right
            c.DrawRect(new SKRect(x1 + 5, y1 + 5, x2 - 5, y2), new SKPaint { Color = SKColor.Parse("949598") });

            c.DrawText(title, Width / 2, Margin + namePaint.TextSize, namePaint);
        }
    }
}
