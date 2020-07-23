using FModel.Creator.Bases;
using PakReader.Pak;
using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;
using SkiaSharp;

namespace FModel.Creator.Rarities
{
    static class Serie
    {
        public static void GetRarity(BaseIcon icon, ObjectProperty o)
        {
            PakPackage p = Utils.GetPropertyPakPackage(o.Value.Resource.OuterIndex.Resource.ObjectName.String);
            if (p.HasExport() && !p.Equals(default))
            {
                var obj = p.GetExport<UObject>();
                if (obj != null)
                    GetRarity(icon, obj);
            }
        }

        public static void GetRarity(BaseIcon icon, IUExport export)
        {
            if (export.TryGetValue("BackgroundTexture", out var t) && t is SoftObjectProperty sop)
                icon.RarityBackgroundImage = Utils.GetSoftObjectTexture(sop);

            if (export.TryGetValue("Colors", out var v) && v is StructProperty s && s.Value is UObject colors)
            {
                if (colors.TryGetValue("Color1", out var c1) && c1 is StructProperty s1 && s1.Value is FLinearColor color1 &&
                    colors.TryGetValue("Color2", out var c2) && c2 is StructProperty s2 && s2.Value is FLinearColor color2 &&
                    colors.TryGetValue("Color4", out var c4) && c4 is StructProperty s4 && s4.Value is FLinearColor color4)
                {
                    icon.RarityBackgroundColors = new SKColor[2] { SKColor.Parse(color1.Hex), SKColor.Parse(color4.Hex) };
                    icon.RarityBorderColor = new SKColor[2] { SKColor.Parse(color2.Hex), SKColor.Parse(color1.Hex) };
                }
            }
        }
    }
}
