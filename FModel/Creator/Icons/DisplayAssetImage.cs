using FModel.Creator.Bases;
using FModel.PakReader;
using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Icons
{
    static class DisplayAssetImage
    {
        public static bool GetDisplayAssetImage(BaseIcon icon, SoftObjectProperty o, ref string assetName)
        {
            string path = o?.Value.AssetPathName.String;
            if (string.IsNullOrEmpty(path))
                path = "/Game/Catalog/MI_OfferImages/MI_" + assetName.Substring(0, assetName.LastIndexOf(".")).Replace("Athena_Commando_", "");

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
                                    icon.IconImage = Utils.GetObjectTexture(value);
                                    assetName = "MI_" + assetName;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
