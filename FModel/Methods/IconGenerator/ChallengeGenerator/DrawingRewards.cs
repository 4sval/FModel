using csharp_wick;
using FModel.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace FModel
{
    static class DrawingRewards
    {
        /// <summary>
        /// if itemToExtract is empty, no need to do this
        /// if itemToExtract contains ':' ("HomebaseBannerIcon" or "AthenaLoadingScreen" or "CosmeticVariantToken"), split to get the itemId
        /// </summary>
        /// <param name="itemToExtract"></param>
        /// <param name="itemQuantity"></param>
        public static void getRewards(string itemToExtract, string itemQuantity)
        {
            if (!string.IsNullOrWhiteSpace(itemToExtract))
            {
                if (itemToExtract.Contains(":"))
                {
                    var parts = itemToExtract.Split(':');
                    if (parts[0] == "HomebaseBannerIcon")
                        DrawRewardBanner(parts[1]);
                    else
                        DrawRewardIcon(parts[1]);
                }
                else if (IsRewardType(itemToExtract, "athenabattlestar"))
                    drawRewards(itemToExtract, "athenabattlestar", itemQuantity);
                else if (IsRewardType(itemToExtract, "AthenaSeasonalXP"))
                    drawRewards(itemToExtract, "AthenaSeasonalXP", itemQuantity);
                else if (IsRewardType(itemToExtract, "MtxGiveaway"))
                    drawRewards(itemToExtract, "MtxGiveaway", itemQuantity);
                else if (IsRewardType(itemToExtract, "AthenaFortbyte"))
                    drawRewards(itemToExtract, "AthenaFortbyte", itemQuantity);
                else
                    DrawRewardIcon(itemToExtract);
            }
        }

        /// <summary>
        /// JohnWick is case sensitive so that way we search the item in the dictionary with CurrentCultureIgnoreCase and if he exists, we take his name
        /// so the name taken will be working for JohnWick
        /// </summary>
        /// <param name="item"></param>
        public static void DrawRewardIcon(string item)
        {
            ItemIcon.ItemIconPath = string.Empty;
            var value = ThePak.AllpaksDictionary.Where(x => string.Equals(x.Key, item, StringComparison.CurrentCultureIgnoreCase)).Select(d => d.Key).FirstOrDefault();

            if (value != null)
            {
                string extractedIconPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[value], value);
                if (extractedIconPath != null)
                {
                    if (extractedIconPath.Contains(".uasset") || extractedIconPath.Contains(".uexp") || extractedIconPath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(extractedIconPath.Substring(0, extractedIconPath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                JArray AssetArray = JArray.FromObject(AssetData);

                                ItemIcon.SearchAthIteDefIcon(AssetArray[0]);

                                drawIcon();
                            }
                        }
                        catch (JsonSerializationException)
                        {
                            //do not crash when JsonSerialization does weird stuff
                        }
                    }
                }
            }
        }

        /// <summary>
        /// "BannerIcons" contains all banners id, their image path and more
        /// so basically we export and serialize "BannerIcons", locate where our banner id is, with FindTokens
        /// after that we only have what we want with token.ToString() -> "SmallImage", "LargeImage", "asset_path_name", "CategoryRowName", "DisplayName", "DisplayDescription"
        /// so we can just parse token.ToString() to properly get the LargeImage's asset_path_name or SmallImage's asset_path_name and draw
        /// </summary>
        /// <param name="bannerName"></param>
        public static void DrawRewardBanner(string bannerName)
        {
            ItemIcon.ItemIconPath = string.Empty;
            string extractedBannerPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary["BannerIcons"], "BannerIcons");
            if (extractedBannerPath != null)
            {
                if (extractedBannerPath.Contains(".uasset") || extractedBannerPath.Contains(".uexp") || extractedBannerPath.Contains(".ubulk"))
                {
                    JohnWick.MyAsset = new PakAsset(extractedBannerPath.Substring(0, extractedBannerPath.LastIndexOf('.')));
                    try
                    {
                        if (JohnWick.MyAsset.GetSerialized() != null)
                        {
                            dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                            JArray AssetArray = JArray.FromObject(AssetData);

                            JToken bannerToken = ((JObject)AssetArray[0]).GetValue(bannerName, StringComparison.InvariantCultureIgnoreCase);
                            if (bannerToken != null)
                            {
                                JToken largeImage = bannerToken["LargeImage"];
                                JToken smallImage = bannerToken["SmallImage"];
                                if (largeImage != null)
                                {
                                    JToken assetPathName = largeImage["asset_path_name"];
                                    if (assetPathName != null)
                                    {
                                        string textureFile = Path.GetFileName(assetPathName.Value<string>()).Substring(0, Path.GetFileName(assetPathName.Value<string>()).LastIndexOf('.'));
                                        ItemIcon.ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                        drawIcon();
                                    }
                                }
                                else if (smallImage != null)
                                {
                                    JToken assetPathName = smallImage["asset_path_name"];
                                    if (assetPathName != null)
                                    {
                                        string textureFile = Path.GetFileName(assetPathName.Value<string>()).Substring(0, Path.GetFileName(assetPathName.Value<string>()).LastIndexOf('.'));
                                        ItemIcon.ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                        drawIcon();
                                    }
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

        private static void drawIcon()
        {
            Image itemIcon = null;
            if (File.Exists(ItemIcon.ItemIconPath))
            {
                using (var bmpTemp = new Bitmap(ItemIcon.ItemIconPath))
                {
                    itemIcon = new Bitmap(bmpTemp);
                }
            }
            else
                itemIcon = Resources.unknown512;

            if (itemIcon != null)
            {
                BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 64, 64), new Point(902, BundleDesign.theY + 3));
                itemIcon.Dispose();
            }
        }

        private static void drawRewards(string itemId, string type, string quantity)
        {
            string value = ThePak.AllpaksDictionary.Where(x => string.Equals(x.Key, itemId, StringComparison.CurrentCultureIgnoreCase)).Select(d => d.Key).FirstOrDefault();
            if (value != null)
            {
                string extractedIconPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[value], value);
                if (extractedIconPath != null)
                {
                    if (extractedIconPath.Contains(".uasset") || extractedIconPath.Contains(".uexp") || extractedIconPath.Contains(".ubulk"))
                    {
                        JohnWick.MyAsset = new PakAsset(extractedIconPath.Substring(0, extractedIconPath.LastIndexOf('.')));
                        try
                        {
                            if (JohnWick.MyAsset.GetSerialized() != null)
                            {
                                dynamic AssetData = JsonConvert.DeserializeObject(JohnWick.MyAsset.GetSerialized());
                                JArray AssetArray = JArray.FromObject(AssetData);

                                ItemIcon.ItemIconPath = string.Empty;
                                ItemIcon.SearchAthIteDefIcon(AssetArray[0]);
                                if (File.Exists(ItemIcon.ItemIconPath))
                                {
                                    Image itemIcon;
                                    using (var bmpTemp = new Bitmap(ItemIcon.ItemIconPath))
                                    {
                                        itemIcon = new Bitmap(bmpTemp);
                                    }
                                    BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 48, 48), new Point(947, BundleDesign.theY + 12));
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

            GraphicsPath graphicsPath   = null;
            switch (type)
            {
                case "athenabattlestar":
                {
                    graphicsPath = new GraphicsPath();
                    drawPathAndFill(graphicsPath, quantity, Color.FromArgb(255, 143, 74, 32), Color.FromArgb(255, 255, 219, 103));
                    break;
                }
                case "AthenaSeasonalXP":
                {
                    graphicsPath = new GraphicsPath();
                    drawPathAndFill(graphicsPath, quantity, Color.FromArgb(255, 81, 131, 15), Color.FromArgb(255, 230, 253, 177));
                    break;
                }
                case "MtxGiveaway":
                {
                    graphicsPath = new GraphicsPath();
                    drawPathAndFill(graphicsPath, quantity, Color.FromArgb(255, 100, 160, 175), Color.FromArgb(255, 220, 230, 255));
                    break;
                }
                case "AthenaFortbyte":
                {
                    BundleDesign.toDrawOn.DrawString("#" + Int32.Parse(quantity).ToString("D2"), new Font(FontUtilities.pfc.Families[1], 40), new SolidBrush(Color.White), new Point(975, BundleDesign.theY + 5), FontUtilities.rightString);
                    break;
                }
                default:
                    break;
            }

            if (graphicsPath != null)
                graphicsPath.Dispose();
        }

        private static bool IsRewardType(string value, string comparison)
        {
            return string.Equals(value, comparison, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// define a pen to draw the outlines of a string with a color and fill with another color
        /// </summary>
        /// <param name="p"></param>
        /// <param name="quantity"></param>
        /// <param name="border"></param>
        /// <param name="filled"></param>
        private static void drawPathAndFill(GraphicsPath p, string quantity, Color border, Color filled)
        {
            Pen myPen = new Pen(border, 3);
            myPen.LineJoin = LineJoin.Round; //needed to avoid spikes
            p.AddString(quantity, FontUtilities.pfc.Families[1], (int)FontStyle.Regular, 27, new Point(945, BundleDesign.theY + 20), FontUtilities.rightString);
            BundleDesign.toDrawOn.DrawPath(myPen, p);

            BundleDesign.toDrawOn.FillPath(new SolidBrush(filled), p);
        }
    }
}
