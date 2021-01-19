using FModel.Creator.Bases;
using SkiaSharp;
using System.Linq;
using FModel.PakReader;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;
using FModel.PakReader.Parsers.PropertyTagData;
using FModel.Creator.Rarities;

namespace FModel.Chic
{
    static class ChicRarity
    {
        public static void GetInGameRarity(BaseIcon icon, EnumProperty e)
        {
            Package p = Creator.Utils.GetPropertyPakPackage("/Game/Balance/RarityData");
            if (p != null && p.HasExport())
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
                        case "EFortRarity::Badass":
                            rarity = EFortRarity.Unattainable;
                            break;
                    }

                    if (d.Values.ElementAt((int)rarity) is StructProperty s && s.Value is UObject colors)
                    {
                        if (colors.TryGetValue("Color1", out var c1) && c1 is StructProperty s1 && s1.Value is FLinearColor color1 &&
                            colors.TryGetValue("Color2", out var c2) && c2 is StructProperty s2 && s2.Value is FLinearColor color2 &&
                            colors.TryGetValue("Color3", out var c3) && c3 is StructProperty s3 && s3.Value is FLinearColor color3)
                        {
                            icon.RarityColors = new SKColor[2] { SKColor.Parse(color1.Hex), SKColor.Parse(color3.Hex) };
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
                    icon.RarityColors = new SKColor[2] { SKColor.Parse("6D6D6D"), SKColor.Parse("333333") };
                    break;
                case "EFortRarity::Rare":
                case "EFortRarity::Sturdy":
                    icon.RarityColors = new SKColor[2] { SKColor.Parse("3669BB"), SKColor.Parse("133254") };
                    break;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    icon.RarityColors = new SKColor[2] { SKColor.Parse("8138C2"), SKColor.Parse("35155C") };
                    break;
                case "EFortRarity::Legendary":
                case "EFortRarity::Fine":
                    icon.RarityColors = new SKColor[2] { SKColor.Parse("C06A38"), SKColor.Parse("5C2814") };
                    break;
                case "EFortRarity::Mythic":
                case "EFortRarity::Elegant":
                    icon.RarityColors = new SKColor[2] { SKColor.Parse("BA9C36"), SKColor.Parse("594415") };
                    break;
                case "EFortRarity::Transcendent":
                case "EFortRarity::Masterwork":
                    icon.RarityColors = new SKColor[2] { SKColor.Parse("5CDCE2"), SKColor.Parse("72C5F8") };
                    break;
            }
        }

        public static void DrawRarity(SKCanvas c, IBase icon)
        {
            if (icon.Margin > 0)
                c.DrawRect(new SKRect(0, 0, icon.Width + icon.Margin, icon.Height + icon.Margin),
                    new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High,
                        Color = new SKColor(20, 20, 20)
                    });

            if (icon is BaseIcon i && i.RarityBackgroundImage != null)
                c.DrawBitmap(i.RarityBackgroundImage, new SKRect(icon.Margin, icon.Margin, icon.Width - icon.Margin, icon.Height - icon.Margin),
                    new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
            else
            {
                c.DrawRect(new SKRect(icon.Margin, icon.Margin, icon.Width - icon.Margin, icon.Height - icon.Margin),
                    new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High,
                        Shader = SKShader.CreateRadialGradient(
                            new SKPoint(icon.Width / 2, icon.Height / 2),
                            icon.Width / 5 * 4,
                            new SKColor[] {
                                new SKColor(30, 30, 30),
                                new SKColor(50, 50, 50)
                            },
                            SKShaderTileMode.Clamp)
                    });
            }
        }
    }
}
