using csharp_wick;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace FModel
{
    static class ItemIcon
    {
        public static string ItemIconPath { get; set; }

        /// <summary>
        /// if user doesn't want featured image, make WasFeatured false and move to SearchAthIteDefIcon
        /// else search or guess the display asset, if found move to SearchFeaturedIcon, if not found move to SearchAthIteDefIcon
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="featured"></param>
        public static void GetItemIcon(JToken theItem, bool featured = false)
        {
            if (!featured)
            {
                Checking.WasFeatured = false;
                SearchAthIteDefIcon(theItem);
            }
            else
            {
                JToken displayAssetPathToken = theItem["DisplayAssetPath"];
                if (displayAssetPathToken != null)
                {
                    JToken displayAssetPathNameToken = displayAssetPathToken["asset_path_name"];
                    if (displayAssetPathNameToken != null && displayAssetPathNameToken.Value<string>().Contains("/Game/Catalog/DisplayAssets/"))
                    {
                        string catalogName = displayAssetPathNameToken.Value<string>();
                        SearchFeaturedIcon(theItem, catalogName.Substring(catalogName.LastIndexOf('.') + 1));
                    }
                }
                else if (displayAssetPathToken == null)
                {
                    SearchFeaturedIcon(theItem, "DA_Featured_" + ThePak.CurrentUsedItem);
                }
                else
                {
                    GetItemIcon(theItem);
                }
            }
        }

        /// <summary>
        /// extract, serialize, get Large or Small image for HeroDefinition or WeaponDefinition
        /// if no HeroDefinition and WeaponDefinition move to SearchLargeSmallIcon
        /// </summary>
        /// <param name="theItem"></param>
        public static void SearchAthIteDefIcon(JToken theItem)
        {
            JToken heroDefinition = theItem["HeroDefinition"];
            JToken weaponDefinition = theItem["WeaponDefinition"];
            if (heroDefinition != null)
            {
                string heroFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[heroDefinition.Value<string>()], heroDefinition.Value<string>());
                if (heroFilePath != null)
                {
                    if (heroFilePath.Contains(".uasset") || heroFilePath.Contains(".uexp") || heroFilePath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(heroFilePath.Substring(0, heroFilePath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                JArray AssetArray = JArray.FromObject(AssetData);

                                JToken largePreviewImage = AssetArray[0]["LargePreviewImage"];
                                if (largePreviewImage != null)
                                {
                                    JToken assetPathName = largePreviewImage["asset_path_name"];
                                    if (assetPathName != null)
                                    {
                                        string textureFile = Path.GetFileName(assetPathName.Value<string>()).Substring(0, Path.GetFileName(assetPathName.Value<string>()).LastIndexOf('.'));

                                        ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                    }
                                }
                            }
                        }
                        catch (JsonSerializationException)
                        {
                            //do not crash when JsonSerialization does weird stuff
                        }
                    }
                }
            }
            else if (weaponDefinition != null)
            {
                //MANUAL FIX
                if (weaponDefinition.Value<string>().Equals("WID_Harvest_Pickaxe_NutCracker"))
                {
                    weaponDefinition = "WID_Harvest_Pickaxe_Nutcracker";
                }
                if (weaponDefinition.Value<string>().Equals("WID_Harvest_Pickaxe_Wukong"))
                {
                    weaponDefinition = "WID_Harvest_Pickaxe_WuKong";
                }

                string weaponFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[weaponDefinition.Value<string>()], weaponDefinition.Value<string>());
                if (weaponFilePath != null)
                {
                    if (weaponFilePath.Contains(".uasset") || weaponFilePath.Contains(".uexp") || weaponFilePath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(weaponFilePath.Substring(0, weaponFilePath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                JArray AssetArray = JArray.FromObject(AssetData);

                                JToken largePreviewImage = AssetArray[0]["LargePreviewImage"];
                                if (largePreviewImage != null)
                                {
                                    JToken assetPathName = largePreviewImage["asset_path_name"];
                                    if (assetPathName != null)
                                    {
                                        string textureFile = Path.GetFileName(assetPathName.Value<string>()).Substring(0, Path.GetFileName(assetPathName.Value<string>()).LastIndexOf('.'));

                                        ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                    }
                                }
                            }
                        }
                        catch (JsonSerializationException)
                        {
                            //do not crash when JsonSerialization does weird stuff
                        }
                    }
                }
            }
            else { SearchLargeSmallIcon(theItem); }
        }

        /// <summary>
        /// convert Large or Small image to a png image
        /// </summary>
        /// <param name="theItem"></param>
        private static void SearchLargeSmallIcon(JToken theItem)
        {
            JToken largePreviewImage = theItem["LargePreviewImage"];
            JToken smallPreviewImage = theItem["SmallPreviewImage"];
            JToken iconBrush = theItem["IconBrush"];
            if (largePreviewImage != null)
            {
                JToken assetPathName = largePreviewImage["asset_path_name"];
                if (assetPathName != null)
                {
                    string textureFile = Path.GetFileName(assetPathName.Value<string>()).Substring(0, Path.GetFileName(assetPathName.Value<string>()).LastIndexOf('.'));

                    ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                }
            }
            else if (smallPreviewImage != null)
            {
                JToken assetPathName = smallPreviewImage["asset_path_name"];
                if (assetPathName != null)
                {
                    string textureFile = Path.GetFileName(assetPathName.Value<string>()).Substring(0, Path.GetFileName(assetPathName.Value<string>()).LastIndexOf('.'));

                    ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                }
            }
            else if (iconBrush != null)
            {
                JToken resourceObject = iconBrush["ResourceObject"];
                if (resourceObject != null)
                {
                    string textureFile = resourceObject.Value<string>();

                    ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                }
            }
        }

        /// <summary>
        /// thank to epic, this is needed
        /// do not load featured icon for these files
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="catName"></param>
        public static void SearchFeaturedIcon(JToken theItem, string catName)
        {
            switch (catName)
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
                    GetItemIcon(theItem);
                    break;
                default:
                    GetFeaturedItemIcon(theItem, catName);
                    break;
            }
        }

        /// <summary>
        /// if the catalogFile is in AllpaksDictionary (is known) extract, serialize the catalogFile and parse to get and convert the featured file to a png image
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="catName"></param>
        private static void GetFeaturedItemIcon(JToken theItem, string catName)
        {
            string value = ThePak.AllpaksDictionary.Where(x => string.Equals(x.Key, catName, StringComparison.CurrentCultureIgnoreCase)).Select(d => d.Key).FirstOrDefault();
            if (value != null)
            {
                string catalogFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[value], value);
                if (!string.IsNullOrEmpty(catalogFilePath))
                {
                    Checking.WasFeatured = true;
                    if (catalogFilePath.Contains(".uasset") || catalogFilePath.Contains(".uexp") || catalogFilePath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(catalogFilePath.Substring(0, catalogFilePath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                ThePak.CurrentUsedItem = value;
                                dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                JArray AssetArray = JArray.FromObject(AssetData);

                                switch (value)
                                {
                                    case "DA_Featured_Glider_ID_070_DarkViking":
                                    case "DA_Featured_CID_319_Athena_Commando_F_Nautilus":
                                        JToken tileImage = AssetArray[0]["TileImage"];
                                        if (tileImage != null)
                                        {
                                            JToken resourceObject = tileImage["ResourceObject"];
                                            if (resourceObject != null)
                                            {
                                                string textureFile = resourceObject.Value<string>();

                                                ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                            }
                                        }
                                        break;
                                    default:
                                        JToken detailsImage = AssetArray[0]["DetailsImage"];
                                        if (detailsImage != null)
                                        {
                                            JToken resourceObject = detailsImage["ResourceObject"];
                                            if (resourceObject != null)
                                            {
                                                string textureFile = resourceObject.Value<string>();

                                                ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                            }
                                        }
                                        break;
                                }

                                // There is no featured image (as legends pack, shadow pack...)
                                if (string.IsNullOrEmpty(ItemIconPath) || ItemIconPath.Contains("Athena\\Prototype\\Textures\\"))
                                    GetItemIcon(theItem);
                            }
                        }
                        catch (JsonSerializationException)
                        {
                            //do not crash when JsonSerialization does weird stuff
                        }
                    }
                }
            }
            else
                GetItemIcon(theItem);
        }

        /// <summary>
        /// This is only triggered if ThePak.CurrentUsedItem is a weapon id, it's to display the bullet type
        /// extract, serialize and parse the ammoFile, search a Large or Small icon, display this icon at the top left of the rarity image
        /// </summary>
        /// <param name="ammoFile"></param>
        /// <param name="toDrawOn"></param>
        public static void GetAmmoData(string ammoFile, Graphics toDrawOn)
        {
            string ammoFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[ammoFile.Substring(ammoFile.LastIndexOf('.') + 1)], ammoFile.Substring(ammoFile.LastIndexOf('.') + 1));
            if (ammoFilePath != null)
            {
                if (ammoFilePath.Contains(".uasset") || ammoFilePath.Contains(".uexp") || ammoFilePath.Contains(".ubulk"))
                {
                    JohnWick.MyAsset = new PakAsset(ammoFilePath.Substring(0, ammoFilePath.LastIndexOf('.')));
                    try
                    {
                        if (JohnWick.MyAsset.GetSerialized() != null)
                        {
                            dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                            JArray AssetArray = JArray.FromObject(AssetData);

                            SearchLargeSmallIcon(AssetArray[0]);

                            if (File.Exists(ItemIconPath))
                            {
                                Image itemIcon;
                                using (var bmpTemp = new Bitmap(ItemIconPath))
                                {
                                    itemIcon = new Bitmap(bmpTemp);
                                }
                                toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 64, 64), new Point(6, 6));
                            }
                            else
                            {
                                Image itemIcon = Resources.unknown512;
                                toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 64, 64), new Point(6, 6));
                            }
                        }
                    }
                    catch (JsonSerializationException)
                    {
                        //do not crash when JsonSerialization does weird stuff
                    }
                }
            }
        }

        /// <summary>
        /// Draw a watermark on an Item Icon
        /// Keep in mind the update mode use different settings than the normal mode, hence there's 2 if statements
        /// </summary>
        /// <param name="toDrawOn"></param>
        public static void DrawWatermark(Graphics toDrawOn)
        {
            if (!Checking.UmWorking && (Settings.Default.isWatermark && !string.IsNullOrEmpty(Settings.Default.wFilename)))
            {
                Image watermark = Image.FromFile(Settings.Default.wFilename);
                if (watermark != null)
                {
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)Settings.Default.wOpacity / 100);
                    toDrawOn.DrawImage(ImageUtilities.ResizeImage(opacityImage, Settings.Default.wSize, Settings.Default.wSize), (522 - Settings.Default.wSize) / 2, (522 - Settings.Default.wSize) / 2, Settings.Default.wSize, Settings.Default.wSize);
                }
            }

            if (Checking.UmWorking && (Settings.Default.UMWatermark && !string.IsNullOrEmpty(Settings.Default.UMFilename)))
            {
                Image watermark = Image.FromFile(Settings.Default.UMFilename);
                if (watermark != null)
                {
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)Settings.Default.UMOpacity / 100);
                    toDrawOn.DrawImage(ImageUtilities.ResizeImage(opacityImage, Settings.Default.UMSize, Settings.Default.UMSize), (522 - Settings.Default.UMSize) / 2, (522 - Settings.Default.UMSize) / 2, Settings.Default.UMSize, Settings.Default.UMSize);
                }
            }
        }
    }
}
