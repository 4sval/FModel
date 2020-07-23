using FModel.Creator.Fortnite;
using PakReader.Pak;
using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System.Linq;

namespace FModel.Creator.Rarities
{
    static class Rarity
    {
        public static void GetInGameRarity(BaseIcon icon, EnumProperty e)
        {
            PakPackage p = Utils.GetPropertyPakPackage("/Game/Balance/RarityData");
            if (p.HasExport() && !p.Equals(default))
            {
                var d = p.GetExport<UObject>();
                if (d != null)
                {
                    EFortRarity rarity = EFortRarity.Uncommon;
                    switch (e?.Value.String)
                    {
                        case "EFortRarity::Common":
                        case "EFortRarity::Handmade":
                            rarity = EFortRarity.Common;
                            break;
                        case "EFortRarity::Rare":
                        case "EFortRarity::Sturdy":
                            rarity = EFortRarity.Rare;
                            break;
                        case "EFortRarity::Epic":
                        case "EFortRarity::Quality":
                            rarity = EFortRarity.Epic;
                            break;
                        case "EFortRarity::Legendary":
                        case "EFortRarity::Fine":
                            rarity = EFortRarity.Legendary;
                            break;
                        case "EFortRarity::Mythic":
                        case "EFortRarity::Elegant":
                            rarity = EFortRarity.Mythic;
                            break;
                        case "EFortRarity::Transcendent":
                        case "EFortRarity::Masterwork":
                            rarity = EFortRarity.Transcendent;
                            break;
                        case "EFortRarity::Unattainable":
                            rarity = EFortRarity.Unattainable;
                            break;
                    }

                    if (d.Values.ElementAt((int)rarity) is StructProperty s && s.Value is UObject colors)
                    {
                        if (colors.TryGetValue("Color1", out var c1) && c1 is StructProperty s1 && s1.Value is FLinearColor color1 &&
                            colors.TryGetValue("Color2", out var c2) && c2 is StructProperty s2 && s2.Value is FLinearColor color2 &&
                            colors.TryGetValue("Color3", out var c3) && c3 is StructProperty s3 && s3.Value is FLinearColor color3)
                        {
                            icon.RarityBackgroundColors = new SKColor[2] { SKColor.Parse(color1.Hex), SKColor.Parse(color3.Hex) };
                            icon.RarityBorderColor = new SKColor[2] { SKColor.Parse(color2.Hex), SKColor.Parse(color1.Hex) };
                        }
                    }
                }
            }
            else GetHardCodedRarity(icon, e);
        }

        public static void GetHardCodedRarity(BaseIcon icon, EnumProperty e)
        {
            switch (e?.Value.String)
            {
                case "EFortRarity::Common":
                case "EFortRarity::Handmade":
                    icon.RarityBackgroundColors = new SKColor[2] { SKColor.Parse("6D6D6D"), SKColor.Parse("333333") };
                    icon.RarityBorderColor = new SKColor[2] { SKColor.Parse("9E9E9E"), SKColor.Parse("9E9E9E") };
                    break;
                case "EFortRarity::Rare":
                case "EFortRarity::Sturdy":
                    icon.RarityBackgroundColors = new SKColor[2] { SKColor.Parse("3669BB"), SKColor.Parse("133254") };
                    icon.RarityBorderColor = new SKColor[2] { SKColor.Parse("5180EE"), SKColor.Parse("5180EE") };
                    break;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    icon.RarityBackgroundColors = new SKColor[2] { SKColor.Parse("8138C2"), SKColor.Parse("35155C") };
                    icon.RarityBorderColor = new SKColor[2] { SKColor.Parse("B251ED"), SKColor.Parse("B251ED") };
                    break;
                case "EFortRarity::Legendary":
                case "EFortRarity::Fine":
                    icon.RarityBackgroundColors = new SKColor[2] { SKColor.Parse("C06A38"), SKColor.Parse("5C2814") };
                    icon.RarityBorderColor = new SKColor[2] { SKColor.Parse("EC9650"), SKColor.Parse("EC9650") };
                    break;
                case "EFortRarity::Mythic":
                case "EFortRarity::Elegant":
                    icon.RarityBackgroundColors = new SKColor[2] { SKColor.Parse("BA9C36"), SKColor.Parse("594415") };
                    icon.RarityBorderColor = new SKColor[2] { SKColor.Parse("EED951"), SKColor.Parse("EED951") };
                    break;
                case "EFortRarity::Transcendent":
                case "EFortRarity::Masterwork":
                    icon.RarityBackgroundColors = new SKColor[2] { SKColor.Parse("D51944"), SKColor.Parse("660522") };
                    icon.RarityBorderColor = new SKColor[2] { SKColor.Parse("FF3F58"), SKColor.Parse("FF3F58") };
                    break;
            }
        }

