using csharp_wick;
using FModel.Parser.Items;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;

namespace FModel
{
    static class DrawText
    {
        private static string CosmeticSource { get; set; }
        private static string ShortDescription { get; set; }
        private static string CosmeticId { get; set; }
        private static string MaxStackSize { get; set; }
        private static string ItemAction { get; set; }
        private static string WeaponRowName { get; set; }
        private static string CosmeticUff { get; set; }
        private static string HeroType { get; set; }
        private static string DefenderType { get; set; }
        private static string MinToMax { get; set; }

        public static void DrawTexts(ItemsIdParser theItem, Graphics myGraphic, string mode)
        {
            DrawDisplayName(theItem, myGraphic);
            DrawDescription(theItem, myGraphic);

            SetTexts(theItem);

            switch (mode)
            {
                case "athIteDef":
                    DrawToLeft(ShortDescription, myGraphic);
                    DrawToRight(CosmeticSource, myGraphic);
                    break;
                case "consAndWeap":
                    DrawToRight(ItemAction, myGraphic);
                    if (MainWindow.ExtractedFilePath.Contains("Items\\Consumables\\"))
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

        private static void SetTexts(ItemsIdParser theItem)
        {
            CosmeticSource = "";
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
                ShortDescription = theItem.ShortDescription;
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
                if (theItem.WeaponStatHandle.RowName != "Harvest_Pickaxe_Athena_C_T01" || theItem.WeaponStatHandle.RowName != "Edged_Sword_Athena_C_T01")
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
                HeroType = theItem.AttributeInitKey.AttributeInitCategory;
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
            if (CosmeticUff != null)
            {
                if (CosmeticUff.Contains("Animated"))
                {
                    Image animatedLogo = Resources.T_Icon_Animated_64;
                    myGraphic.DrawImage(ImageUtilities.ResizeImage(animatedLogo, 32, 32), new Point(6, -2));
                }
                else if (CosmeticUff.Contains("HasUpgradeQuests") && theItem.ExportType != "AthenaPetCarrierItemDefinition")
                {
                    Image questLogo = Resources.T_Icon_Quests_64;
                    myGraphic.DrawImage(ImageUtilities.ResizeImage(questLogo, 32, 32), new Point(6, 6));
                }
                else if (CosmeticUff.Contains("HasUpgradeQuests") && theItem.ExportType == "AthenaPetCarrierItemDefinition")
                {
                    Image petLogo = Resources.T_Icon_Pets_64;
                    myGraphic.DrawImage(ImageUtilities.ResizeImage(petLogo, 32, 32), new Point(6, 6));
                }
                else if (CosmeticUff.Contains("HasVariants"))
                {
                    Image variantsLogo = Resources.T_Icon_Variant_64;
                    myGraphic.DrawImage(ImageUtilities.ResizeImage(variantsLogo, 32, 32), new Point(6, 6));
                }
                else if (CosmeticUff.Contains("Reactive"))
                {
                    Image reactiveLogo = Resources.T_Icon_Adaptive_64;
                    myGraphic.DrawImage(ImageUtilities.ResizeImage(reactiveLogo, 32, 32), new Point(7, 7));
                }
                else if (CosmeticUff.Contains("Traversal"))
                {
                    Image traversalLogo = Resources.T_Icon_Traversal_64;
                    myGraphic.DrawImage(ImageUtilities.ResizeImage(traversalLogo, 32, 32), new Point(6, 3));
                }
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
                myGraphic.DrawString(theItem.DisplayName, new Font(FontUtilities.pfc.Families[0], 35), new SolidBrush(Color.White), new Point(522 / 2, 395), FontUtilities.centeredString);
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
                myGraphic.DrawString(theItem.Description, new Font("Arial", 10), new SolidBrush(Color.White), new RectangleF(5, 441, 512, 49), FontUtilities.centeredStringLine);
            }
        }

        /// <summary>
        /// draw text at bottom right
        /// </summary>
        /// <param name="text"></param>
        /// <param name="myGraphic"></param>
        private static void DrawToRight(string text, Graphics myGraphic)
        {
            myGraphic.DrawString(text, new Font(FontUtilities.pfc.Families[0], 13), new SolidBrush(Color.White), new Point(522 - 5, 500), FontUtilities.rightString);
        }

        /// <summary>
        /// draw text at bottom left
        /// </summary>
        /// <param name="text"></param>
        /// <param name="myGraphic"></param>
        private static void DrawToLeft(string text, Graphics myGraphic)
        {
            myGraphic.DrawString(text, new Font(FontUtilities.pfc.Families[0], 13), new SolidBrush(Color.White), new Point(5, 500));
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
                myGraphic.DrawImage(ImageUtilities.ResizeImage(itemIcon, 122, 122), new Point(395, 282));
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
                            JObject jo = JObject.Parse(parsedJson);
                            foreach (JToken token in jo.FindTokens(weaponName))
                            {
                                var statParsed = Parser.Weapons.WeaponStatParser.FromJson(token.ToString());

                                Image bulletImage = Resources.dmg64;
                                myGraphic.DrawImage(ImageUtilities.ResizeImage(bulletImage, 15, 15), new Point(5, 500));

                                Image clipSizeImage = Resources.clipSize64;
                                myGraphic.DrawImage(ImageUtilities.ResizeImage(clipSizeImage, 15, 15), new Point(52, 500));

                                DrawToRight("Reload Time: " + statParsed.ReloadTime + " seconds", myGraphic);
                                DrawToLeft("    " + statParsed.DmgPb, myGraphic); //damage per bullet
                                myGraphic.DrawString("     " + statParsed.ClipSize, new Font(FontUtilities.pfc.Families[0], 13), new SolidBrush(Color.White), new Point(50, 500));
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
