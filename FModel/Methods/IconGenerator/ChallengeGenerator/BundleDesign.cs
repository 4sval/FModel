using FModel.Parser.Challenges;
using FModel.Parser.Items;
using FModel.Properties;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System;

namespace FModel
{
    static class BundleDesign
    {
        public static string BundlePath { get; set; }
        public static int theY { get; set; }
        public static Graphics toDrawOn { get; set; }
        public static ItemsIdParser myItem { get; set; }

        /// <summary>
        /// get a random color in case DisplayStyle doesn't exist in drawBackground()
        /// </summary>
        /// <returns></returns>
        private static Color getRandomColor()
        {
            Random rnd = new Random();

            int Red = rnd.Next(50, 200);
            int Green = rnd.Next(50, 200);
            int Blue = rnd.Next(50, 200);

            return Color.FromArgb(255, Red, Green, Blue);
        }

        /// <summary>
        /// draw the pretty header if DisplayStyle exist, else the pretty header but with random colors
        /// </summary>
        /// <param name="myBitmap"></param>
        /// <param name="myBundle"></param>
        public static void drawBackground(Bitmap myBitmap, ChallengeBundleIdParser myBundle)
        {
            new UpdateMyState("Drawing...", "Waiting").ChangeProcessState();
            if (myBundle.DisplayStyle != null)
            {
                //main header
                toDrawOn.FillRectangle(new SolidBrush(BundleInfos.getSecondaryColor(myBundle)), new Rectangle(0, 0, myBitmap.Width, 281));

                //gradient at left and right main header
                LinearGradientBrush linGrBrush_left = new LinearGradientBrush(new Point(0, 282 / 2), new Point(282, 282 / 2),
                    ControlPaint.Light(BundleInfos.getSecondaryColor(myBundle), (float)0.3), BundleInfos.getSecondaryColor(myBundle));
                toDrawOn.FillRectangle(linGrBrush_left, new Rectangle(0, 0, 282, 282));
                LinearGradientBrush linGrBrush_right = new LinearGradientBrush(new Point(2500, 282 / 2), new Point(1500, 282 / 2),
                    ControlPaint.Light(BundleInfos.getSecondaryColor(myBundle), (float)0.3), BundleInfos.getSecondaryColor(myBundle));
                toDrawOn.FillRectangle(linGrBrush_right, new Rectangle(1500, 0, 1000, 282));

                //last folder with border
                GraphicsPath p = new GraphicsPath();
                Pen myPen = new Pen(ControlPaint.Light(BundleInfos.getSecondaryColor(myBundle), (float)0.2), 3);
                myPen.LineJoin = LineJoin.Round; //needed to avoid spikes
                p.AddString(
                    Utilities.CaseInsensitiveContains(BundleInfos.getLastFolder(BundlePath), "SEASON", StringComparison.CurrentCultureIgnoreCase) ?
                    SearchResource.getTextByKey("15EB9C4A494F597285CC5CA2EAA571E1", "SEASON") + " " + BundleInfos.getLastFolder(BundlePath).Substring(BundleInfos.getLastFolder(BundlePath).Length - 1) :
                    BundleInfos.getLastFolder(BundlePath),
                    Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1],
                    (int)FontStyle.Regular, 55,
                    new Point(342, 40),
                    FontUtilities.leftString
                    );
                toDrawOn.DrawPath(myPen, p);
                toDrawOn.FillPath(new SolidBrush(ControlPaint.Dark(BundleInfos.getSecondaryColor(myBundle), (float)0.05)), p);

                //name
                toDrawOn.DrawString(BundleInfos.getBundleDisplayName(myItem), new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1], 115), new SolidBrush(Color.White), new Point(325, 70));

