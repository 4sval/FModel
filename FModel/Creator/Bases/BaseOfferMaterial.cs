using SkiaSharp;
using System;
using System.Windows;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Bases
{
    public class BaseOfferMaterial
    {
        public SKBitmap FallbackImage;
        public SKBitmap IconImage;
        public SKBitmap RarityBackgroundImage;
        public SKColor[] RarityBackgroundColors;
        public SKColor RarityBorderColor;
        public int Size = 512;
        public int Margin = 2;

        public BaseOfferMaterial()
        {
            FallbackImage = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T_Placeholder_Item_Image.png")).Stream);
            IconImage = null;
            RarityBackgroundImage = null;
            RarityBackgroundColors = new SKColor[2] { SKColor.Parse("4F4F69"), SKColor.Parse("4F4F69") };
            RarityBorderColor = SKColor.Parse("9092AB");
        }

        public BaseOfferMaterial(IUExport export) : this()
        {
            if (export.GetExport<ArrayProperty>("VectorParameterValues") is ArrayProperty vectorParameterValues)
            {
                foreach (StructProperty vectorParameter in vectorParameterValues.Value)
                {
                    if (vectorParameter.Value is UObject parameter &&
                        parameter.TryGetValue("ParameterValue", out var i) && i is StructProperty v && v.Value is FLinearColor value &&
                        parameter.TryGetValue("ParameterInfo", out var i1) && i1 is StructProperty i2 && i2.Value is UObject info &&
                        info.TryGetValue("Name", out var j1) && j1 is NameProperty name)
                    {
                        if (name.Value.String.Equals("Background_Color_A"))
                        {
                            RarityBackgroundColors[0] = SKColor.Parse(value.Hex);
                            RarityBorderColor = RarityBackgroundColors[0];
                        }
                        else if (name.Value.String.Equals("Background_Color_B"))
                        {
                            RarityBackgroundColors[1] = SKColor.Parse(value.Hex);
                        }
                    }
                }
            }

            if (export.GetExport<ArrayProperty>("TextureParameterValues") is ArrayProperty textureParameterValues)
            {
                foreach (StructProperty textureParameter in textureParameterValues.Value)
                {
                    if (textureParameter.Value is UObject parameter &&
                        parameter.TryGetValue("ParameterValue", out var i) && i is ObjectProperty value &&
                        parameter.TryGetValue("ParameterInfo", out var i1) && i1 is StructProperty i2 && i2.Value is UObject info &&
                        info.TryGetValue("Name", out var j1) && j1 is NameProperty name)
                    {
                        if (name.Value.String.Equals("SeriesTexture"))
                        {
                            RarityBackgroundImage = Utils.GetObjectTexture(value);
                        }
                        else if (IconImage == null && value.Value.Resource.OuterIndex.Resource != null && (name.Value.String.Equals("OfferImage") || name.Value.String.Contains("Texture")))
                        {
                            IconImage = Utils.GetObjectTexture(value);
                            if (IconImage == null) IconImage = Utils.GetTexture($"{value.Value.Resource.OuterIndex.Resource.ObjectName.String}_1");
                            if (IconImage == null) IconImage = Utils.GetTexture($"{value.Value.Resource.OuterIndex.Resource.ObjectName.String}_01");
                        }
                    }
                }
            }

            if (IconImage == null)
                IconImage = FallbackImage;
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
            if (RarityBackgroundImage != null)
                c.DrawBitmap(RarityBackgroundImage, new SKRect(Margin, Margin, Size - Margin, Size - Margin),
                    new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

            c.DrawBitmap(IconImage ?? FallbackImage, new SKRect(Margin, Margin, Size - Margin, Size - Margin),
                new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
        }
    }
}
