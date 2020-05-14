using PakReader.Pak;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Icons
{
    static class DisplayAssetImage
    {
        public static bool GetDisplayAssetImage(BaseIcon icon, SoftObjectProperty o, string assetName)
        {
            string imageType = "DetailsImage";
            switch ("DA_Featured_" + assetName)
            {
                case "DA_Featured_Glider_ID_141_AshtonBoardwalk.uasset":
                case "DA_Featured_Glider_ID_150_TechOpsBlue.uasset":
                case "DA_Featured_Glider_ID_131_SpeedyMidnight.uasset":
                case "DA_Featured_Pickaxe_ID_178_SpeedyMidnight.uasset":
                case "DA_Featured_Glider_ID_015_Brite.uasset":
                case "DA_Featured_Glider_ID_016_Tactical.uasset":
                case "DA_Featured_Glider_ID_017_Assassin.uasset":
                case "DA_Featured_Pickaxe_ID_027_Scavenger.uasset":
                case "DA_Featured_Pickaxe_ID_028_Space.uasset":
                case "DA_Featured_Pickaxe_ID_029_Assassin.uasset":
                    return false;
                case "DA_Featured_Glider_ID_070_DarkViking.uasset":
                case "DA_Featured_CID_319_Athena_Commando_F_Nautilus.uasset":
                    imageType = "TileImage";
                    break;
            }

            string path = o?.Value.AssetPathName.String;
            if (string.IsNullOrEmpty(path))
                path = "/Game/Catalog/DisplayAssets/DA_Featured_" + assetName.Substring(0, assetName.LastIndexOf("."));

            PakPackage p = Utils.GetPropertyPakPackage(path);
            if (p.HasExport() && !p.Equals(default))
            {
                var obj = p.GetExport<UObject>();
                if (obj != null)
                {
                    if (obj.TryGetValue(imageType, out var v1) && v1 is StructProperty s && s.Value is UObject type &&
                        type.TryGetValue("ResourceObject", out var v2) && v2 is ObjectProperty resourceObject)
                    {
                        if (!resourceObject.Value.Resource.OuterIndex.Resource.ObjectName.String.Contains("/Game/Athena/Prototype/Textures/"))
                        {
                            icon.IconImage = Utils.GetObjectTexture(resourceObject);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
