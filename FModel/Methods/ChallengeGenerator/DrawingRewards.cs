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
                    if (parts[0] == "HomebaseBannerIcon") { DrawRewardBanner(parts[1]); }
                    else { DrawRewardIcon(parts[1]); }
                }
                else if (string.Equals(itemToExtract, "athenabattlestar", StringComparison.CurrentCultureIgnoreCase)) { drawBattleStar(itemQuantity); }
                else if (string.Equals(itemToExtract, "AthenaSeasonalXP", StringComparison.CurrentCultureIgnoreCase)) { drawSeasonalXp(itemQuantity); }
                else if (string.Equals(itemToExtract, "MtxGiveaway", StringComparison.CurrentCultureIgnoreCase)) { drawMtxGiveaway(itemQuantity); }
                else if (string.Equals(itemToExtract, "AthenaFortbyte", StringComparison.CurrentCultureIgnoreCase)) { drawFortbyte(itemQuantity); }
                else { DrawRewardIcon(itemToExtract); }
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

                                    drawIcon();
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

                                drawIcon();
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
            if (File.Exists(ItemIcon.ItemIconPath))
            {
                Image itemIcon;
                using (var bmpTemp = new Bitmap(ItemIcon.ItemIconPath))
                {
                    itemIcon = new Bitmap(bmpTemp);
                }
                BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 110, 110), new Point(2300, BundleDesign.theY + 6));
            }
            else
            {
                Image itemIcon = Resources.unknown512;
                BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 110, 110), new Point(2300, BundleDesign.theY + 6));
            }
        }

        private static void drawBattleStar(string quantity)
        {
            Image rewardIcon = Resources.T_FNBR_BattlePoints_L;
            BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(rewardIcon, 75, 75), new Point(2325, BundleDesign.theY + 22));

            GraphicsPath p = new GraphicsPath();
            drawPathAndFill(p, quantity, Color.FromArgb(255, 143, 74, 32), Color.FromArgb(255, 255, 219, 103));
        }

        private static void drawSeasonalXp(string quantity)
        {
            Image rewardIcon = Resources.T_FNBR_SeasonalXP_L;
            BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(rewardIcon, 75, 75), new Point(2325, BundleDesign.theY + 22));

            GraphicsPath p = new GraphicsPath();
            drawPathAndFill(p, quantity, Color.FromArgb(255, 81, 131, 15), Color.FromArgb(255, 230, 253, 177));
        }

        private static void drawMtxGiveaway(string quantity)
        {
            Image rewardIcon = Resources.T_Items_MTX_L;
            BundleDesign.toDrawOn.DrawImage(ImageUtilities.ResizeImage(rewardIcon, 75, 75), new Point(2325, BundleDesign.theY + 22));

            GraphicsPath p = new GraphicsPath();
            drawPathAndFill(p, quantity, Color.FromArgb(255, 100, 160, 175), Color.FromArgb(255, 220, 230, 255));
        }

        private static void drawFortbyte(string quantity)
        {
            BundleDesign.toDrawOn.DrawString("#" + Int32.Parse(quantity).ToString("D2"), new Font(FontUtilities.pfc.Families[1], 50), new SolidBrush(Color.White), new Point(2325, BundleDesign.theY + 22));
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
