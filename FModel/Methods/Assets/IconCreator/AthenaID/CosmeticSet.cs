using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets.IconCreator.AthenaID
{
    static class CosmeticSet
    {
        private static JArray ItemSetsArray { get; set; }

        public static string GetCosmeticSet(string SetTagName)
        {
            if (ItemSetsArray == null)
            {
                string jsonData = AssetsUtility.GetAssetJsonDataByPath("/FortniteGame/Content/Athena/Items/Cosmetics/Metadata/CosmeticSets", true);
                if (jsonData != null && AssetsUtility.IsValidJson(jsonData))
                {
                    dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                    JArray AssetArray = JArray.FromObject(AssetData);
                    ItemSetsArray = AssetArray[0]["rows"].Value<JArray>();
                    return SearchSetDisplayName(SetTagName);
                }
            }
            else
            {
                return SearchSetDisplayName(SetTagName);
            }
            return string.Empty;
        }

        private static string SearchSetDisplayName(string SetTagName)
        {
            JArray setArray = ItemSetsArray
                .Where(x => string.Equals(x["Item1"].Value<string>(), SetTagName))
                .Select(x => x["Item2"]["properties"].Value<JArray>())
                .FirstOrDefault();
            if (setArray != null)
            {
                JToken set_namespace = AssetsUtility.GetPropertyTagText<JToken>(setArray, "DisplayName", "namespace");
                JToken set_key = AssetsUtility.GetPropertyTagText<JToken>(setArray, "DisplayName", "key");
                JToken set_source_string = AssetsUtility.GetPropertyTagText<JToken>(setArray, "DisplayName", "source_string");
                if (set_namespace != null && set_key != null && set_source_string != null)
                {
                    string cosmeticSet = AssetTranslations.SearchTranslation(set_namespace.Value<string>(), set_key.Value<string>(), set_source_string.Value<string>());
                    string cosmeticPart = AssetTranslations.SearchTranslation("Fort.Cosmetics", "CosmeticItemDescription_SetMembership_NotRich", "\nPart of the {0} set.");
                    return string.Format(cosmeticPart, cosmeticSet);
                }
            }
            return string.Empty;
        }
    }
}
