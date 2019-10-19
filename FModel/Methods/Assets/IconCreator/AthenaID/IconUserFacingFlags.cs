﻿using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PakReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FModel.Methods.Assets.IconCreator.AthenaID
{
    static class IconUserFacingFlags
    {
        private static JArray ItemCategoriesArray { get; set; }
        public static int xCoords = 4 - 25;

        public static void DrawUserFacingFlag(JToken uFF)
        {
            if (ItemCategoriesArray == null)
            {
                PakReader.PakReader reader = AssetsUtility.GetPakReader("/FortniteGame/Content/Items/ItemCategories");
                if (reader != null)
                {
                    List<FPakEntry> entriesList = AssetsUtility.GetPakEntries("/FortniteGame/Content/Items/ItemCategories");
                    string jsonData = AssetsUtility.GetAssetJsonData(reader, entriesList, true);
                    if (AssetsUtility.IsValidJson(jsonData))
                    {
                        dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                        JArray AssetArray = JArray.FromObject(AssetData);
                        JToken tertiaryCategoriesToken = AssetsUtility.GetPropertyTag<JToken>(AssetArray[0]["properties"].Value<JArray>(), "TertiaryCategories");
                        if (tertiaryCategoriesToken != null)
                        {
                            ItemCategoriesArray = tertiaryCategoriesToken["data"].Value<JArray>();

                            string uFFTargeted = uFF.Value<string>().Substring("Cosmetics.UserFacingFlags.".Length);
                            SearchUserFacingFlag(uFFTargeted);
                        }
                    }
                }
            }
            else
            {
                string uFFTargeted = uFF.Value<string>().Substring("Cosmetics.UserFacingFlags.".Length);
                SearchUserFacingFlag(uFFTargeted);
            }
        }

        private static void SearchUserFacingFlag(string uFFTarget)
        {
            foreach (JToken data in ItemCategoriesArray)
            {
                JArray propertiesArray = data["struct_type"]["properties"].Value<JArray>();
                if (propertiesArray != null)
                {
                    JToken categoryNameToken = AssetsUtility.GetPropertyTagText<JToken>(propertiesArray, "CategoryName", "source_string");
                    if (categoryNameToken != null)
                    {
                        if (
                            uFFTarget.Contains("Animated") && categoryNameToken.Value<string>() == "Animated" ||
                            uFFTarget.Contains("HasVariants") && categoryNameToken.Value<string>() == "Unlockable Styles" ||
                            uFFTarget.Contains("Reactive") && categoryNameToken.Value<string>() == "Reactive" ||
                            uFFTarget.Contains("Traversal") && categoryNameToken.Value<string>() == "Traversal" ||
                            uFFTarget.Contains("BuiltInEmote") && categoryNameToken.Value<string>() == "Built-in")
                        {
                            GetUFFImage(propertiesArray);
                        }
                        else if (uFFTarget.Contains("HasUpgradeQuests") && categoryNameToken.Value<string>() == "Unlockable Styles")
                        {
                            if (AssetsLoader.ExportType == "AthenaPetCarrierItemDefinition")
                            {
                                BitmapImage bmp = new BitmapImage();
                                bmp.BeginInit();
                                bmp.CacheOption = BitmapCacheOption.OnLoad;
                                bmp.UriSource = new Uri("pack://application:,,,/Resources/T-Icon-Pets-64.png");
                                bmp.EndInit();
                                bmp.Freeze();

                                xCoords += 25;
                                IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(xCoords, 4, 25, 25));
                            }
                            else
                            {
                                BitmapImage bmp = new BitmapImage();
                                bmp.BeginInit();
                                bmp.CacheOption = BitmapCacheOption.OnLoad;
                                bmp.UriSource = new Uri("pack://application:,,,/Resources/T-Icon-Quests-64.png");
                                bmp.EndInit();
                                bmp.Freeze();

                                xCoords += 25;
                                IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(xCoords, 4, 25, 25));
                            }
                        }
                    }
                }
            }
        }

        private static void GetUFFImage(JArray Properties)
        {
            JToken categoryBrushToken = AssetsUtility.GetPropertyTag<JToken>(Properties, "CategoryBrush");
            if (categoryBrushToken != null)
            {
                JArray categoryBrushProperties = categoryBrushToken["struct_type"]["properties"].Value<JArray>();
                if (categoryBrushProperties != null)
                {
                    JToken BrushXXSToken = AssetsUtility.GetPropertyTag<JToken>(categoryBrushProperties, "Brush_XXS");
                    if (BrushXXSToken != null)
                    {
                        JArray brushXXSProperties = BrushXXSToken["struct_type"]["properties"].Value<JArray>();
                        if (brushXXSProperties != null)
                        {
                            JToken resourceObjectToken = AssetsUtility.GetPropertyTagOuterImport<JToken>(brushXXSProperties, "ResourceObject");
                            if (resourceObjectToken != null)
                            {
                                string texturePath = FoldersUtility.FixFortnitePath(resourceObjectToken.Value<string>());
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

                                        xCoords += 25;
                                        IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(xCoords, 4, 25, 25));
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