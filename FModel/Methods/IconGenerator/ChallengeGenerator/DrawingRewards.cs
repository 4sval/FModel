using csharp_wick;
using FModel.Parser.Items;
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
                    drawRewards("athenabattlestar", itemQuantity);
                else if (IsRewardType(itemToExtract, "AthenaSeasonalXP"))
                    drawRewards("AthenaSeasonalXP", itemQuantity);
                else if (IsRewardType(itemToExtract, "MtxGiveaway"))
                    drawRewards("MtxGiveaway", itemQuantity);
                else if (IsRewardType(itemToExtract, "AthenaFortbyte"))
                    drawRewards("AthenaFortbyte", itemQuantity);
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
                                var itemId = ItemsIdParser.FromJson(JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString());
                                for (int i = 0; i < itemId.Length; i++)
                                {
                                    ItemIcon.SearchAthIteDefIcon(itemId[i]);

                                    drawIcon(item);
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
                            string parsedJson = JToken.Parse(JohnWick.MyAsset.GetSerialized()).ToString().TrimStart('[').TrimEnd(']');
                            JObject jo = JObject.Parse(parsedJson);
                            foreach (JToken token in jo.FindTokens(bannerName))
                            {
                                var bannerId = Parser.Banners.BannersParser.FromJson(token.ToString());

                                if (bannerId.LargeImage != null)
                                {
                                    string textureFile = Path.GetFileName(bannerId.LargeImage.AssetPathName)
                                        ?.Substring(0,
                                            Path.GetFileName(bannerId.LargeImage.AssetPathName).LastIndexOf('.'));

                                    ItemIcon.ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                }
                                else if (bannerId.SmallImage != null)
                                {
                                    string textureFile = Path.GetFileName(bannerId.SmallImage.AssetPathName)
                                        ?.Substring(0,
                                            Path.GetFileName(bannerId.SmallImage.AssetPathName).LastIndexOf('.'));

                                    ItemIcon.ItemIconPath = JohnWick.AssetToTexture2D(textureFile);
                                }

                                drawIcon(bannerName);
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

        private static void drawIcon(string itemId)
        {
            Image itemIcon = null;
            if (File.Exists(ItemIcon.ItemIconPath))
            {
                if (Settings.Default.challengesDebug)
                {
                    //draw quest reward id
                    BundleDesign.toDrawOn.DrawString(itemId, new Font("Courier New", 12), new SolidBrush(Color.White), new RectangleF(2110, BundleDesign.theY + 30, 190, 60), FontUtilities.centeredStringLine);
                }

                using (var bmpTemp = new Bitmap(ItemIcon.ItemIconPath))
                {
                    itemIcon = new Bitmap(bmpTemp);
                }
            }
            else
                itemIcon = Resources.unknown512;

            if (itemIcon != null)
            {
                BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 110, 110), new Point(2300, BundleDesign.theY + 6));
                itemIcon.Dispose();
            }
        }

        private static void drawRewards(string type, string quantity)
        {
            Image rewardIcon            = null;
            GraphicsPath graphicsPath   = null;

            switch (type)
            {
                case "athenabattlestar":
                {
                    rewardIcon = Resources.BattlePoints;
                    BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(rewardIcon, 75, 75), new Point(2325, BundleDesign.theY + 22));

                    graphicsPath = new GraphicsPath();
                    drawPathAndFill(graphicsPath, quantity, Color.FromArgb(255, 143, 74, 32), Color.FromArgb(255, 255, 219, 103));
                    break;
                }
                case "AthenaSeasonalXP":
                {
                    rewardIcon = Resources.SeasonalXP;
                    BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(rewardIcon, 75, 75), new Point(2325, BundleDesign.theY + 22));

                    graphicsPath = new GraphicsPath();
                    drawPathAndFill(graphicsPath, quantity, Color.FromArgb(255, 81, 131, 15), Color.FromArgb(255, 230, 253, 177));
                    break;
                }
                case "MtxGiveaway":
                {
                    rewardIcon = Resources.ItemsMTX;
                    BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(rewardIcon, 75, 75), new Point(2325, BundleDesign.theY + 22));

                    graphicsPath = new GraphicsPath();
                    drawPathAndFill(graphicsPath, quantity, Color.FromArgb(255, 100, 160, 175), Color.FromArgb(255, 220, 230, 255));
                    break;
                }
                case "AthenaFortbyte":
                {
                    BundleDesign.toDrawOn.DrawString("#" + Int32.Parse(quantity).ToString("D2"), new Font(FontUtilities.pfc.Families[1], 50), new SolidBrush(Color.White), new Point(2325, BundleDesign.theY + 22));
                    break;
                }
                default:
                    break;
            }

            if (rewardIcon != null)
                rewardIcon = null;

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
            Pen myPen = new Pen(border, 5);
            myPen.LineJoin = LineJoin.Round; //needed to avoid spikes
            p.AddString(quantity, FontUtilities.pfc.Families[1], (int)FontStyle.Regular, 60, new Point(2322, BundleDesign.theY + 25), FontUtilities.rightString);
            BundleDesign.toDrawOn.DrawPath(myPen, p);

            BundleDesign.toDrawOn.FillPath(new SolidBrush(filled), p);
        }
    }
}
