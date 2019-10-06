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
        public static void DrawIconImage(JArray AssetProperties, bool isFeatured)
        {
            if (isFeatured)
            {
                JToken displayAssetPathToken = AssetsUtility.GetPropertyTagStruct<JToken>(AssetProperties, "DisplayAssetPath", "asset_path_name");
                if (displayAssetPathToken != null)
                {
                    string displayAssetPath = FoldersUtility.FixFortnitePath(displayAssetPathToken.Value<string>());
                    DrawFeaturedImageFromTagData(AssetProperties, displayAssetPath);
                }
                else if (AssetEntries.AssetEntriesDict.ContainsKey("/FortniteGame/Content/Catalog/DisplayAssets/DA_Featured_" + FWindow.FCurrentAsset + ".uasset"))
                {
                    string displayAssetPath = "/FortniteGame/Content/Catalog/DisplayAssets/DA_Featured_" + FWindow.FCurrentAsset;
                    DrawFeaturedImageFromTagData(AssetProperties, displayAssetPath);
                }
                else
                {
                    DrawIconImage(AssetProperties, false);
                }
            }
            else
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
                    string weaponName = weaponToken.Value<string>();

                    if (weaponToken.Value<string>().Equals("WID_Harvest_Pickaxe_STWCosmetic_Tier")) //STW PICKAXES MANUAL FIX
                    {
                        weaponName = "WID_Harvest_Pickaxe_STWCosmetic_Tier_" + FWindow.FCurrentAsset.Substring(FWindow.FCurrentAsset.Length - 1);
                    }

                    string assetPath = "/FortniteGame/Content/Athena/Items/Weapons/" + weaponName;
                    DrawImageFromTagData(assetPath);
                }
                else
                {
                    DrawLargeSmallImage(AssetProperties);
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

        private static void DrawFeaturedImageFromTagData(JArray AssetProperties, string displayAssetPath)
        {
            switch (displayAssetPath.Substring(displayAssetPath.LastIndexOf("/", System.StringComparison.InvariantCultureIgnoreCase) + 1))
            {
                case "DA_Featured_Glider_ID_141_AshtonBoardwalk":
                case "DA_Featured_Glider_ID_150_TechOpsBlue":
                case "DA_Featured_Glider_ID_131_SpeedyMidnight":
                case "DA_Featured_Pickaxe_ID_178_SpeedyMidnight":
                case "DA_Featured_Glider_ID_015_Brite":
                case "DA_Featured_Glider_ID_016_Tactical":
                case "DA_Featured_Glider_ID_017_Assassin":
                case "DA_Featured_Pickaxe_ID_027_Scavenger":
                case "DA_Featured_Pickaxe_ID_028_Space":
                case "DA_Featured_Pickaxe_ID_029_Assassin":
                    DrawIconImage(AssetProperties, false);
                    break;
                default:
                    DrawFeaturedImage(AssetProperties, displayAssetPath);
                    break;
            }
        }

        private static void DrawFeaturedImage(JArray AssetProperties, string displayAssetPath)
        {
            PakReader.PakReader reader = AssetsUtility.GetPakReader(displayAssetPath);
            if (reader != null)
            {
                List<FPakEntry> entriesList = AssetsUtility.GetPakEntries(displayAssetPath);
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
                        JArray displayAssetProperties = AssetMainToken["properties"].Value<JArray>();
                        switch (displayAssetPath.Substring(displayAssetPath.LastIndexOf("/", System.StringComparison.InvariantCultureIgnoreCase) + 1))
                        {
                            case "DA_Featured_Glider_ID_070_DarkViking":
                            case "DA_Featured_CID_319_Athena_Commando_F_Nautilus":
                                JArray TileImageProperties = AssetsUtility.GetPropertyTagStruct<JArray>(displayAssetProperties, "TileImage", "properties");
                                if (TileImageProperties != null)
                                {
                                    DrawFeaturedImageFromDisplayAssetProperty(AssetProperties, TileImageProperties);
                                }
                                break;
                            default:
                                JArray DetailsImageProperties = AssetsUtility.GetPropertyTagStruct<JArray>(displayAssetProperties, "DetailsImage", "properties");
                                if (DetailsImageProperties != null)
                                {
                                    DrawFeaturedImageFromDisplayAssetProperty(AssetProperties, DetailsImageProperties);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private static void DrawFeaturedImageFromDisplayAssetProperty(JArray AssetProperties, JArray displayAssetProperties)
        {
            JToken resourceObjectToken = AssetsUtility.GetPropertyTagOuterImport<JToken>(displayAssetProperties, "ResourceObject");
            if (resourceObjectToken != null)
            {
                string texturePath = FoldersUtility.FixFortnitePath(resourceObjectToken.Value<string>());
                if (texturePath.Contains("/FortniteGame/Content/Athena/Prototype/Textures/"))
                {
                    DrawIconImage(AssetProperties, false);
                }
                else
                {
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
