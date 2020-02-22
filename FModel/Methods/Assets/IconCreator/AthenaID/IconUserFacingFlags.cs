using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets.IconCreator.AthenaID
{
    static class IconUserFacingFlags
    {
        private static JArray TertiaryCategoriesDataArray { get; set; }
        private static JArray SecondaryCategoriesDataArray { get; set; }
        public static int xCoords = 4 - 25;
        private const string PET_CUSTOM_ICON = "pack://application:,,,/Resources/T-Icon-Pets-64.png";
        private const string QUEST_CUSTOM_ICON = "pack://application:,,,/Resources/T-Icon-Quests-64.png";

        public static void DrawUserFacingFlag(JToken uFF)
        {
            if (TertiaryCategoriesDataArray == null)
            {
                string jsonData = AssetsUtility.GetAssetJsonDataByPath("/FortniteGame/Content/Items/ItemCategories", true);
                if (jsonData != null && AssetsUtility.IsValidJson(jsonData))
                {
                    dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                    JArray AssetArray = JArray.FromObject(AssetData);
                    JToken tertiaryCategoriesToken = AssetsUtility.GetPropertyTag<JToken>(AssetArray[0]["properties"].Value<JArray>(), "TertiaryCategories");
                    if (tertiaryCategoriesToken != null)
                    {
                        TertiaryCategoriesDataArray = tertiaryCategoriesToken["data"].Value<JArray>();

                        string uFFTargeted = uFF.Value<string>().Substring("Cosmetics.UserFacingFlags.".Length);
                        SearchUserFacingFlag(uFFTargeted);
                    }
                }
            }
            else
            {
                string uFFTargeted = uFF.Value<string>().Substring("Cosmetics.UserFacingFlags.".Length);
                SearchUserFacingFlag(uFFTargeted);
            }
        }

        public static void DrawHeroFacingFlag(JToken uFF)
        {
            if (SecondaryCategoriesDataArray == null)
            {
                string jsonData = AssetsUtility.GetAssetJsonDataByPath("/FortniteGame/Content/Items/ItemCategories", true);
                if (jsonData != null && AssetsUtility.IsValidJson(jsonData))
                {
                    dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                    JArray AssetArray = JArray.FromObject(AssetData);
                    JToken secondaryCategoriesToken = AssetsUtility.GetPropertyTag<JToken>(AssetArray[0]["properties"].Value<JArray>(), "SecondaryCategories");
                    if (secondaryCategoriesToken != null)
                    {
                        SecondaryCategoriesDataArray = secondaryCategoriesToken["data"].Value<JArray>();

                        string uFFTargeted = uFF.Value<string>().Substring("Unlocks.Class.".Length);
                        SearchHeroFacingFlag(uFFTargeted);
                    }
                }
            }
            else
            {
                string uFFTargeted = uFF.Value<string>().Substring("Unlocks.Class.".Length);
                SearchHeroFacingFlag(uFFTargeted);
            }
        }

        public static void DrawWeaponFacingFlag(JToken uFF)
        {
            if (SecondaryCategoriesDataArray == null)
            {
                string jsonData = AssetsUtility.GetAssetJsonDataByPath("/FortniteGame/Content/Items/ItemCategories", true);
                if (jsonData != null && AssetsUtility.IsValidJson(jsonData))
                {
                    dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                    JArray AssetArray = JArray.FromObject(AssetData);
                    JToken secondaryCategoriesToken = AssetsUtility.GetPropertyTag<JToken>(AssetArray[0]["properties"].Value<JArray>(), "SecondaryCategories");
                    if (secondaryCategoriesToken != null)
                    {
                        SecondaryCategoriesDataArray = secondaryCategoriesToken["data"].Value<JArray>();

                        string uFFTargeted = uFF.Value<string>();
                        SearchWeaponFacingFlag(uFFTargeted);
                    }
                }
            }
            else
            {
                string uFFTargeted = uFF.Value<string>();
                SearchWeaponFacingFlag(uFFTargeted);
            }
        }

        private static void SearchWeaponFacingFlag(string uFFTarget)
        {
            foreach (JToken data in SecondaryCategoriesDataArray)
            {
                JArray propertiesArray = data["struct_type"]["properties"].Value<JArray>();
                if (propertiesArray != null)
                {
                    JArray wTagsArray = AssetsUtility.GetPropertyTagStruct<JArray>(propertiesArray, "TagContainer", "gameplay_tags");
                    if (wTagsArray != null)
                    {
                        if (wTagsArray.FirstOrDefault(i => i.Value<string>().ToLowerInvariant().Contains(uFFTarget.ToLowerInvariant())) != null)
                            GetUFFImage(propertiesArray);
                    }
                }
            }
        }

        private static void SearchHeroFacingFlag(string uFFTarget)
        {
            foreach (JToken data in SecondaryCategoriesDataArray)
            {
                JArray propertiesArray = data["struct_type"]["properties"].Value<JArray>();
                if (propertiesArray != null)
                {
                    JToken categoryNameToken = AssetsUtility.GetPropertyTagText<JToken>(propertiesArray, "CategoryName", "source_string");
                    if (categoryNameToken != null)
                    {
                        if (uFFTarget.Equals("Commando") && categoryNameToken.Value<string>() == "Soldier" ||
                            categoryNameToken.Value<string>().Equals(uFFTarget, StringComparison.InvariantCultureIgnoreCase))
                        {
                            GetUFFImage(propertiesArray);
                        }
                    }
                }
            }
        }

        private static void SearchUserFacingFlag(string uFFTarget)
        {
            foreach (JToken data in TertiaryCategoriesDataArray)
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
                            uFFTarget.Contains("BuiltInEmote") && categoryNameToken.Value<string>() == "Built-in" ||
                            uFFTarget.Contains("Synced") && categoryNameToken.Value<string>() == "Synced" || 
                            uFFTarget.Contains("GearUp") && categoryNameToken.Value<string>() == "Forged" ||
                            uFFTarget.Contains("Enlightened") && categoryNameToken.Value<string>() == "Enlightened")
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
                                bmp.UriSource = new Uri(PET_CUSTOM_ICON);
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
                                bmp.UriSource = new Uri(QUEST_CUSTOM_ICON);
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
