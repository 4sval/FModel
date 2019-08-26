using csharp_wick;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Collections.Generic;

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
        private static string[] CosmeticUff { get; set; }
        private static string HeroType { get; set; }
        private static string DefenderType { get; set; }
        private static string MinToMax { get; set; }
        private static JArray cosmeticsSetsArray { get; set; }
        private static JArray weaponsStatsArray { get; set; }
        private static JArray tertiaryCategoriesArray { get; set; }
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
            CosmeticUff = null;
            HeroType = "";
            DefenderType = "";
            MinToMax = "";

            JToken shortDescription = theItem["ShortDescription"];
            if (shortDescription != null)
            {
                JToken key = shortDescription["key"];
                JToken sourceString = shortDescription["source_string"];
                ShortDescription = SearchResource.getTextByKey(key != null ? key.Value<string>() : "", sourceString != null ? sourceString.Value<string>() : "");
            }

            if (theItem["export_type"] != null && theItem["export_type"].Value<string>().Equals("AthenaItemWrapDefinition"))
                ShortDescription = SearchResource.getTextByKey("ItemWrapShortDescription", "Wrap", "Fort.Cosmetics");

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

                    IEnumerable<JToken> userFacingFlags = gameplayTagsArray.Children<JToken>().Where(x => x.ToString().StartsWith("Cosmetics.UserFacingFlags."));
                    if (userFacingFlags != null)
                    {
                        CosmeticUff = new string[userFacingFlags.Count()];
                        for (int i = 0; i < CosmeticUff.Length; i++)
                        {
                            CosmeticUff[i] = userFacingFlags.ElementAt(i).Value<string>().Substring("Cosmetics.UserFacingFlags.".Length);
                        }
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

        private static void DrawCosmeticUff(JToken theItem, Graphics myGraphic)
        {
            if (tertiaryCategoriesArray == null)
            {
                string extractedCosmeticsSetsPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary["ItemCategories"], "ItemCategories");
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
                                JToken tertiaryCategories = AssetData[0]["TertiaryCategories"];
                                if (tertiaryCategories != null)
                                {
                                    tertiaryCategoriesArray = tertiaryCategories.Value<JArray>();
                                    DrawCosmeticUffFromArray(theItem, myGraphic);
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
            else { DrawCosmeticUffFromArray(theItem, myGraphic); }
        }
        /// <summary>
        /// search for a known Cosmetics.UserFacingFlags, if found draw the uff icon
        /// Cosmetics.UserFacingFlags icons are basically the style icon or the animated/reactive/traversal icon
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="myGraphic"></param>
        private static void DrawCosmeticUffFromArray(JToken theItem, Graphics myGraphic)
        {
            if (CosmeticUff != null)
            {
                int xCoord = 6;
                for (int x = 0; x < tertiaryCategoriesArray.Count; x++)
                {
                    JToken categoryName = tertiaryCategoriesArray[x]["CategoryName"];
                    if (categoryName != null)
                    {
                        JToken text = categoryName["source_string"];
                        if (text != null)
                        {
                            if (CosmeticUff.Any(target => target.Contains("Animated")) && string.Equals(text.Value<string>(), "Animated"))
                            {
                                Image imageLogo = getUffFromBrush(x);
                                drawImageLogo(myGraphic, imageLogo, xCoord);
                                xCoord += 28;
                            }
                            else if (CosmeticUff.Any(target => target.Contains("HasVariants")) && string.Equals(text.Value<string>(), "Unlockable Styles"))
                            {
                                Image imageLogo = getUffFromBrush(x);
                                drawImageLogo(myGraphic, imageLogo, xCoord);
                                xCoord += 28;
                            }
                            else if (CosmeticUff.Any(target => target.Contains("Reactive")) && string.Equals(text.Value<string>(), "Reactive"))
                            {
                                Image imageLogo = getUffFromBrush(x);
                                drawImageLogo(myGraphic, imageLogo, xCoord);
                                xCoord += 28;
                            }
                            else if (CosmeticUff.Any(target => target.Contains("Traversal")) && string.Equals(text.Value<string>(), "Traversal"))
                            {
                                Image imageLogo = getUffFromBrush(x);
                                drawImageLogo(myGraphic, imageLogo, xCoord);
                                xCoord += 28;
                            }
                            else if (CosmeticUff.Any(target => target.Contains("BuiltInEmote")) && string.Equals(text.Value<string>(), "Built-in"))
                            {
                                Image imageLogo = getUffFromBrush(x);
                                drawImageLogo(myGraphic, imageLogo, xCoord);
                                xCoord += 28;
                            }
                            else if (CosmeticUff.Any(target => target.Contains("HasUpgradeQuests")) && string.Equals(text.Value<string>(), "Unlockable Styles") && !theItem["export_type"].Value<string>().Equals("AthenaPetCarrierItemDefinition"))
                            {
                                Image imageLogo = Resources.Quests64;
                                drawImageLogo(myGraphic, imageLogo, xCoord);
                                xCoord += 28;
                            }
                            else if (CosmeticUff.Any(target => target.Contains("HasUpgradeQuests")) && string.Equals(text.Value<string>(), "Unlockable Styles") && theItem["export_type"].Value<string>().Equals("AthenaPetCarrierItemDefinition"))
                            {
                                Image imageLogo = Resources.Pets64;
                                drawImageLogo(myGraphic, imageLogo, xCoord);
                                xCoord += 28;
                            }
                        }
                    }
                }
            }
        }
        private static Image getUffFromBrush(int index)
        {
            JToken categoryBrush = tertiaryCategoriesArray[index]["CategoryBrush"];
            if (categoryBrush != null)
            {
                JToken brush_XXS = categoryBrush["Brush_XXS"];
                if (brush_XXS != null)
                {
                    JToken resourceObject = brush_XXS["ResourceObject"];
                    if (resourceObject != null)
                    {
                        string texture = JohnWick.AssetToTexture2D(resourceObject.Value<string>());
                        if (!string.IsNullOrEmpty(texture))
                        {
                            return Image.FromFile(texture);
                        }
                        else { return null; }
                    }
                    else { return null; }
                }
                else { return null; }
            }
            else { return null; }
        }
        private static void drawImageLogo(Graphics myGraphic, Image logo, int x)
        {
            if (logo != null)
            {
                Point pointCoords = new Point(x, 6);

                myGraphic.DrawImage(ImageUtilities.ResizeImage(logo, 28, 28), pointCoords);
                logo.Dispose();
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
                    //myGraphic.DrawRectangle(new Pen(new SolidBrush(Color.Red)), new Rectangle(5, 385, 512, 59));

                    Size rectSize = string.Equals(Settings.Default.rarityDesign, "Flat") ? new Rectangle(5, 405, 512, 55).Size : new Rectangle(5, 395, 512, 49).Size;
                    Point textPoint = string.Equals(Settings.Default.rarityDesign, "Flat") ? new Point(522, 405) : new Point(522 / 2, 395);
                    if (string.Equals(Settings.Default.rarityDesign, "Minimalist"))
                    {
                        rectSize = string.Equals(Settings.Default.rarityDesign, "Flat") ? new Rectangle(5, 395, 512, 55).Size : string.Equals(Settings.Default.IconLanguage, "Russian") ? new Rectangle(5, 385, 512, 69).Size : new Rectangle(5, 385, 512, 59).Size;
                        textPoint = string.Equals(Settings.Default.rarityDesign, "Flat") ? new Point(522, 405) : string.Equals(Settings.Default.IconLanguage, "Russian") ? new Point(522 / 2, 400) : new Point(522 / 2, 408);
                    }
                    else if (Settings.Default.IconLanguage == "Russian")
                    {
                        rectSize = string.Equals(Settings.Default.rarityDesign, "Flat") ? new Rectangle(5, 395, 512, 55).Size : new Rectangle(5, 385, 512, 59).Size;
                        textPoint = string.Equals(Settings.Default.rarityDesign, "Flat") ? new Point(522, 405) : new Point(522 / 2, 390);
                    }

                    string text = SearchResource.getTextByKey(key.Value<string>(), sourceString.Value<string>());

                    float size = 35;
                    if (string.Equals(Settings.Default.rarityDesign, "Minimalist"))
                    {
                        size = string.Equals(Settings.Default.IconLanguage, "Russian") ? 55 : 45;
                    }
                    else if (string.Equals(Settings.Default.IconLanguage, "Russian"))
                    {
                        size = 45;
                    }

                    Font goodFont = FontUtilities.FindFont(
                        myGraphic,
                        Settings.Default.IconLanguage == "Russian" || string.Equals(Settings.Default.rarityDesign, "Minimalist") ? text.ToUpper() : text,
                        rectSize,
                        new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : Settings.Default.IconLanguage == "Russian" || Settings.Default.IconLanguage == "Chinese (S)" ? FontUtilities.pfc.Families[1] : FontUtilities.pfc.Families[0], size)
                        );

                    myGraphic.DrawString(
                        Settings.Default.IconLanguage == "Russian" || string.Equals(Settings.Default.rarityDesign, "Minimalist") ? text.ToUpper() : text,
                        goodFont,
                        new SolidBrush(Color.White),
                        textPoint,
                        string.Equals(Settings.Default.rarityDesign, "Flat") ? FontUtilities.rightString : FontUtilities.centeredString
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
                        //myGraphic.DrawRectangle(new Pen(new SolidBrush(Color.Pink)), new Rectangle(5, 455, 512, 62));

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
                        new Font("Arial", string.Equals(Settings.Default.rarityDesign, "Flat") ? 9 : string.Equals(Settings.Default.rarityDesign, "Minimalist") ? 12 : 10),
                        new SolidBrush(Color.White),
                        new RectangleF(5, string.Equals(Settings.Default.rarityDesign, "Flat") || string.Equals(Settings.Default.rarityDesign, "Minimalist") ? 455 : 441, 512, string.Equals(Settings.Default.rarityDesign, "Flat") ? 42 : string.Equals(Settings.Default.rarityDesign, "Minimalist") ? 62 : 49),
                        string.Equals(Settings.Default.rarityDesign, "Flat") ? FontUtilities.rightString : FontUtilities.centeredStringLine
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
                if (!Settings.Default.IconLanguage.Equals("English"))
                {
                    string translatedName = SearchResource.getTextByKey(setToken["DisplayName"]["key"].Value<string>(), setToken["DisplayName"]["source_string"].Value<string>(), setToken["DisplayName"]["namespace"].Value<string>());

                    return string.Format(SearchResource.getTextByKey("CosmeticItemDescription_SetMembership_NotRich", setToken["DisplayName"]["source_string"].Value<string>(), "Fort.Cosmetics"), translatedName);
                }
                else
                    return string.Format("\nPart of the {0} set.", setToken["DisplayName"]["source_string"].Value<string>());
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
            if (!string.Equals(Settings.Default.rarityDesign, "Minimalist"))
            {
                myGraphic.DrawString(text, new Font(FontUtilities.pfc.Families[0], 11), new SolidBrush(Color.White), new Point(522 - 5, 503), FontUtilities.rightString);
            }
        }

        /// <summary>
        /// draw text at bottom left
        /// </summary>
        /// <param name="text"></param>
        /// <param name="myGraphic"></param>
        private static void DrawToLeft(string text, Graphics myGraphic)
        {
            if (!string.Equals(Settings.Default.rarityDesign, "Minimalist"))
            {
                myGraphic.DrawString(text, new Font(Settings.Default.IconLanguage == "Russian" || Settings.Default.IconLanguage == "Chinese (S)" ? FontUtilities.pfc.Families[1] : FontUtilities.pfc.Families[0], 11), new SolidBrush(Color.White), new Point(5, Settings.Default.IconLanguage == "Russian" || Settings.Default.IconLanguage == "Chinese (S)" ? 500 : 503));
            }
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