        public static void DrawRarity(SKCanvas c, BaseIcon icon)
        {
            // border
            c.DrawRect(new SKRect(0, 0, icon.Size, icon.Size),
                new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    Shader = SKShader.CreateLinearGradient(
                        new SKPoint(icon.Size / 2, icon.Size),
                        new SKPoint(icon.Size, icon.Size / 4),
                        icon.RarityBorderColor,
                        SKShaderTileMode.Clamp)
                });

            switch ((EIconDesign)Properties.Settings.Default.AssetsIconDesign)
            {
                case EIconDesign.Flat:
                    {
                        if (icon.RarityBackgroundImage != null)
                            c.DrawBitmap(icon.RarityBackgroundImage, new SKRect(icon.Margin, icon.Margin, icon.Size - icon.Margin, icon.Size - icon.Margin),
                                new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
                        else
                        {
                            c.DrawRect(new SKRect(icon.Margin, icon.Margin, icon.Size - icon.Margin, icon.Size - icon.Margin),
                                new SKPaint
                                {
                                    IsAntialias = true,
                                    FilterQuality = SKFilterQuality.High,
                                    Color = icon.RarityBackgroundColors[0]
                                });

                            var paint = new SKPaint
                            {
                                IsAntialias = true,
                                FilterQuality = SKFilterQuality.High,
                                Color = icon.RarityBackgroundColors[1].WithAlpha(75)
                            };
                            var pathTop = new SKPath { FillType = SKPathFillType.EvenOdd };
                            pathTop.MoveTo(icon.Margin, icon.Margin);
                            pathTop.LineTo(icon.Margin + (icon.Size / 17 * 10), icon.Margin);
                            pathTop.LineTo(icon.Margin, icon.Margin + (icon.Size / 17));
                            pathTop.Close();
                            c.DrawPath(pathTop, paint);

                            var pathBottom = new SKPath { FillType = SKPathFillType.EvenOdd };
                            pathBottom.MoveTo(icon.Margin, icon.Size - icon.Margin);
                            pathBottom.LineTo(icon.Margin, icon.Size - icon.Margin - (icon.Size / 17 * 2.5f));
                            pathBottom.LineTo(icon.Size - icon.Margin, icon.Size - icon.Margin - (icon.Size / 17 * 4.5f));
                            pathBottom.LineTo(icon.Size - icon.Margin, icon.Size - icon.Margin);
                            pathBottom.Close();
                            c.DrawPath(pathBottom, paint);
                        }
                        break;
                    }
                default:
                    {
                        if (icon.RarityBackgroundImage != null)
                            c.DrawBitmap(icon.RarityBackgroundImage, new SKRect(icon.Margin, icon.Margin, icon.Size - icon.Margin, icon.Size - icon.Margin),
                                new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
                        else
                            c.DrawRect(new SKRect(icon.Margin, icon.Margin, icon.Size - icon.Margin, icon.Size - icon.Margin),
                                new SKPaint
                                {
                                    IsAntialias = true,
                                    FilterQuality = SKFilterQuality.High,
                                    Shader = SKShader.CreateRadialGradient(
                                        new SKPoint(icon.Size / 2, icon.Size / 2),
                                        icon.Size / 5 * 4,
                                        icon.RarityBackgroundColors,
                                        SKShaderTileMode.Clamp)
                                });
                        break;
                    }
            }
        }
    }
}
