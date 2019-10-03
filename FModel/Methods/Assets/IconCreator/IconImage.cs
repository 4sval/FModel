using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PakReader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FModel.Methods.Assets.IconCreator
{
    static class IconImage
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
                DrawLargeSmallImage(AssetProperties);
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
                        JArray AssetProperties = AssetMainToken["properties"].Value<JArray>();
                        DrawLargeSmallImage(AssetProperties);
                    }
                }
            }
        }

        private static void DrawLargeSmallImage(JArray propertiesArray)
        {
            JToken largePreviewImage = propertiesArray.Where(x => string.Equals(x["name"].Value<string>(), "LargePreviewImage")).FirstOrDefault();
            JToken smallPreviewImage = propertiesArray.Where(x => string.Equals(x["name"].Value<string>(), "SmallPreviewImage")).FirstOrDefault();
            if (largePreviewImage != null || smallPreviewImage != null)
            {
                JToken assetPathName =
                    largePreviewImage != null ? largePreviewImage["tag_data"]["asset_path_name"] :
                    smallPreviewImage != null ? smallPreviewImage["tag_data"]["asset_path_name"] : null;

                if (assetPathName != null)
                {
                    string texturePath = FoldersUtility.FixFortnitePath(assetPathName.Value<string>());
                    using (Stream image = AssetsUtility.GetStreamImageFromPath(texturePath))
                    {
                        if (image != null)
                        {
                            BitmapImage bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = image;
                            bmp.EndInit();

                            IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(3, 3, 509, 509));
                        }
                    }
                }
            }
        }
    }
}
