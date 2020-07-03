using FModel.Creator.Valorant;
using PakReader.Pak;
using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Labels
{
    static class ExplicitAsset
    {
        public static void GetAsset(BaseLabel icon, SoftObjectProperty s)
        {
            PakPackage p = Utils.GetPropertyPakPackage(s.Value.AssetPathName.String);
            if (p.HasExport() && !p.Equals(default))
            {
                var obj = p.GetIndexedExport<UObject>(0);
                if (obj != null)
                {
                    if (obj.TryGetValue("UIData", out var t) && t is SoftObjectProperty sop)
                    {
                        p = Utils.GetPropertyPakPackage(sop.Value.AssetPathName.String);
                        if (p.HasExport() && !p.Equals(default))
                        {
                            obj = p.GetIndexedExport<UObject>(0);
                            if (obj != null)
                            {
                                var uiData = new BaseUIData(obj);
                                icon.IconImages.Add(uiData);
                                icon.Height += uiData.IconImage.Height;
                            }
                        }
                    }
                }
            }
        }
    }
}
