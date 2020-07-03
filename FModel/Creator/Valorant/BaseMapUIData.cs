using FModel.Creator.Texts;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;

namespace FModel.Creator.Valorant
{
    public class BaseMapUIData
    {
        public SKBitmap Splash;
        public SKBitmap VLogo;
        public string DisplayName;
        public string Description;
        public string Coordinates;
        public int Width = 1920;
        public int Height = 1080;

        public BaseMapUIData()
        {
            Splash = null;
            VLogo = null;
            DisplayName = "";
            Description = "";
            Coordinates = "";
        }

        public BaseMapUIData(IUExport export) : this()
        {
            if (export.GetExport<TextProperty>("DisplayName") is TextProperty displayName)
                DisplayName = Text.GetTextPropertyBase(displayName) ?? "";
            if (export.GetExport<TextProperty>("Description") is TextProperty description)
                Description = Text.GetTextPropertyBase(description) ?? "";
            if (export.GetExport<TextProperty>("Coordinates") is TextProperty coordinates)
                Coordinates = Text.GetTextPropertyBase(coordinates) ?? "";

            if (export.GetExport<ObjectProperty>("Splash") is ObjectProperty icon)
                Splash = Utils.GetObjectTexture(icon);

            VLogo = Utils.GetTexture("/Game/UI/Shared/Icons/Valorant_logo_cutout").Resize(48, 48);

            if (Splash != null)
            {
                Width = Splash.Width;
                Height = Splash.Height;
            }
        }

        public void Draw(SKCanvas c)
        {
            int paddingLR = 80;
            int paddingTB = 35;
            int nameSize = 200;
            int descriptionSize = 30;
            using var namePaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = Text.TypeFaces.DisplayNameTypeface,
                TextSize = nameSize,
                TextAlign = SKTextAlign.Left,
                Color = SKColor.Parse("FFFBFA")
            };
            while (namePaint.MeasureText(DisplayName) > Width - (paddingLR * 2))
            {
                namePaint.TextSize = nameSize -= 2;
            }
            using var descriptionPaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = Text.TypeFaces.DescriptionTypeface,
                TextSize = descriptionSize,
                TextAlign = SKTextAlign.Left,
                Color = SKColor.Parse("FFFBFA")
            };
            while (descriptionPaint.MeasureText(Description) > Width - (paddingLR * 2))
            {
                descriptionPaint.TextSize = descriptionSize -= 2;
            }

            c.DrawBitmap(Splash, new SKRect(0, 0, Width, Height), new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
            c.DrawText(DisplayName.ToUpper(), paddingLR, paddingTB + namePaint.TextSize, namePaint);
            c.DrawRect(new SKRect(paddingLR + 2.5f, paddingTB + 25 + namePaint.TextSize, paddingLR + 202.5f, paddingTB + 27.5f + namePaint.TextSize), new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true, Color = SKColor.Parse("5AFFFBFA") });
            c.DrawText(Description, paddingLR + 2.5f, paddingTB + 40 + namePaint.TextSize + descriptionPaint.TextSize, descriptionPaint);

            descriptionPaint.Typeface = Text.TypeFaces.BundleDefaultTypeface;
            c.DrawText(Coordinates.ToUpper(), paddingLR, Height - paddingTB - descriptionPaint.TextSize, descriptionPaint);

            if (VLogo != null)
            {
                c.DrawBitmap(VLogo, new SKRect(Width - VLogo.Width - paddingLR, paddingLR, Width - paddingLR, paddingLR + VLogo.Height), new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
                c.DrawRect(new SKRect(Width - VLogo.Width - paddingLR, paddingLR + VLogo.Height + 5, Width - paddingLR, paddingLR + VLogo.Height + 7.5f), new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true, Color = SKColor.Parse("FFFBFA") });
            }
        }
    }
}
