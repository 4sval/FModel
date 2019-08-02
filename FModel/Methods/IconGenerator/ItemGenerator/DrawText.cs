using csharp_wick;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Linq;
using System.IO;

namespace FModel
{
    static class DrawText
    {
        private static string CosmeticSource { get; set; }
        private static string CosmeticSet { get; set; }
        private static string ShortDescription { get; set; }
        private static string CosmeticId { get; set; }
        private static string MaxStackSize { get; set; }
        private static string ItemAction { get; set; }
        private static string WeaponDataTable { get; set; }
        private static string WeaponRowName { get; set; }
        private static string CosmeticUff { get; set; }
        private static string HeroType { get; set; }
        private static string DefenderType { get; set; }
        private static string MinToMax { get; set; }
        private static JArray cosmeticsSetsArray { get; set; }
        private static JArray weaponsStatsArray { get; set; }
        private static string weaponStatsFilename { get; set; }

        public static void DrawTexts(JToken theItem, Graphics myGraphic, string mode)
        {
            SetTexts(theItem);

            DrawDisplayName(theItem, myGraphic);
            DrawDescription(theItem, myGraphic);

            switch (mode)
            {
                case "athIteDef":
                    DrawToLeft(ShortDescription, myGraphic);
                    DrawToRight(CosmeticSource, myGraphic);
                    break;
                case "consAndWeap":
                    DrawToRight(ItemAction, myGraphic);
                    if (Checking.ExtractedFilePath.Contains("Items\\Consumables\\"))
                    {
                        DrawToLeft(MaxStackSize, myGraphic);
                    }
                    break;
                case "variant":
                    DrawToLeft(ShortDescription, myGraphic);
                    DrawToRight(CosmeticId, myGraphic);
                    break;
                case "stwHeroes":
                    DrawToRight(HeroType, myGraphic);
                    DrawPower(myGraphic);
                    break;
                case "stwDefenders":
                    DrawToRight(DefenderType, myGraphic);
                    DrawPower(myGraphic);
                    break;
            }

            JToken exportToken = theItem["export_type"];
            if (exportToken != null && exportToken.Value<string>().Equals("AthenaItemWrapDefinition") && Checking.WasFeatured && ItemIcon.ItemIconPath.Contains("WeaponRenders"))
            {
                DrawAdditionalImage(theItem, myGraphic);
            }

            JToken ammoToken = theItem["AmmoData"];
            if (ammoToken != null)
            {
                JToken assetPathName = ammoToken["asset_path_name"];
                if (assetPathName != null && assetPathName.Value<string>().Contains("Ammo")) //TO AVOID TRIGGERING CONSUMABLES, NAME SHOULD CONTAIN "AMMO"
                {
                    ItemIcon.GetAmmoData(assetPathName.Value<string>(), myGraphic);

                    DrawWeaponStat(WeaponDataTable, WeaponRowName, myGraphic);
                }
            }

            DrawCosmeticUff(theItem, myGraphic);
        }

