using csharp_wick;
using FModel.Parser.Items;
using FModel.Parser.LocResParser;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
        private static string WeaponRowName { get; set; }
        private static string CosmeticUff { get; set; }
        private static string HeroType { get; set; }
        private static string DefenderType { get; set; }
        private static string MinToMax { get; set; }
        private static JObject wStatsjo { get; set; }
        private static JObject cSetsjo { get; set; }

        public static void DrawTexts(ItemsIdParser theItem, Graphics myGraphic, string mode)
        {
            using (myGraphic)
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

                if (theItem.ExportType == "AthenaItemWrapDefinition" && Checking.WasFeatured && ItemIcon.ItemIconPath.Contains("WeaponRenders"))
                {
                    DrawAdditionalImage(theItem, myGraphic);
                }
                if (theItem.AmmoData != null && theItem.AmmoData.AssetPathName.Contains("Ammo")) //TO AVOID TRIGGERING CONSUMABLES, NAME SHOULD CONTAIN "AMMO"
                {
                    ItemIcon.GetAmmoData(theItem.AmmoData.AssetPathName, myGraphic);
                    DrawWeaponStat(WeaponRowName, myGraphic);
                }

                DrawCosmeticUff(theItem, myGraphic);
            }
        }

        /// <summary>
        /// todo: find a better way to handle errors
        /// </summary>
        /// <param name="theItem"></param>
        private static void SetTexts(ItemsIdParser theItem)
        {
            CosmeticSource = "";
            CosmeticSet = "";
            ShortDescription = "";
            CosmeticId = "";
            MaxStackSize = "";
            ItemAction = "";
            WeaponRowName = "";
            CosmeticUff = "";
            HeroType = "";
            DefenderType = "";
            MinToMax = "";

            try
            {
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
                        ShortDescription = theItem.ShortDescription != null ? SearchResource.getTextByKey(theItem.ShortDescription.Key, theItem.ShortDescription.SourceString) : "";
                        break;
                    default:
                        ShortDescription = theItem.ShortDescription != null ? theItem.ShortDescription.SourceString : "";
                        break;
                }
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
            try
            {
                CosmeticSet = theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.Set."))];
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
            try
            {
                CosmeticSource = theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.Source."))].Substring(17);
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
            try
            {
                CosmeticId = theItem.CosmeticItem;
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
            try
            {
                MaxStackSize = "Max Stack Size: " + theItem.MaxStackSize;
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
            try
            {
                ItemAction = theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Athena.ItemAction."))].Substring(18);
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
            try
            {
                if (theItem.WeaponStatHandle != null && theItem.WeaponStatHandle.RowName != "Harvest_Pickaxe_Athena_C_T01" && theItem.WeaponStatHandle.RowName != "Edged_Sword_Athena_C_T01")
                {
                    WeaponRowName = theItem.WeaponStatHandle.RowName;
                }
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
            try
            {
                CosmeticUff = theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("Cosmetics.UserFacingFlags."))];
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
            try
            {
                HeroType = theItem.AttributeInitKey != null ? theItem.AttributeInitKey.AttributeInitCategory : "";
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
            try
            {
                DefenderType = theItem.GameplayTags.GameplayTagsGameplayTags[Array.FindIndex(theItem.GameplayTags.GameplayTagsGameplayTags, x => x.StartsWith("NPC.CharacterType.Survivor.Defender."))].Substring(36);
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
            try
            {
                MinToMax = "   " + theItem.MinLevel + " to " + theItem.MaxLevel;
            }
            catch (Exception)
            {
                //avoid generator to stop when a string isn't found
            }
        }

        /// <summary>
        /// search for a known Cosmetics.UserFacingFlags, if found draw the uff icon
        /// Cosmetics.UserFacingFlags icons are basically the style icon or the animated/reactive/traversal icon
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="myGraphic"></param>
        private static void DrawCosmeticUff(ItemsIdParser theItem, Graphics myGraphic)
        {
            Image imageLogo = null;
            Point pointCoords = new Point(6, 6);

            if (CosmeticUff != null)
            {
                if (CosmeticUff.Contains("Animated"))
                    imageLogo = Resources.Animated64;
                else if (CosmeticUff.Contains("HasUpgradeQuests") && theItem.ExportType != "AthenaPetCarrierItemDefinition")
                    imageLogo = Resources.Quests64;
                else if (CosmeticUff.Contains("HasUpgradeQuests") && theItem.ExportType == "AthenaPetCarrierItemDefinition")
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
        private static void DrawDisplayName(ItemsIdParser theItem, Graphics myGraphic)
        {
            if (theItem.DisplayName != null)
            {
                //myGraphic.DrawRectangle(new Pen(new SolidBrush(Color.Red)), new Rectangle(5, 405, 512, 55));

                string text = SearchResource.getTextByKey(theItem.DisplayName.Key, theItem.DisplayName.SourceString);

                if (Settings.Default.rarityNew)
                {
                    Font goodFont = FontUtilities.FindFont(myGraphic, text, new Rectangle(5, 405, 512, 55).Size, new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[0], 35));
                    myGraphic.DrawString(text, goodFont, new SolidBrush(Color.White), new Point(522, 405), FontUtilities.rightString);
                }
                else
                {
                    Font goodFont = FontUtilities.FindFont(myGraphic, text, new Rectangle(5, 395, 512, 49).Size, new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[0], 35));
                    myGraphic.DrawString(text, goodFont, new SolidBrush(Color.White), new Point(522 / 2, 395), FontUtilities.centeredString);
                }
            }
        }

        /// <summary>
        /// draw item description if exist
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="myGraphic"></param>
        private static void DrawDescription(ItemsIdParser theItem, Graphics myGraphic)
        {
            if (theItem.Description != null)
            {
                //myGraphic.DrawRectangle(new Pen(new SolidBrush(Color.Pink)), new Rectangle(5, 455, 512, 42));

                string text = SearchResource.getTextByKey(theItem.Description.Key, theItem.Description.SourceString);
                if (!string.IsNullOrEmpty(CosmeticSet))
                {
                    string theSet = DrawCosmeticSet(CosmeticSet);
                    if (!string.IsNullOrEmpty(theSet))
                    {
                        text += theSet;
                    }
                }

                if (Settings.Default.rarityNew)
                {
                    myGraphic.DrawString(text, new Font("Arial", 9), new SolidBrush(Color.White), new RectangleF(5, 455, 512, 42), FontUtilities.rightString);
                }
                else
                {
                    myGraphic.DrawString(text, new Font("Arial", 10), new SolidBrush(Color.White), new RectangleF(5, 441, 512, 49), FontUtilities.centeredStringLine);
                }
            }
        }

        private static string DrawCosmeticSet(string setName)
        {
            if (cSetsjo == null)
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
                                string parsedJson = JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString().TrimStart('[').TrimEnd(']');
                                cSetsjo = JObject.Parse(parsedJson);
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
            string toReturn = "";

            JToken setToken = cSetsjo.FindTokens(setName).FirstOrDefault();
            Parser.CosmeticSetsParser.CosmeticSetsParser cSetsParsed = Parser.CosmeticSetsParser.CosmeticSetsParser.FromJson(setToken.ToString());

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
                    JToken setNameTokenLocalized = SearchResource.jo.FindTokens("CosmeticSets").FirstOrDefault();
                    string parsedJson = JToken.Parse(setNameTokenLocalized.ToString()).ToString().TrimStart('[').TrimEnd(']');
                    JToken setNameToken = JObject.Parse(parsedJson).FindTokens(cSetsParsed.DisplayName.Key).FirstOrDefault();
                    string translatedName = setNameToken == null ? cSetsParsed.DisplayName.SourceString : setNameToken.ToString();

                    JToken setDescriptionToken = SearchResource.jo.FindTokens("Fort.Cosmetics").FirstOrDefault();
                    LocResParser dTokenParsed = LocResParser.FromJson(setDescriptionToken.ToString());
                    if (dTokenParsed.CosmeticItemDescriptionSetMembershipNotRich != null)
                    {
                        toReturn = string.Format(dTokenParsed.CosmeticItemDescriptionSetMembershipNotRich, translatedName);
                    }
                    break;
                default:
                    toReturn = string.Format("\nPart of the {0} set.", cSetsParsed.DisplayName.SourceString);
                    break;
            }

            return toReturn;
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
            myGraphic.DrawString(text, new Font(FontUtilities.pfc.Families[0], 11), new SolidBrush(Color.White), new Point(5, 503));
        }

        /// <summary>
        /// this is only triggered for wraps, in case the featured (weapon render) image is drawn
        /// also draw the non featured image to make it clear it's a wrap, not a weapon
        /// </summary>
        /// <param name="theItem"></param>
        /// <param name="myGraphic"></param>
        private static void DrawAdditionalImage(ItemsIdParser theItem, Graphics myGraphic)
        {
            string wrapAddImg = theItem.LargePreviewImage.AssetPathName.Substring(theItem.LargePreviewImage.AssetPathName.LastIndexOf(".", StringComparison.Ordinal) + 1);

            ItemIcon.ItemIconPath = JohnWick.AssetToTexture2D(wrapAddImg);

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

        /// <summary>
        /// this is only triggered for weapons
        /// draw the damage per bullet as well as the reload time
        /// </summary>
        /// <param name="weaponName"></param>
        /// <param name="myGraphic"></param>
        private static void DrawWeaponStat(string weaponName, Graphics myGraphic)
        {
            if (wStatsjo == null)
            {
                ItemIcon.ItemIconPath = string.Empty;
                string extractedWeaponsStatPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary["AthenaRangedWeapons"], "AthenaRangedWeapons");
                if (extractedWeaponsStatPath != null)
                {
                    if (extractedWeaponsStatPath.Contains(".uasset") || extractedWeaponsStatPath.Contains(".uexp") || extractedWeaponsStatPath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(extractedWeaponsStatPath.Substring(0, extractedWeaponsStatPath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                string parsedJson = JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString().TrimStart('[').TrimEnd(']');
                                wStatsjo = JObject.Parse(parsedJson);
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
            foreach (JToken token in wStatsjo.FindTokens(weaponName))
            {
                Parser.Weapons.WeaponStatParser statParsed = Parser.Weapons.WeaponStatParser.FromJson(token.ToString());

                Image bulletImage = Resources.dmg64;
                myGraphic.DrawImage(ImageUtilities.ResizeImage(bulletImage, 15, 15), new Point(5, 502));
                DrawToLeft("     " + statParsed.DmgPb, myGraphic); //damage per bullet

                Image clipSizeImage = Resources.clipSize64;
                myGraphic.DrawImage(ImageUtilities.ResizeImage(clipSizeImage, 15, 15), new Point(52, 502));
                myGraphic.DrawString("      " + statParsed.ClipSize, new Font(FontUtilities.pfc.Families[0], 11), new SolidBrush(Color.White), new Point(50, 503));

                Image reload = Resources.reload64;
                myGraphic.DrawImage(ImageUtilities.ResizeImage(reload, 15, 15), new Point(50 + (statParsed.ClipSize.ToString().Length * 7) + 47, 502)); //50=clipsize text position | for each clipsize letter we add 7 to x | 47=difference between 2 icons
                myGraphic.DrawString(statParsed.ReloadTime + " " + SearchResource.getTextByKey("6BA53D764BA5CC13E821D2A807A72365", "seconds"), new Font(FontUtilities.pfc.Families[0], 11), new SolidBrush(Color.White), new Point(64 + (statParsed.ClipSize.ToString().Length * 7) + 47, 503)); //64=50+icon size (-1 because that wasn't perfectly at the position i wanted)

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
            Image bolt = Resources.LBolt64;
            myGraphic.DrawImage(ImageUtilities.ResizeImage(bolt, 15, 15), new Point(5, 501));

            DrawToLeft(MinToMax, myGraphic);
        }
    }
}