                //image
                if (myBundle.DisplayStyle.DisplayImage != null)
                {
                    string textureFile = Path.GetFileName(myBundle.DisplayStyle.DisplayImage.AssetPathName).Substring(0, Path.GetFileName(myBundle.DisplayStyle.DisplayImage.AssetPathName).LastIndexOf('.'));
                    Image challengeIcon;
                    using (var bmpTemp = new Bitmap(JohnWick.AssetToTexture2D(textureFile)))
                    {
                        challengeIcon = new Bitmap(bmpTemp);
                    }
                    toDrawOn.DrawImage(ImageUtilities.ResizeImage(challengeIcon, 282, 282), new Point(40, 0));
                }
                else if (myBundle.LargePreviewImage != null)
                {
                    string textureFile = Path.GetFileName(myBundle.LargePreviewImage.AssetPathName).Substring(0, Path.GetFileName(myBundle.LargePreviewImage.AssetPathName).LastIndexOf('.'));
                    Image challengeIcon;
                    using (var bmpTemp = new Bitmap(JohnWick.AssetToTexture2D(textureFile)))
                    {
                        challengeIcon = new Bitmap(bmpTemp);
                    }
                    toDrawOn.DrawImage(ImageUtilities.ResizeImage(challengeIcon, 282, 282), new Point(40, 0));
                }
                else
                {
                    toDrawOn.DrawImage(ImageUtilities.ResizeImage(Resources.unknown512, 282, 282), new Point(40, 0));
                }