        /// <summary>
        /// todo: find a better way to handle errors
        /// </summary>
        /// <param name="theItem"></param>
        private static void SetTexts(JToken theItem)
        {
            CosmeticSource = "";
            CosmeticSet = "";
            ShortDescription = "";
            CosmeticId = "";
            MaxStackSize = "";
            ItemAction = "";
            WeaponDataTable = "";
            WeaponRowName = "";
            CosmeticUff = "";
            HeroType = "";
            DefenderType = "";
            MinToMax = "";

            JToken shortDescription = theItem["ShortDescription"];
            if (shortDescription != null)
            {
                JToken key = shortDescription["key"];
                JToken sourceString = shortDescription["source_string"];

                switch (Settings.Default.IconLanguage)
                {
                    case "French":
                    case "German":
                    case "Italian":
                    case "Spanish":
                    case "Spanish (LA)":
                    case "Arabic":
                    case "Japanese":
                    case "Korean":
                    case "Polish":
                    case "Portuguese (Brazil)":
                    case "Russian":
                    case "Turkish":
                    case "Chinese (S)":
                    case "Traditional Chinese":
                        if (key != null && sourceString != null)
                        {
                            ShortDescription = SearchResource.getTextByKey(key.Value<string>(), sourceString.Value<string>());
                        }
                        break;
                    default:
                        if (sourceString != null)
                        {
                            ShortDescription = sourceString.Value<string>();
                        }
                        break;
                }
            }

            JToken gameplayTags = theItem["GameplayTags"];
            if (gameplayTags != null)
            {
                JToken gameplayTagsTwo = gameplayTags["gameplay_tags"];
                if (gameplayTagsTwo != null)
                {
                    JArray gameplayTagsArray = gameplayTagsTwo.Value<JArray>();

                    JToken cosmeticSet = gameplayTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Cosmetics.Set."));
                    if (cosmeticSet != null)
                    {
                        CosmeticSet = gameplayTagsArray[gameplayTagsArray.IndexOf(cosmeticSet)].Value<string>();
                    }

                    JToken cosmeticSource = gameplayTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Cosmetics.Source."));
                    if (cosmeticSource != null)
                    {
                        CosmeticSource = cosmeticSource.Value<string>().Substring(17);
                    }

                    JToken athenaItemAction = gameplayTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Athena.ItemAction."));
                    if (athenaItemAction != null)
                    {
                        ItemAction = athenaItemAction.Value<string>().Substring(18);
                    }

                    JToken userFacingFlags = gameplayTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Cosmetics.UserFacingFlags."));
                    if (userFacingFlags != null)
                    {
                        CosmeticUff = userFacingFlags.Value<string>();
                    }

                    JToken defenderType = gameplayTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("NPC.CharacterType.Survivor.Defender."));
                    if (defenderType != null)
                    {
                        DefenderType = defenderType.Value<string>().Substring(36);
                    }
                }
            }
            
            JToken cosmeticId = theItem["cosmetic_item"];
            if (cosmeticId != null)
            {
                CosmeticId = cosmeticId.Value<string>();
            }

            JToken maxStackSize = theItem["MaxStackSize"];
            if (maxStackSize != null)
            {
                MaxStackSize = "Max Stack Size: " + maxStackSize.Value<string>();
            }

            JToken weaponStatHandle = theItem["WeaponStatHandle"];
            if (weaponStatHandle != null)
            {
                JToken dataTable = weaponStatHandle["DataTable"];
                if (dataTable != null)
                {
                    WeaponDataTable = dataTable.Value<string>();
                }

                JToken rowName = weaponStatHandle["RowName"];
                if (rowName != null)
                {
                    WeaponRowName = rowName.Value<string>();
                }
            }

            JToken attributeInitKey = theItem["AttributeInitKey"];
            if (attributeInitKey != null)
            {
                JToken attributeInitCategory = attributeInitKey["AttributeInitCategory"];
                if (attributeInitCategory != null)
                {
                    HeroType = attributeInitCategory.Value<string>();
                }
            }

            JToken minLevel = theItem["MinLevel"];
            JToken maxLevel = theItem["MaxLevel"];
            if (maxLevel != null)
            {
                MinToMax = "    0 to " + maxLevel.Value<string>();
                if (minLevel != null)
                {
                    MinToMax = "    " + minLevel.Value<string>() + " to " + maxLevel.Value<string>();
                }
            }
        }

