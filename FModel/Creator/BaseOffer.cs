using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System;
using System.Windows;

namespace FModel.Creator
{
    public class BaseOffer
    {
        public SKBitmap FallbackImage;
        public SKBitmap IconImage;
        public SKColor[] RarityBackgroundColors;
        public SKColor RarityBorderColor;
        public int Size = 512;
        public int Margin = 2;

        public BaseOffer()
        {
            FallbackImage = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T_Placeholder_Item_Image.png")).Stream);
            IconImage = FallbackImage;
            RarityBackgroundColors = new SKColor[2] { SKColor.Parse("4F4F69"), SKColor.Parse("4F4F69") };
            RarityBorderColor = SKColor.Parse("9092AB");
        }

        public BaseOffer(IUExport export) : this()
        {
            if (export.TryGetValue("DetailsImage", out var v1) && v1 is StructProperty s &&
                s.Value is UObject typeImage && typeImage.TryGetValue("ResourceObject", out var v2) && v2 is ObjectProperty resourceObject)
            {
                IconImage = Utils.GetObjectTexture(resourceObject);
            }

            if (export.TryGetValue("Gradient", out var g) && g is StructProperty r && r.Value is UObject gradient)
            {
                if (gradient.TryGetValue("Start", out var s1) && s1 is StructProperty t1 && t1.Value is FLinearColor start &&
                    gradient.TryGetValue("Stop", out var s2) && s2 is StructProperty t2 && t2.Value is FLinearColor stop)
                {
                    RarityBackgroundColors = new SKColor[2] { SKColor.Parse(stop.Hex), SKColor.Parse(start.Hex) };
                }
            }

            if (export.TryGetValue("Background", out var b) && b is StructProperty a && a.Value is FLinearColor background)
                RarityBorderColor = SKColor.Parse(background.Hex);
        }

        public void Draw(SKCanvas c)
        {
            // border
            c.DrawRect(new SKRect(0, 0, Size, Size),
                new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    Color = RarityBorderColor
                });

            c.DrawRect(new SKRect(Margin, Margin, Size - Margin, Size - Margin),
                new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    Shader = SKShader.CreateRadialGradient(
                        new SKPoint(Size / 2, Size / 2),
                        Size / 5 * 4,
                        RarityBackgroundColors,
                        SKShaderTileMode.Clamp)
                });

            c.DrawBitmap(IconImage ?? FallbackImage, new SKRect(Margin, Margin, Size - Margin, Size - Margin),
                new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
        }
    }
}
