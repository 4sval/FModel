using csharp_wick;
using FModel.Parser.Featured;
using FModel.Parser.Items;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

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
        public static void GetItemIcon(ItemsIdParser theItem, bool featured = false)
        {
            if (!featured)
            {
                Checking.WasFeatured = false;
                SearchAthIteDefIcon(theItem);
            }
            if (featured)
            {
                if (theItem.DisplayAssetPath != null && theItem.DisplayAssetPath.AssetPathName.Contains("/Game/Catalog/DisplayAssets/"))
                {
                    string catalogName = theItem.DisplayAssetPath.AssetPathName;
                    SearchFeaturedIcon(theItem, catalogName.Substring(catalogName.LastIndexOf('.') + 1));
                }
                else if (theItem.DisplayAssetPath == null)
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
        public static void SearchAthIteDefIcon(ItemsIdParser theItem)
        {
            if (theItem.HeroDefinition != null)
            {
                string heroFilePath;
                if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                    heroFilePath = JohnWick.ExtractAsset(ThePak.CurrentUsedPak, theItem.HeroDefinition);
                else
                    heroFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[theItem.HeroDefinition], theItem.HeroDefinition);

                if (heroFilePath != null)
                {
                    if (heroFilePath.Contains(".uasset") || heroFilePath.Contains(".uexp") || heroFilePath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(heroFilePath.Substring(0, heroFilePath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                string parsedJson = JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString();
                                var itemId = ItemsIdParser.FromJson(parsedJson);
                                for (int i = 0; i < itemId.Length; i++)
                                {
                                    if (itemId[i].LargePreviewImage != null)
                                    {
                                        string textureFile = Path.GetFileName(itemId[i].LargePreviewImage.AssetPathName)
                                            ?.Substring(0,
                                                Path.GetFileName(itemId[i].LargePreviewImage.AssetPathName)
                                                    .LastIndexOf('.'));


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
            else if (theItem.WeaponDefinition != null)
            {
                //MANUAL FIX
                if (theItem.WeaponDefinition == "WID_Harvest_Pickaxe_NutCracker")
                {
                    theItem.WeaponDefinition = "WID_Harvest_Pickaxe_Nutcracker";
                }
                if (theItem.WeaponDefinition == "WID_Harvest_Pickaxe_Wukong")
                {
                    theItem.WeaponDefinition = "WID_Harvest_Pickaxe_WuKong";
                }

                string weaponFilePath;
                if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                {
                    weaponFilePath = JohnWick.ExtractAsset(ThePak.CurrentUsedPak, theItem.WeaponDefinition);
                }
                else
                {
                    weaponFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[theItem.WeaponDefinition], theItem.WeaponDefinition);
                }

                if (weaponFilePath != null)
                {
                    if (weaponFilePath.Contains(".uasset") || weaponFilePath.Contains(".uexp") || weaponFilePath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(weaponFilePath.Substring(0, weaponFilePath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                string parsedJson = JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString();
                                var itemId = ItemsIdParser.FromJson(parsedJson);
                                for (int i = 0; i < itemId.Length; i++)
                                {
                                    if (itemId[i].LargePreviewImage != null)
                                    {
                                        string textureFile = Path.GetFileName(itemId[i].LargePreviewImage.AssetPathName)
                                            ?.Substring(0,
                                                Path.GetFileName(itemId[i].LargePreviewImage.AssetPathName)
                                                    .LastIndexOf('.'));

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
        private static void SearchLargeSmallIcon(ItemsIdParser theItem)
        {
            if (theItem.LargePreviewImage != null)
            {
                string textureFile = Path.GetFileName(theItem.LargePreviewImage.AssetPathName)?.Substring(0,
                    Path.GetFileName(theItem.LargePreviewImage.AssetPathName).LastIndexOf('.'));

                ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
            }
            else if (theItem.SmallPreviewImage != null)
            {
                string textureFile = Path.GetFileName(theItem.SmallPreviewImage.AssetPathName)?.Substring(0,
                    Path.GetFileName(theItem.SmallPreviewImage.AssetPathName).LastIndexOf('.'));

                ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
            }
        }

        /// <summary>
        /// thank to epic, this is needed
        /// do not load featured icon for these files
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="catName"></param>
        public static void SearchFeaturedIcon(ItemsIdParser theItem, string catName)
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
                case "DA_Featured_EID_Dunk":
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
        private static void GetFeaturedItemIcon(ItemsIdParser theItem, string catName)
        {
            ThePak.CurrentUsedItem = catName;

            string catalogFilePath;
            try
            {
                if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                {
                    catalogFilePath = JohnWick.ExtractAsset(ThePak.CurrentUsedPak, catName);
                }
                else
                {
                    catalogFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[catName], catName);
                }

                if (catalogFilePath != null)
                {
                    Checking.WasFeatured = true;
                    if (catalogFilePath.Contains(".uasset") || catalogFilePath.Contains(".uexp") || catalogFilePath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(catalogFilePath.Substring(0, catalogFilePath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                string parsedJson = JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString();
                                var featuredId = FeaturedParser.FromJson(parsedJson);
                                for (int i = 0; i < featuredId.Length; i++)
                                {
                                    switch (catName)
                                    {
                                        case "DA_Featured_Glider_ID_070_DarkViking":
                                        case "DA_Featured_CID_319_Athena_Commando_F_Nautilus":
                                            if (featuredId[i].TileImage != null)
                                            {
                                                string textureFile = featuredId[i].TileImage.ResourceObject;
                                                ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                            }
                                            break;
                                        default:
                                            if (featuredId[i].DetailsImage != null)
                                            {
                                                string textureFile = featuredId[i].DetailsImage.ResourceObject;
                                                ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                            }
                                            break;
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
            catch (KeyNotFoundException)
            {
                GetItemIcon(theItem);
            }
        }

        /// <summary>
        /// This is only triggered if ThePak.CurrentUsedItem is a weapon id, it's to display the bullet type
        /// extract, serialize and parse the ammoFile, search a Large or Small icon, display this icon at the top left of the rarity image
        /// </summary>
        /// <param name="ammoFile"></param>
        /// <param name="toDrawOn"></param>
        public static void GetAmmoData(string ammoFile, Graphics toDrawOn)
        {
            string ammoFilePath;
            if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
            {
                ammoFilePath = JohnWick.ExtractAsset(ThePak.CurrentUsedPak, ammoFile.Substring(ammoFile.LastIndexOf('.') + 1));
            }
            else
            {
                ammoFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[ammoFile.Substring(ammoFile.LastIndexOf('.') + 1)], ammoFile.Substring(ammoFile.LastIndexOf('.') + 1));
            }

            if (ammoFilePath != null)
            {
                if (ammoFilePath.Contains(".uasset") || ammoFilePath.Contains(".uexp") || ammoFilePath.Contains(".ubulk"))
                {
                    JohnWick.MyAsset = new PakAsset(ammoFilePath.Substring(0, ammoFilePath.LastIndexOf('.')));
                    try
                    {
                        if (JohnWick.MyAsset.GetSerialized() != null)
                        {
                            string parsedJson = JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString();
                            var ammoId = ItemsIdParser.FromJson(parsedJson);
                            for (int i = 0; i < ammoId.Length; i++)
                            {
                                SearchLargeSmallIcon(ammoId[i]);

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
        /// Keep in mind the update mode use different settings than the normal mode, hence there's 2 if statement
        /// </summary>
        /// <param name="toDrawOn"></param>
        public static void DrawWatermark(Graphics toDrawOn)
        {
            if (Checking.UmWorking == false && (Settings.Default.isWatermark && !string.IsNullOrEmpty(Settings.Default.wFilename)))
            {
                Image watermark = Image.FromFile(Settings.Default.wFilename);

                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)Settings.Default.wOpacity / 100);

                toDrawOn.DrawImage(ImageUtilities.ResizeImage(opacityImage, Settings.Default.wSize, Settings.Default.wSize), (522 - Settings.Default.wSize) / 2, (522 - Settings.Default.wSize) / 2, Settings.Default.wSize, Settings.Default.wSize);
            }
            if (Checking.UmWorking && (Settings.Default.UMWatermark && !string.IsNullOrEmpty(Settings.Default.UMFilename)))
            {
                Image watermark = Image.FromFile(Settings.Default.UMFilename);

                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)Settings.Default.UMOpacity / 100);

                toDrawOn.DrawImage(ImageUtilities.ResizeImage(opacityImage, Settings.Default.UMSize, Settings.Default.UMSize), (522 - Settings.Default.UMSize) / 2, (522 - Settings.Default.UMSize) / 2, Settings.Default.UMSize, Settings.Default.UMSize);
            }
        }
    }
}