        /// <summary>
        /// search for a known Cosmetics.UserFacingFlags, if found draw the uff icon
        /// Cosmetics.UserFacingFlags icons are basically the style icon or the animated/reactive/traversal icon
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="myGraphic"></param>
        private static void DrawCosmeticUff(JToken theItem, Graphics myGraphic)
        {
            Image imageLogo = null;
            Point pointCoords = new Point(6, 6);

            if (CosmeticUff != null)
            {
                if (CosmeticUff.Contains("Animated"))
                    imageLogo = Resources.Animated64;
                else if (CosmeticUff.Contains("HasUpgradeQuests") && !theItem["export_type"].Value<string>().Equals("AthenaPetCarrierItemDefinition"))
                    imageLogo = Resources.Quests64;
                else if (CosmeticUff.Contains("HasUpgradeQuests") && theItem["export_type"].Value<string>().Equals("AthenaPetCarrierItemDefinition"))
                    imageLogo = Resources.Pets64;
                else if (CosmeticUff.Contains("HasVariants"))
                    imageLogo = Resources.Variant64;
                else if (CosmeticUff.Contains("Reactive"))
                    imageLogo = Resources.Adaptive64;
                else if (CosmeticUff.Contains("Traversal"))
                    imageLogo = Resources.Traversal64;
            }

            if (imageLogo != null)
            {
                myGraphic.DrawImage(ImageUtilities.ResizeImage(imageLogo, 28, 28), pointCoords);
                imageLogo.Dispose();
            }
        }

