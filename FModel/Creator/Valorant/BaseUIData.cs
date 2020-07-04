using FModel.Creator.Texts;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;

namespace FModel.Creator.Valorant
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
        public int Width = 768; // keep it 512 (or a multiple of 512) if you don't want blurry icons
        public int Height = 96;
        public int Margin = 3;

        public BaseUIData()
        {
            IconImage = null;
            DisplayName = "";
            Description = "";
        }

        public BaseUIData(IUExport export) : this()
        {
            if (export.GetExport<TextProperty>("DisplayName") is TextProperty displayName)
                DisplayName = Text.GetTextPropertyBase(displayName) ?? "";
            if (export.GetExport<TextProperty>("Description") is TextProperty description)
            {
                Description = Text.GetTextPropertyBase(description) ?? "";
                if (Description.Equals(DisplayName)) Description = string.Empty;
                if (!string.IsNullOrEmpty(Description))
                {
                    Height += (int)descriptionPaint.TextSize * Helper.SplitLines(Description, descriptionPaint, Width - Margin).Length;
                    Height += (int)descriptionPaint.TextSize;
                }
            }

            if (export.GetExport<ObjectProperty>("StoreFeaturedImage", "FullRender", "VerticalPromoImage", "LargeIcon", "DisplayIcon2", "DisplayIcon") is ObjectProperty icon)
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
        }
    }
}
