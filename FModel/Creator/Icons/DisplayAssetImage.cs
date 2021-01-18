using FModel.Creator.Bases;
using FModel.PakReader;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Icons
{
    static class DisplayAssetImage
    {
        public static bool GetDisplayAssetImage(BaseIcon icon, IUExport o, ref string assetName)
        {
            string path;
            if (o.TryGetValue("DisplayAssetPath", out var d) && d is StructProperty da && da.Value is FSoftObjectPath daOut)
            {
                path = daOut.AssetPathName.String;
            }
            else
            {
                path = assetName.Substring(0, assetName.LastIndexOf(".")).Replace("Athena_Commando_", "");
                switch (path) // Modified matrix's temp fix
                {
                    case "CID_971_M_Jupiter_S0Z6M":
                    case "CID_964_M_Historian_869BC":
                    case "CID_990_M_GrilledCheese_SNX4K":
                        path = path.Substring(0, path.LastIndexOf("_"));
                        break;
                }
                path = "/Game/Catalog/MI_OfferImages/MI_" + path;
            }

            Package p = Utils.GetPropertyPakPackage(path);
            if (p != null && p.HasExport())
            {
                var obj = p.GetExport<UObject>();
                if (obj != null)
                {
                    if (obj.GetExport<ArrayProperty>("TextureParameterValues") is ArrayProperty textureParameterValues)
                    {
                        foreach (StructProperty textureParameter in textureParameterValues.Value)
                        {
                            if (textureParameter.Value is UObject parameter &&
                                parameter.TryGetValue("ParameterValue", out var i) && i is ObjectProperty value &&
                                parameter.TryGetValue("ParameterInfo", out var i1) && i1 is StructProperty i2 && i2.Value is UObject info &&
                                info.TryGetValue("Name", out var j1) && j1 is NameProperty name)
                            {
                                if (name.Value.String.Equals("OfferImage") || name.Value.String.Equals("Texture"))
                                {
                                    icon.IconImage = Utils.GetObjectTexture(value) ?? Utils.GetTexture($"{value.Value.Resource.OuterIndex.Resource.ObjectName.String}_1") ?? Utils.GetTexture($"{value.Value.Resource.OuterIndex.Resource.ObjectName.String}_01");
                                    assetName = "MI_" + assetName;
                                    return true;
                                }
                            }
                        }
                    }
                    return GenerateOldFormat(icon, obj, ref assetName);
                }
            }

            return GenerateOldFormat(icon, ref assetName);
        }

        private static bool GenerateOldFormat(BaseIcon icon, ref string assetName)
        {
            var p = Utils.GetPropertyPakPackage("/Game/Catalog/DisplayAssets/DA_Featured_" + assetName.Substring(0, assetName.LastIndexOf(".")));
            if (p != null && p.HasExport())
            {
                var obj = p.GetExport<UObject>();
                if (obj != null)
                {
                    return GenerateOldFormat(icon, obj, ref assetName);
                }
            }
            return false;
        }
        private static bool GenerateOldFormat(BaseIcon icon, UObject obj, ref string assetName)
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

            if (obj.TryGetValue(imageType, out var v1) && v1 is StructProperty s && s.Value is UObject type &&
                type.TryGetValue("ResourceObject", out var v2) && v2 is ObjectProperty resourceObject &&
                resourceObject.Value.Resource.OuterIndex.Resource != null &&
                !resourceObject.Value.Resource.OuterIndex.Resource.ObjectName.String.Contains("/Game/Athena/Prototype/Textures/"))
            {
                icon.IconImage = Utils.GetObjectTexture(resourceObject);
                assetName = "DA_Featured_" + assetName;
                return true;
            }
            return false;
        }
    }
}
