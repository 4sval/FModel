using FModel.Methods.Utilities;
using Newtonsoft.Json.Linq;
using System;
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
                    //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
                    string assetPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.ToLowerInvariant().Contains("/" + heroToken.Value<string>().ToLowerInvariant() + ".uasset")).Select(d => d.Key).FirstOrDefault();
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        DrawImageFromTagData(assetPath.Substring(0, assetPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase)));
                    }
                }
                else if (weaponToken != null)
                {
                    string weaponName = weaponToken.Value<string>();
                    if (weaponToken.Value<string>().Equals("WID_Harvest_Pickaxe_STWCosmetic_Tier")) //STW PICKAXES MANUAL FIX
                    {
                        weaponName = "WID_Harvest_Pickaxe_STWCosmetic_Tier_" + FWindow.FCurrentAsset.Substring(FWindow.FCurrentAsset.Length - 1);
                    }

                    //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
                    string assetPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.ToLowerInvariant().Contains("/" + weaponName.ToLowerInvariant() + ".uasset")).Select(d => d.Key).FirstOrDefault();
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        DrawImageFromTagData(assetPath.Substring(0, assetPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase)));
                    }
                }
                else
                {
                    DrawLargeSmallImage(AssetProperties);
                }
            }
        }

        private static void DrawImageFromTagData(string assetPath)
        {
            string jsonData = AssetsUtility.GetAssetJsonDataByPath(assetPath);
            if (jsonData != null)
            {
                if (AssetsUtility.IsValidJson(jsonData))
                {
                    JToken AssetMainToken = AssetsUtility.ConvertJson2Token(jsonData);
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
                            bmp.Freeze();

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
            string jsonData = AssetsUtility.GetAssetJsonDataByPath(displayAssetPath);
            if (jsonData != null)
            {
                if (AssetsUtility.IsValidJson(jsonData))
                {
                    FWindow.FCurrentAsset = Path.GetFileName(displayAssetPath);

                    JToken AssetMainToken = AssetsUtility.ConvertJson2Token(jsonData);
                    if (AssetMainToken != null)
                    {
                        JArray displayAssetProperties = AssetMainToken["properties"].Value<JArray>();
                        switch (displayAssetPath.Substring(displayAssetPath.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase) + 1))
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
            JToken resourceObjectOuterImportToken = AssetsUtility.GetPropertyTagOuterImport<JToken>(displayAssetProperties, "ResourceObject");
            JToken resourceObjectImportToken = AssetsUtility.GetPropertyTagImport<JToken>(displayAssetProperties, "ResourceObject");
            if (resourceObjectOuterImportToken != null && resourceObjectOuterImportToken.Value<string>() != null)
            {
                string texturePath = FoldersUtility.FixFortnitePath(resourceObjectOuterImportToken.Value<string>());
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
                            bmp.Freeze();

                            IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(3, 3, 509, 509));
                        }
                    }

                    if (AssetsLoader.ExportType == "AthenaItemWrapDefinition" && texturePath.Contains("WeaponRenders"))
                    {
                        DrawAdditionalWrapImage(AssetProperties);
                    }
                }
            }
            else if (resourceObjectImportToken != null)
            {
                //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
                string renderSwitchPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.Contains("/" + resourceObjectImportToken.Value<string>())).Select(d => d.Key).FirstOrDefault();
                if (!string.IsNullOrEmpty(renderSwitchPath))
                {
                    if (renderSwitchPath.Contains("MI_UI_FeaturedRenderSwitch_") || 
                        renderSwitchPath.Contains("M-Wraps-StreetDemon") || 
                        renderSwitchPath.Contains("/FortniteGame/Content/UI/Foundation/Textures/Icons/Wraps/FeaturedMaterials/"))
                    {
                        string jsonData = AssetsUtility.GetAssetJsonDataByPath(renderSwitchPath.Substring(0, renderSwitchPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase)));
                        if (jsonData != null)
                        {
                            if (AssetsUtility.IsValidJson(jsonData))
                            {
                                JToken AssetMainToken = AssetsUtility.ConvertJson2Token(jsonData);
                                if (AssetMainToken != null)
                                {
                                    JArray renderSwitchProperties = AssetMainToken["properties"].Value<JArray>();
                                    if (renderSwitchProperties != null)
                                    {
                                        JArray textureParameterArray = AssetsUtility.GetPropertyTagText<JArray>(renderSwitchProperties, "TextureParameterValues", "data");
                                        textureParameterArray = textureParameterArray[textureParameterArray.Count() > 1 && !AssetsLoader.ExportType.Equals("AthenaItemWrapDefinition") ? 1 : 0]["struct_type"]["properties"].Value<JArray>();
                                        if (textureParameterArray != null)
                                        {
                                            JToken parameterValueToken = AssetsUtility.GetPropertyTagOuterImport<JToken>(textureParameterArray, "ParameterValue");
                                            if (parameterValueToken != null)
                                            {
                                                string texturePath = FoldersUtility.FixFortnitePath(parameterValueToken.Value<string>());
                                                using (Stream image = AssetsUtility.GetStreamImageFromPath(texturePath))
                                                {
                                                    if (image != null)
                                                    {
                                                        BitmapImage bmp = new BitmapImage();
                                                        bmp.BeginInit();
                                                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                                                        bmp.StreamSource = image;
                                                        bmp.EndInit();
                                                        bmp.Freeze();

                                                        IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(3, 3, 509, 509));
                                                    }
                                                }

                                                if (AssetsLoader.ExportType == "AthenaItemWrapDefinition" && texturePath.Contains("WeaponRenders"))
                                                {
                                                    DrawAdditionalWrapImage(AssetProperties);
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
        }

        private static void DrawAdditionalWrapImage(JArray AssetProperties)
        {
            JToken largePreviewImage = AssetProperties.Where(x => string.Equals(x["name"].Value<string>(), "LargePreviewImage")).FirstOrDefault();
            JToken smallPreviewImage = AssetProperties.Where(x => string.Equals(x["name"].Value<string>(), "SmallPreviewImage")).FirstOrDefault();
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
                            bmp.Freeze();

                            IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(275, 272, 122, 122));
                        }
                    }
                }
            }
        }
    }
}