        /// <summary>
        /// draw item name if exist
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="myGraphic"></param>
        private static void DrawDisplayName(JToken theItem, Graphics myGraphic)
        {
            JToken displayName = theItem["DisplayName"];
            if (displayName != null)
            {
                JToken key = displayName["key"];
                JToken sourceString = displayName["source_string"];
                if (key != null && sourceString != null)
                {
                    //myGraphic.DrawRectangle(new Pen(new SolidBrush(Color.Red)), new Rectangle(5, 405, 512, 55));

                    string text = SearchResource.getTextByKey(key.Value<string>(), sourceString.Value<string>());

                    Font goodFont = FontUtilities.FindFont(
                        myGraphic,
                        text,
                        Settings.Default.rarityNew ? new Rectangle(5, 405, 512, 55).Size : new Rectangle(5, 395, 512, 49).Size,
                        new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : Settings.Default.IconLanguage == "Russian" ? FontUtilities.pfc.Families[1] : FontUtilities.pfc.Families[0], 35)
                        );

                    myGraphic.DrawString(
                        text,
                        goodFont,
                        new SolidBrush(Color.White),
                        Settings.Default.rarityNew ? new Point(522, 405) : new Point(522 / 2, 395),
                        Settings.Default.rarityNew ? FontUtilities.rightString : FontUtilities.centeredString
                        );
                }
            }
        }

        /// <summary>
        /// draw item description if exist
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="myGraphic"></param>
        private static void DrawDescription(JToken theItem, Graphics myGraphic)
        {
            JToken description = theItem["Description"];
            if (description != null)
            {
                string descriptionText = string.Empty;

                if (theItem["export_type"].Value<string>().Equals("FortAbilityKit"))
                {
                    JArray descriptionArray = theItem["Description"].Value<JArray>();
                    foreach (JToken token in descriptionArray)
                    {
                        JToken key = token["key"];
                        JToken sourceString = token["source_string"];
                        if (key != null && sourceString != null)
                        {
                            string text = SearchResource.getTextByKey(key.Value<string>(), sourceString.Value<string>());
                            descriptionText += text;
                        }
                    }
                }
                else
                {
                    JToken key = description["key"];
                    JToken sourceString = description["source_string"];
                    if (key != null && sourceString != null)
                    {
                        //myGraphic.DrawRectangle(new Pen(new SolidBrush(Color.Pink)), new Rectangle(5, 455, 512, 42));

                        string text = SearchResource.getTextByKey(key.Value<string>(), sourceString.Value<string>());
                        descriptionText = text;

                        if (!string.IsNullOrEmpty(CosmeticSet))
                        {
                            string theSet = DrawCosmeticSet(CosmeticSet);
                            if (!string.IsNullOrEmpty(theSet))
                            {
                                descriptionText += theSet;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(descriptionText))
                {
                    myGraphic.DrawString(
                        descriptionText,
                        new Font("Arial", Settings.Default.rarityNew ? 9 : 10),
                        new SolidBrush(Color.White),
                        new RectangleF(5, Settings.Default.rarityNew ? 455 : 441, 512, Settings.Default.rarityNew ? 42 : 49),
                        Settings.Default.rarityNew ? FontUtilities.rightString : FontUtilities.centeredStringLine
                        );
                }
            }
        }

        private static string DrawCosmeticSet(string setName)
        {
            if (cosmeticsSetsArray == null)
            {
                string extractedCosmeticsSetsPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary["CosmeticSets"], "CosmeticSets");

                if (extractedCosmeticsSetsPath != null)
                {
                    if (extractedCosmeticsSetsPath.Contains(".uasset") || extractedCosmeticsSetsPath.Contains(".uexp") || extractedCosmeticsSetsPath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(extractedCosmeticsSetsPath.Substring(0, extractedCosmeticsSetsPath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                cosmeticsSetsArray = JArray.FromObject(AssetData);
                                return searchSetName(setName);
                            }
                            else { return ""; }
                        }
                        catch (JsonSerializationException)
                        {
                            return "";
                            //do not crash when JsonSerialization does weird stuff
                        }
                    }
                    else { return ""; }
                }
                else { return ""; }
            }
            else { return searchSetName(setName); }
        }
        private static string searchSetName(string setName)
        {
            JToken setToken = cosmeticsSetsArray[0][setName];
            if (setToken != null)
            {
                string toReturn = string.Empty;
                switch (Settings.Default.IconLanguage)
                {
                    case "French":
                    case "German":
                    case "Italian":
                    case "Spanish":
                    case "Spanish (LA)":
                    case "Arabic":
                    case "Japanese":
                    case "Korean":
                    case "Polish":
                    case "Portuguese (Brazil)":
                    case "Russian":
                    case "Turkish":
                    case "Chinese (S)":
                    case "Traditional Chinese":
                        string translatedName = SearchResource.getTextByKey(setToken["DisplayName"]["key"].Value<string>(), setToken["DisplayName"]["source_string"].Value<string>(), setToken["DisplayName"]["namespace"].Value<string>());

                        toReturn = string.Format(SearchResource.getTextByKey("CosmeticItemDescription_SetMembership_NotRich", setToken["DisplayName"]["source_string"].Value<string>(), "Fort.Cosmetics"), translatedName);
                        break;
                    default:
                        toReturn = string.Format("\nPart of the {0} set.", setToken["DisplayName"]["source_string"].Value<string>());
                        break;
                }
                return toReturn;
            }
            else { return ""; }
        }

        /// <summary>
        /// draw text at bottom right
        /// </summary>
        /// <param name="text"></param>
        /// <param name="myGraphic"></param>
        private static void DrawToRight(string text, Graphics myGraphic)
        {
            myGraphic.DrawString(text, new Font(FontUtilities.pfc.Families[0], 11), new SolidBrush(Color.White), new Point(522 - 5, 503), FontUtilities.rightString);
        }

        /// <summary>
        /// draw text at bottom left
        /// </summary>
        /// <param name="text"></param>
        /// <param name="myGraphic"></param>
        private static void DrawToLeft(string text, Graphics myGraphic)
        {
            myGraphic.DrawString(text, new Font(Settings.Default.IconLanguage == "Russian" ? FontUtilities.pfc.Families[1] : FontUtilities.pfc.Families[0], 11), new SolidBrush(Color.White), new Point(5, Settings.Default.IconLanguage == "Russian" ? 500 : 503));
        }

        /// <summary>
        /// this is only triggered for wraps, in case the featured (weapon render) image is drawn
        /// also draw the non featured image to make it clear it's a wrap, not a weapon
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="myGraphic"></param>
        private static void DrawAdditionalImage(JToken theItem, Graphics myGraphic)
        {
            JToken largePreviewImage = theItem["LargePreviewImage"];
            if (largePreviewImage != null)
            {
                JToken assetPathName = largePreviewImage["asset_path_name"];
                if (assetPathName != null)
                {
                    string textureFile = Path.GetFileName(assetPathName.Value<string>()).Substring(0, Path.GetFileName(assetPathName.Value<string>()).LastIndexOf('.'));

                    ItemIcon.ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                    if (File.Exists(ItemIcon.ItemIconPath))
                    {
                        Image itemIcon;
                        using (var bmpTemp = new Bitmap(ItemIcon.ItemIconPath))
                        {
                            itemIcon = new Bitmap(bmpTemp);
                        }
                        myGraphic.DrawImage(ImageUtilities.ResizeImage(itemIcon, 122, 122), new Point(275, 272));
                    }
                }
            }
        }

        /// <summary>
        /// this is only triggered for weapons
        /// draw the damage per bullet as well as the reload time
        /// </summary>
        /// <param name="weaponName"></param>
        /// <param name="myGraphic"></param>
        private static void DrawWeaponStat(string filename, string weaponName, Graphics myGraphic)
        {
            if (weaponsStatsArray == null || !weaponStatsFilename.Equals(filename))
            {
                ItemIcon.ItemIconPath = string.Empty;
                string extractedWeaponsStatPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[filename], filename);
                if (extractedWeaponsStatPath != null)
                {
                    if (extractedWeaponsStatPath.Contains(".uasset") || extractedWeaponsStatPath.Contains(".uexp") || extractedWeaponsStatPath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(extractedWeaponsStatPath.Substring(0, extractedWeaponsStatPath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                weaponsStatsArray = JArray.FromObject(AssetData);
                                weaponStatsFilename = filename;
                                loopingLol(weaponName, myGraphic);
                            }
                        }
                        catch (JsonSerializationException)
                        {
                            //do not crash when JsonSerialization does weird stuff
                        }
                    }
                }
            }
            else { loopingLol(weaponName, myGraphic); }
        }
        private static void loopingLol(string weaponName, Graphics myGraphic)
        {
            JToken weaponToken = weaponsStatsArray[0][weaponName];
            if (weaponToken != null)
            {
                JToken dmgPb = weaponToken["DmgPB"];
                if (dmgPb != null)
                {
                    Image bulletImage = Resources.dmg64;
                    myGraphic.DrawImage(ImageUtilities.ResizeImage(bulletImage, 15, 15), new Point(5, 502));
                    DrawToLeft("     " + dmgPb.Value<string>(), myGraphic); //damage per bullet
                }

                JToken clipSize = weaponToken["ClipSize"];
                if (clipSize != null)
                {
                    Image clipSizeImage = Resources.clipSize64;
                    myGraphic.DrawImage(ImageUtilities.ResizeImage(clipSizeImage, 15, 15), new Point(52, 502));
                    myGraphic.DrawString("      " + clipSize.Value<string>(), new Font(FontUtilities.pfc.Families[0], 11), new SolidBrush(Color.White), new Point(50, 503));
                }
                else { clipSize = ""; }

                JToken reloadTime = weaponToken["ReloadTime"];
                if (reloadTime != null)
                {
                    Image reload = Resources.reload64;
                    myGraphic.DrawImage(ImageUtilities.ResizeImage(reload, 15, 15), new Point(50 + (clipSize.Value<string>().Length * 7) + 47, 502)); //50=clipsize text position | for each clipsize letter we add 7 to x | 47=difference between 2 icons
                    myGraphic.DrawString(reloadTime.Value<string>() + " " + SearchResource.getTextByKey("6BA53D764BA5CC13E821D2A807A72365", "seconds"), new Font(FontUtilities.pfc.Families[0], 11), new SolidBrush(Color.White), new Point(64 + (clipSize.Value<string>().Length * 7) + 47, 503)); //64=50+icon size (-1 because that wasn't perfectly at the position i wanted)
                }

                DrawToRight(weaponName, myGraphic);
            }
        }

        /// <summary>
        /// this is only triggered for heroes and defenders
        /// draw the minimum and maximum level as well as a bolt icon
        /// </summary>
        /// <param name="myGraphic"></param>
        private static void DrawPower(Graphics myGraphic)
        {
            if (!string.IsNullOrEmpty(MinToMax))
            {
                Image bolt = Resources.LBolt64;
                myGraphic.DrawImage(ImageUtilities.ResizeImage(bolt, 15, 15), new Point(5, 501));

                DrawToLeft(MinToMax, myGraphic);
            }
        }
    }
}
