using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PakReader;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FModel.Methods.Assets.IconCreator.AthenaID
{
    class IconImage
    {
        public static void DrawIconImage(JArray AssetProperties)
        {
            JToken heroToken = AssetsUtility.GetPropertyTagImport<JToken>(AssetProperties, "HeroDefinition");
            JToken weaponToken = AssetsUtility.GetPropertyTagImport<JToken>(AssetProperties, "WeaponDefinition");
            if (heroToken != null)
            {
                string assetPath = "/FortniteGame/Content/Athena/Heroes/" + heroToken.Value<string>();
                DrawImageFromTagData(assetPath);
            }
            else if (weaponToken != null)
            {
                string assetPath = "/FortniteGame/Content/Athena/Items/Weapons/" + weaponToken.Value<string>();
                DrawImageFromTagData(assetPath);
            }
            else
            {
                JToken largePreviewImage = AssetProperties.Where(x => string.Equals(x["name"].Value<string>(), "LargePreviewImage")).FirstOrDefault();
                if (largePreviewImage != null)
                {
                    JToken assetPathName = largePreviewImage["tag_data"]["asset_path_name"];
                    if (assetPathName != null)
                    {
                        string texturePath = FoldersUtility.FixFortnitePath(assetPathName.Value<string>());
                        using (Stream image = AssetsUtility.GetStreamImageFromPath(texturePath))
                        {
                            if (image != null)
                            {
                                SKRect rect = new SKRect(3, 3, 512, 512);
                                IconCreator.ICCanvas.DrawBitmap(SKBitmap.Decode(image), rect);
                            }
                        }
                    }
                }
            }
        }

        private static void DrawImageFromTagData(string assetPath)
        {
            PakReader.PakReader reader = AssetsUtility.GetPakReader(assetPath);
            if (reader != null)
            {
                List<FPakEntry> entriesList = AssetsUtility.GetPakEntries(assetPath);
                string jsonData = AssetsUtility.GetAssetJsonData(reader, entriesList);

                if (AssetsUtility.IsValidJson(jsonData))
                {
                    dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                    JToken AssetMainToken = null;
                    if (jsonData.StartsWith("[") && jsonData.EndsWith("]"))
                    {
                        JArray AssetArray = JArray.FromObject(AssetData);
                        AssetMainToken = AssetArray[0];
                    }
                    else if (jsonData.StartsWith("{") && jsonData.EndsWith("}"))
                    {
                        AssetMainToken = AssetData;
                    }

                    if (AssetMainToken != null)
                    {
                        JArray pArray = AssetMainToken["properties"].Value<JArray>();
                        JToken largePreviewImage = pArray.Where(x => string.Equals(x["name"].Value<string>(), "LargePreviewImage")).FirstOrDefault();
                        if (largePreviewImage != null)
                        {
                            JToken assetPathName = largePreviewImage["tag_data"]["asset_path_name"];
                            if (assetPathName != null)
                            {
                                string texturePath = FoldersUtility.FixFortnitePath(assetPathName.Value<string>());
                                using (Stream image = AssetsUtility.GetStreamImageFromPath(texturePath))
                                {
                                    if (image != null)
                                    {
                                        SKRect rect = new SKRect(3, 3, 512, 512);
                                        IconCreator.ICCanvas.DrawBitmap(SKBitmap.Decode(image), rect);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
