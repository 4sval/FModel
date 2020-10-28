using FModel.Creator.Bases;
using FModel.PakReader;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.PropertyTagData;
using SkiaSharp;

namespace FModel.Creator.Icons
{
    static class LargeSmallImage
    {
        public static void GetPreviewImage(BaseIcon icon, StructProperty u)
        {
            if (u.Value is UObject o && o.TryGetValue("ResourceObject", out var v) && v is ObjectProperty resourceObject)
                icon.IconImage = Utils.GetObjectTexture(resourceObject);
        }
        public static void GetPreviewImage(BaseIcon icon, ObjectProperty o, string assetName) => GetPreviewImage(icon, o, assetName, true);
        public static void GetPreviewImage(BaseIcon icon, ObjectProperty o, string assetName, bool hightRes)
        {
            string path = o.Value.Resource?.OuterIndex.Resource?.ObjectName.String;
            if (path?.Equals("/Game/Athena/Items/Weapons/WID_Harvest_Pickaxe_STWCosmetic_Tier") == true)
                path += "_" + assetName.Substring(assetName.LastIndexOf(".") - 1, 1);

            Package p = Utils.GetPropertyPakPackage(path);
            if (p.HasExport() && !p.Equals(default))
            {
                if (GetPreviewImage(icon, p.GetIndexedExport<UObject>(0), hightRes))
                    return;
                else if (GetPreviewImage(icon, p.GetIndexedExport<UObject>(1), hightRes)) // FortniteGame/Content/Athena/Items/Cosmetics/Pickaxes/Pickaxe_ID_402_BlackKnightFemale1H.uasset
                    return;
            }
        }
        public static void GetPreviewImage(BaseIcon icon, SoftObjectProperty s) => icon.IconImage = Utils.GetSoftObjectTexture(s);

        private static bool GetPreviewImage(BaseIcon icon, UObject obj, bool hightRes)
        {
            if (obj != null)
            {
                if (hightRes && obj.TryGetValue("LargePreviewImage", out var sLarge) && sLarge is SoftObjectProperty largePreviewImage && !string.IsNullOrEmpty(largePreviewImage.Value.AssetPathName.String))
                {
                    GetPreviewImage(icon, largePreviewImage);
                    return true;
                }
                else if (obj.TryGetValue("SmallPreviewImage", out var sSmall1) && sSmall1 is SoftObjectProperty smallPreviewImage1 && !string.IsNullOrEmpty(smallPreviewImage1.Value.AssetPathName.String))
                {
                    GetPreviewImage(icon, smallPreviewImage1);
                    return true;
                }
                else if (obj.TryGetValue("SmallPreviewImage", out var sSmall2) && sSmall2 is ObjectProperty smallPreviewImage2 && !string.IsNullOrEmpty(smallPreviewImage2.Value.Resource.OuterIndex.Resource.ObjectName.String))
                {
                    icon.IconImage = Utils.GetObjectTexture(smallPreviewImage2);
                    return true;
                }
            }
            return false;
        }

        public static void DrawPreviewImage(SKCanvas c, IBase icon) =>
            c.DrawBitmap(icon.IconImage ?? icon.FallbackImage, new SKRect(icon.Margin, icon.Margin, icon.Width - icon.Margin, icon.Height - icon.Margin),
                new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
        
        public static void DrawNotStretchedPreviewImage(SKCanvas c, IBase icon)
        {
            SKBitmap i = icon.IconImage ?? icon.FallbackImage;
            int x = i.Width < icon.Width ? ((icon.Width / 2) - (i.Width / 2)) : icon.Margin;
            c.DrawBitmap(i, new SKRect(x, icon.Margin, (x + i.Width) - (icon.Margin * 2), i.Height - icon.Margin),
                new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
        }
    }
}