                //fill the rest
                toDrawOn.FillRectangle(new SolidBrush(ControlPaint.Dark(BundleInfos.getSecondaryColor(myBundle), (float)0.1)), new Rectangle(0, 271, myBitmap.Width, myBitmap.Height));
            }
            else
            {
                Color myBaseColor = getRandomColor();

                //main header
                toDrawOn.FillRectangle(new SolidBrush(myBaseColor), new Rectangle(0, 0, myBitmap.Width, 281));

                //gradient at left and right main header
                LinearGradientBrush linGrBrush_left = new LinearGradientBrush(new Point(0, 282 / 2), new Point(282, 282 / 2),
                    ControlPaint.Light(myBaseColor, (float)0.3), myBaseColor);
                toDrawOn.FillRectangle(linGrBrush_left, new Rectangle(0, 0, 282, 282));
                LinearGradientBrush linGrBrush_right = new LinearGradientBrush(new Point(2500, 282 / 2), new Point(1500, 282 / 2),
                    ControlPaint.Light(myBaseColor, (float)0.3), myBaseColor);
                toDrawOn.FillRectangle(linGrBrush_right, new Rectangle(1500, 0, 1000, 282));

                //fill the rest
                toDrawOn.FillRectangle(new SolidBrush(ControlPaint.Dark(myBaseColor, (float)0.1)), new Rectangle(0, 271, myBitmap.Width, myBitmap.Height));

                //last folder
                toDrawOn.DrawString(Utilities.CaseInsensitiveContains(BundleInfos.getLastFolder(BundlePath), "SEASON", StringComparison.CurrentCultureIgnoreCase) ?
                    SearchResource.getTextByKey("15EB9C4A494F597285CC5CA2EAA571E1", "SEASON") + " " + BundleInfos.getLastFolder(BundlePath).Substring(BundleInfos.getLastFolder(BundlePath).Length - 1) :
                    BundleInfos.getLastFolder(BundlePath),
                    new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1], 42),
                    new SolidBrush(ControlPaint.Dark(myBaseColor, (float)0.05)),
                    new Point(40, 40)
                    );

                //name
                toDrawOn.DrawString(BundleInfos.getBundleDisplayName(myItem), new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1], 115), new SolidBrush(Color.White), new Point(25, 70));
            }
        }

        /// <summary>
        /// get and draw completion text and its reward
        /// if AssetPathName is "None" we take the TemplateId (it's most likely a banner)
        /// else we take AssetPathName and we ignore "AthenaBattlePass_WeeklyChallenge_Token" and "AthenaBattlePass_WeeklyBundle_Token" because these are useless
        /// ignoring these 2 should give us an item id, we draw this item
        /// </summary>
        /// <param name="myBundle"></param>
        public static void drawCompletionReward(ChallengeBundleIdParser myBundle)
        {
            if (myBundle.BundleCompletionRewards != null)
            {
                theY += 100;
                for (int x = 0; x < myBundle.BundleCompletionRewards.Length; x++)
                {
                    for (int i = 0; i < myBundle.BundleCompletionRewards[x].Rewards.Length; i++)
                    {
                        string compCount = myBundle.BundleCompletionRewards[x].CompletionCount.ToString();
                        string itemQuantity = myBundle.BundleCompletionRewards[x].Rewards[i].Quantity.ToString();

                        if (myBundle.BundleCompletionRewards[x].Rewards[i].ItemDefinition.AssetPathName == "None")
                        {
                            theY += 140;

                            DrawingRewards.getRewards(myBundle.BundleCompletionRewards[x].Rewards[i].TemplateId, itemQuantity);

                            drawCompletionText(compCount);
                        }
                        else
                        {
                            string rewardId = Path.GetFileName(myBundle.BundleCompletionRewards[x].Rewards[i].ItemDefinition.AssetPathName.Substring(0, myBundle.BundleCompletionRewards[x].Rewards[i].ItemDefinition.AssetPathName.LastIndexOf(".", StringComparison.Ordinal)));

                            if (rewardId != "AthenaBattlePass_WeeklyChallenge_Token" && rewardId != "AthenaBattlePass_WeeklyBundle_Token")
                            {
                                theY += 140;

                                try //needed for rare cases where the icon is in /Content/icon.uasset and atm idk why but i can't extract
                                {
                                    if (rewardId.Contains("Fortbyte_WeeklyChallengesComplete_")) { drawForbyteReward(); }
                                    else { DrawingRewards.getRewards(rewardId, itemQuantity); }
                                }
                                catch (Exception)
                                {
                                    drawUnknownReward();
                                }

                                drawCompletionText(compCount);
                            }
                        }
                    }
                }
            }
        }
        private static void drawCompletionText(string count)
        {
            string all = "Complete ALL CHALLENGES to earn the reward item";
            string any = "Complete ANY " + count + " CHALLENGES to earn the reward item";

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
                    all = SearchResource.getTextByKey("CompletionRewardFormat_All", "Complete ALL CHALLENGES to earn the reward item", "AthenaChallengeDetailsEntry");
                    any = SearchResource.getTextByKey("CompletionRewardFormat", "Complete ANY " + count + " CHALLENGES to earn the reward item", "AthenaChallengeDetailsEntry");

                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(all);
                    if (doc.DocumentNode.InnerText.Contains(" {0}")) //avoid white space
                    {
                        all = doc.DocumentNode.InnerText.Replace(" {0}", string.Empty);
                    }
                    else { all = doc.DocumentNode.InnerText.Replace("{0}", string.Empty); }

                    doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(any);
                    if (doc.DocumentNode.InnerText.Contains("{QuestNumber}")) //russian
                    {
                        any = doc.DocumentNode.InnerText.Replace("{QuestNumber}", count);
                    }
                    else { any = string.Format(doc.DocumentNode.InnerText, count); }
                    break;
                default:
                    break;
            }

            toDrawOn.DrawString(count == "-1" ? all : any, new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1], 50), new SolidBrush(Color.White), new Point(100, theY + 22));
        }

        /// <summary>
        /// draw the watermark at the bottom of the bundle of challenges icon
        /// </summary>
        /// <param name="myBitmap"></param>
        public static void drawWatermark(Bitmap myBitmap)
        {
            string text = Settings.Default.challengesWatermark;
            if (string.IsNullOrWhiteSpace(text))
            {
                text = "{Bundle_Name} Generated using FModel & JohnWickParse - {Date}";
            }

            if (text.Contains("{Bundle_Name}"))
            {
                text = text.Replace("{Bundle_Name}", SearchResource.getTextByKey(myItem.DisplayName.Key, myItem.DisplayName.SourceString));
            }
            if (text.Contains("{Date}"))
            {
                text = text.Replace("{Date}", DateTime.Now.ToString("dd/MM/yyyy"));
            }

            toDrawOn.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), new Rectangle(0, theY + 240, myBitmap.Width, 40));
            toDrawOn.DrawString(text, new Font(FontUtilities.pfc.Families[0], 20), new SolidBrush(Color.FromArgb(150, 255, 255, 255)), new Point(myBitmap.Width / 2, theY + 250), FontUtilities.centeredString);
        }

        private static void drawForbyteReward()
        {
            string textureFile = "T_UI_ChallengeTile_Fortbytes";
            ItemIcon.ItemIconPath = JohnWick.AssetToTexture2D(textureFile);

            if (File.Exists(ItemIcon.ItemIconPath))
            {
                Image itemIcon;
                using (var bmpTemp = new Bitmap(ItemIcon.ItemIconPath))
                {
                    itemIcon = new Bitmap(bmpTemp);
                }
                toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 110, 110), new Point(2300, theY + 6));
            }
            else
            {
                Image itemIcon = Resources.unknown512;
                toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 110, 110), new Point(2300, theY + 6));
            }
        }
        private static void drawUnknownReward()
        {
            Image itemIcon = Resources.unknown512;
            toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 110, 110), new Point(2300, theY + 6));
        }
    }
}
