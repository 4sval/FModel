using SkiaSharp;
using System;
using System.Windows;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Bases
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
            if (export.GetExport<StructProperty>("DetailsImage", "TileImage") is StructProperty typeImage)
            {
                if (typeImage.Value is UObject t && t.TryGetValue("ResourceObject", out var v) && v is ObjectProperty resourceObject)
                {
                    IconImage = Utils.GetObjectTexture(resourceObject);
                }
            }

            if (export.GetExport<StructProperty>("Gradient") is StructProperty gradient)
            {
                if (gradient.Value is UObject g &&
                    g.TryGetValue("Start", out var s1) && s1 is StructProperty t1 && t1.Value is FLinearColor start &&
                    g.TryGetValue("Stop", out var s2) && s2 is StructProperty t2 && t2.Value is FLinearColor stop)
                {
                    RarityBackgroundColors = new SKColor[2] { SKColor.Parse(start.Hex), SKColor.Parse(stop.Hex) };
                }
            }

            if (export.GetExport<StructProperty>("Background") is StructProperty background)
            {
                if (background.Value is FLinearColor b)
                {
                    RarityBorderColor = SKColor.Parse(b.Hex);
                }
            }
        }

        public void DrawBackground(SKCanvas c)
        {
            if (RarityBackgroundColors[0] == RarityBackgroundColors[1])
                RarityBackgroundColors[0] = RarityBorderColor;

            RarityBackgroundColors[0].ToHsl(out var _, out var _, out var l1);
            RarityBackgroundColors[1].ToHsl(out var _, out var _, out var l2);
            bool reverse = l1 > l2;

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
                        new SKColor[2] { reverse ? RarityBackgroundColors[0] : RarityBackgroundColors[1], reverse ? RarityBackgroundColors[1] : RarityBackgroundColors[0] },
                        SKShaderTileMode.Clamp)
                });
        }

        public void DrawImage(SKCanvas c)
        {
            c.DrawBitmap(IconImage ?? FallbackImage, new SKRect(Margin, Margin, Size - Margin, Size - Margin),
                new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
        }
    }
}
