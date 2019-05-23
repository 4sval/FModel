using csharp_wick;
using FModel.Parser.Featured;
using FModel.Parser.Items;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.IO;

namespace FModel
{
    class ItemIcon
    {
        public static string ItemIconPath { get; set; }

        public static void GetItemIcon(ItemsIdParser theItem, bool featured = false)
        {
            if (featured == false)
            {
                Checking.WasFeatured = false;
                SearchAthIteDefIcon(theItem);
            }
            if (featured)
            {
                if (theItem.DisplayAssetPath != null && theItem.DisplayAssetPath.AssetPathName.Contains("/Game/Catalog/DisplayAssets/"))
                {
                    string catalogName = theItem.DisplayAssetPath.AssetPathName;
                    SearchFeaturedIcon(theItem, catalogName);
                }
                else if (theItem.DisplayAssetPath == null)
                {
                    SearchFeaturedIcon(theItem, "DA_Featured_" + ThePak.CurrentUsedItem, true);
                }
                else
                {
                    GetItemIcon(theItem);
                }
            }
        }
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
                        catch (JsonSerializationException) { }
                    }
                }
            }
            else if (theItem.WeaponDefinition != null)
            {
                //MANUAL FIX
                if (theItem.WeaponDefinition == "WID_Harvest_Pickaxe_NutCracker")
                    theItem.WeaponDefinition = "WID_Harvest_Pickaxe_Nutcracker";
                if (theItem.WeaponDefinition == "WID_Harvest_Pickaxe_Wukong")
                    theItem.WeaponDefinition = "WID_Harvest_Pickaxe_WuKong";

                string weaponFilePath;
                if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                    weaponFilePath = JohnWick.ExtractAsset(ThePak.CurrentUsedPak, theItem.WeaponDefinition);
                else
                    weaponFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[theItem.WeaponDefinition], theItem.WeaponDefinition);

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
                        catch (JsonSerializationException) { }
                    }
                }
            }
            else
                SearchLargeSmallIcon(theItem);
        }
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
        public static void SearchFeaturedIcon(ItemsIdParser theItem, string catName, bool manualSearch = false)
        {
            if (manualSearch == false)
            {
                ThePak.CurrentUsedItem = catName.Substring(catName.LastIndexOf('.') + 1);

                if (ThePak.CurrentUsedItem == "DA_Featured_Glider_ID_141_AshtonBoardwalk")
                    GetItemIcon(theItem);
                else
                {
                    string catalogFilePath;
                    if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                        catalogFilePath = JohnWick.ExtractAsset(ThePak.CurrentUsedPak, catName.Substring(catName.LastIndexOf('.') + 1));
                    else
                        catalogFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[catName.Substring(catName.LastIndexOf('.') + 1)], catName.Substring(catName.LastIndexOf('.') + 1));

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
                                        //Thanks EPIC
                                        if (ThePak.CurrentUsedItem == "DA_Featured_CID_319_Athena_Commando_F_Nautilus")
                                        {
                                            if (featuredId[i].TileImage != null)
                                            {
                                                string textureFile = featuredId[i].TileImage.ResourceObject;
                                                ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                            }
                                        }
                                        else
                                        {
                                            if (featuredId[i].DetailsImage != null)
                                            {
                                                string textureFile = featuredId[i].DetailsImage.ResourceObject;
                                                ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (JsonSerializationException) { }
                        }
                    }
                }
            }
            if (manualSearch)
            {
                //Thanks EPIC
                if (catName == "DA_Featured_Glider_ID_015_Brite" ||
                    catName == "DA_Featured_Glider_ID_016_Tactical" ||
                    catName == "DA_Featured_Glider_ID_017_Assassin" ||
                    catName == "DA_Featured_Pickaxe_ID_027_Scavenger" ||
                    catName == "DA_Featured_Pickaxe_ID_028_Space" ||
                    catName == "DA_Featured_Pickaxe_ID_029_Assassin" ||
                    catName == "DA_Featured_EID_Dunk")
                    GetItemIcon(theItem);
                else if (ThePak.AllpaksDictionary.ContainsKey(catName))
                {
                    ThePak.CurrentUsedItem = catName;

                    string catalogFilePath;
                    if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                        catalogFilePath = JohnWick.ExtractAsset(ThePak.CurrentUsedPak, catName);
                    else
                        catalogFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[catName], catName);

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
                                        //Thanks EPIC
                                        if (ThePak.CurrentUsedItem == "DA_Featured_Glider_ID_070_DarkViking")
                                        {
                                            if (featuredId[i].TileImage != null)
                                            {
                                                string textureFile = featuredId[i].TileImage.ResourceObject;
                                                ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                            }
                                        }
                                        else
                                        {
                                            if (featuredId[i].DetailsImage != null)
                                            {
                                                string textureFile = featuredId[i].DetailsImage.ResourceObject;
                                                ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (JsonSerializationException) { }
                        }
                    }
                }
                else
                    GetItemIcon(theItem);
            }
        }
        public static void GetAmmoData(string ammoFile, Graphics toDrawOn)
        {
            string ammoFilePath;
            if (ThePak.CurrentUsedPakGuid != null && ThePak.CurrentUsedPakGuid != "0-0-0-0")
                ammoFilePath = JohnWick.ExtractAsset(ThePak.CurrentUsedPak, ammoFile.Substring(ammoFile.LastIndexOf('.') + 1));
            else
                ammoFilePath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[ammoFile.Substring(ammoFile.LastIndexOf('.') + 1)], ammoFile.Substring(ammoFile.LastIndexOf('.') + 1));

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
                                    toDrawOn.DrawImage(Forms.Settings.ResizeImage(itemIcon, 64, 64), new Point(6, 6));
                                }
                                else
                                {
                                    Image itemIcon = Resources.unknown512;
                                    toDrawOn.DrawImage(Forms.Settings.ResizeImage(itemIcon, 64, 64), new Point(6, 6));
                                }
                            }
                        }
                    }
                    catch (JsonSerializationException) { }
                }
            }
        }
    }
}
