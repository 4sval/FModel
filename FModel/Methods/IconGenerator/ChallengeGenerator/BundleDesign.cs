using FModel.Properties;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System;
using Newtonsoft.Json.Linq;

namespace FModel
{
    static class BundleDesign
    {
        public static string BundlePath { get; set; }
        public static int theY { get; set; }
        public static Graphics toDrawOn { get; set; }
        public static JToken myItem { get; set; }
        private static Color headerColor { get; set; }
        public static bool isBundleLevelup { get; set; }
        public static bool isRequiresBattlePass { get; set; }
        public static bool isGrantWithBundle { get; set; }

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
        public static void drawBackground(Bitmap myBitmap, JToken myBundle)
        {
            new UpdateMyState("Drawing...", "Waiting").ChangeProcessState();

            drawHeader(myBitmap, myBundle);
        }

        private static void drawHeader(Bitmap myBitmap, JToken myBundle)
        {
            bool isSXBanner = false;
            string bundleDisplayName = BundleInfos.getBundleDisplayName(myItem);
            string lastFolder = BundleInfos.getLastFolder(BundlePath);

            JToken displayStyle = myBundle["DisplayStyle"];
            if (displayStyle != null)
            {
                if (Settings.Default.isChallengesTheme)
                {
                    string[] colorParts = Settings.Default.challengesColors.Split(',');
                    headerColor = Color.FromArgb(255, Int32.Parse(colorParts[0]), Int32.Parse(colorParts[1]), Int32.Parse(colorParts[2]));
                }
                else
                {
                    headerColor = BundleInfos.getSecondaryColor(myBundle);
                }

                JToken customBackground = displayStyle["CustomBackground"];
                JToken displayImage = displayStyle["DisplayImage"];
                if (customBackground != null)
                {
                    JToken assetPathName = customBackground["asset_path_name"];
                    if (assetPathName != null)
                    {
                        if (assetPathName.Value<string>().Contains("/Game/Athena/UI/Challenges/Art/MissionTileImages/") && !ThePak.CurrentUsedItem.Equals("QuestBundle_S10_SeasonX"))
                        {
                            isSXBanner = true;
                            string textureFile = Path.GetFileName(assetPathName.Value<string>()).Substring(0, Path.GetFileName(assetPathName.Value<string>()).LastIndexOf('.'));
                            Image challengeIcon;
                            using (Bitmap bmpTemp = new Bitmap(JohnWick.AssetToTexture2D(textureFile)))
                            {
                                challengeIcon = new Bitmap(bmpTemp);
                            }

                            toDrawOn.FillRectangle(new SolidBrush(headerColor), new Rectangle(-1, -1, myBitmap.Width + 1, 257));
                            if (Settings.Default.isChallengesTheme && File.Exists(Settings.Default.challengesBannerFileName))
                            {
                                Image banner = Image.FromFile(Settings.Default.challengesBannerFileName);
                                var opacityImage = ImageUtilities.SetImageOpacity(banner, (float)Settings.Default.challengesOpacity / 1000);
                                toDrawOn.DrawImage(ImageUtilities.ResizeImage(opacityImage, 1024, 256), 0, 0);
                            }
                            else
                            {
                                var opacityImage = ImageUtilities.SetImageOpacity(challengeIcon, (float)0.3);
                                toDrawOn.DrawImage(opacityImage, new Point(0, 0));
                            }
                        }
                    }
                }
                if (!isSXBanner)
                {
                    if (displayImage != null)
                    {
                        JToken assetPathName = displayImage["asset_path_name"];
                        if (assetPathName != null)
                        {
                            string textureFile = Path.GetFileName(assetPathName.Value<string>()).Substring(0, Path.GetFileName(assetPathName.Value<string>()).LastIndexOf('.'));
                            Image challengeIcon;
                            using (var bmpTemp = new Bitmap(JohnWick.AssetToTexture2D(textureFile)))
                            {
                                challengeIcon = new Bitmap(bmpTemp);
                            }

                            toDrawOn.FillRectangle(new SolidBrush(headerColor), new Rectangle(-1, -1, myBitmap.Width + 1, 257));
                            if (Settings.Default.isChallengesTheme)
                            {
                                if (File.Exists(Settings.Default.challengesBannerFileName))
                                {
                                    Image banner = Image.FromFile(Settings.Default.challengesBannerFileName);
                                    var opacityImage = ImageUtilities.SetImageOpacity(banner, (float)Settings.Default.challengesOpacity / 1000);
                                    toDrawOn.DrawImage(ImageUtilities.ResizeImage(opacityImage, 1024, 256), 0, 0);
                                }
                            }

                            toDrawOn.DrawImage(ImageUtilities.ResizeImage(challengeIcon, 256, 256), new Point(0, 0));
                        }
                    }
                    else
                    {
                        toDrawOn.FillRectangle(new SolidBrush(headerColor), new Rectangle(-1, -1, myBitmap.Width + 1, 257));
                        if (Settings.Default.isChallengesTheme)
                        {
                            if (File.Exists(Settings.Default.challengesBannerFileName))
                            {
                                Image banner = Image.FromFile(Settings.Default.challengesBannerFileName);
                                var opacityImage = ImageUtilities.SetImageOpacity(banner, (float)Settings.Default.challengesOpacity / 1000);
                                toDrawOn.DrawImage(ImageUtilities.ResizeImage(opacityImage, 1024, 256), 0, 0);
                            }
                        }
                    }
                }
            }
            else
            {
                if (Settings.Default.isChallengesTheme)
                {
                    string[] colorParts = Settings.Default.challengesColors.Split(',');
                    headerColor = Color.FromArgb(255, Int32.Parse(colorParts[0]), Int32.Parse(colorParts[1]), Int32.Parse(colorParts[2]));
                }
                else
                {
                    headerColor = getRandomColor();
                }

                toDrawOn.FillRectangle(new SolidBrush(headerColor), new Rectangle(-1, -1, myBitmap.Width + 1, 257));
                if (Settings.Default.isChallengesTheme)
                {
                    if (File.Exists(Settings.Default.challengesBannerFileName))
                    {
                        Image banner = Image.FromFile(Settings.Default.challengesBannerFileName);
                        var opacityImage = ImageUtilities.SetImageOpacity(banner, (float)Settings.Default.challengesOpacity / 1000);
                        toDrawOn.DrawImage(ImageUtilities.ResizeImage(opacityImage, 1024, 256), 0, 0);
                    }
                }
            }

            GraphicsPath gp = new GraphicsPath();
            gp.StartFigure();
            gp.AddLine(0, 256, myBitmap.Width, 256);
            gp.AddLine(myBitmap.Width, 256, myBitmap.Width, 241);
            gp.AddLine(myBitmap.Width, 241, myBitmap.Width / 2 + 25, 236);
            gp.AddLine(myBitmap.Width / 2 + 25, 236, myBitmap.Width / 2 + 35, 249);
            gp.AddLine(myBitmap.Width / 2 + 35, 249, 0, 241);
            gp.CloseFigure();
            toDrawOn.FillPath(new SolidBrush(ControlPaint.Light(headerColor)), gp);

            GraphicsPath p = new GraphicsPath();
            Pen myPen = new Pen(ControlPaint.Light(headerColor, (float)0.2), 3);
            myPen.LineJoin = LineJoin.Round; //needed to avoid spikes
            p.AddString(
                lastFolder,
                Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1],
                (int)FontStyle.Regular, 30,
                new Point(isSXBanner || displayStyle == null ? 30 : 265, 70),
                FontUtilities.leftString
                );
            toDrawOn.DrawPath(myPen, p);
            toDrawOn.FillPath(new SolidBrush(ControlPaint.Dark(headerColor, (float)0.05)), p);

            toDrawOn.DrawString(bundleDisplayName, new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1], 40), new SolidBrush(Color.White), new Point(isSXBanner || displayStyle == null ? 25 : 260, 105));

            toDrawOn.FillRectangle(new SolidBrush(ControlPaint.Dark(headerColor, (float)0.1)), new Rectangle(-1, 255, myBitmap.Width + 1, myBitmap.Height));
        }

        public static void drawQuestBackground(Bitmap myBitmap, bool noCompletion = true)
        {
            toDrawOn.FillRectangle(new SolidBrush(Color.FromArgb(50, headerColor.R, headerColor.G, headerColor.B)), new Rectangle(25, theY, myBitmap.Width - 50, 70));

            GraphicsPath gp = new GraphicsPath();
            gp.StartFigure();
            gp.AddLine(32, theY + 5, 29, theY + 67);
            gp.AddLine(29, theY + 67, myBitmap.Width - 160, theY + 62);
            gp.AddLine(myBitmap.Width - 160, theY + 62, myBitmap.Width - 150, theY + 4);
            gp.CloseFigure();
            toDrawOn.FillPath(new SolidBrush(Color.FromArgb(50, headerColor.R, headerColor.G, headerColor.B)), gp);

            if (noCompletion) { toDrawOn.FillRectangle(new SolidBrush(headerColor), new Rectangle(60, theY + 47, 500, 7)); }

            gp = new GraphicsPath();
            gp.StartFigure();
            gp.AddLine(39, theY + 35, 45, theY + 32);
            gp.AddLine(45, theY + 32, 48, theY + 37);
            gp.AddLine(48, theY + 37, 42, theY + 40);
            gp.CloseFigure();
            toDrawOn.FillPath(new SolidBrush(headerColor), gp);
        }

        /// <summary>
        /// get and draw completion text and its reward
        /// if AssetPathName is "None" we take the TemplateId (it's most likely a banner)
        /// else we take AssetPathName and we ignore "AthenaBattlePass_WeeklyChallenge_Token" and "AthenaBattlePass_WeeklyBundle_Token" because these are useless
        /// ignoring these 2 should give us an item id, we draw this item
        /// </summary>
        /// <param name="myBundle"></param>
        public static void drawCompletionReward(Bitmap myBitmap, JToken myBundle)
        {
            JToken bundleCompletionRewards = myBundle["BundleCompletionRewards"];
            if (bundleCompletionRewards != null)
            {
                new UpdateMyState("Drawing Completion Rewards...", "Waiting").ChangeProcessState();
                theY += 35;

                JArray bundleCompletionRewardsArray = bundleCompletionRewards.Value<JArray>();
                foreach (JToken token in bundleCompletionRewardsArray)
                {
                    string compCount = string.Empty;
                    JToken completionCount = token["CompletionCount"];
                    if (completionCount != null)
                    {
                        compCount = completionCount.Value<string>();
                    }

                    JToken rewards = token["Rewards"];
                    if (rewards != null)
                    {
                        JArray rewardsArray = rewards.Value<JArray>();
                        for (int i = 0; i < rewardsArray.Count; i++)
                        {
                            string itemQuantity = string.Empty;
                            JToken quantity = rewardsArray[i]["Quantity"];
                            if (quantity != null)
                            {
                                itemQuantity = quantity.Value<string>();
                            }

                            JToken itemDefinition = rewardsArray[i]["ItemDefinition"];
                            if (itemDefinition != null)
                            {
                                JToken assetPathName = itemDefinition["asset_path_name"];
                                if (assetPathName != null)
                                {
                                    if (assetPathName.Value<string>().Equals("None"))
                                    {
                                        theY += 140;
                                        drawCompletionText(myBitmap, compCount);
                                        DrawingRewards.getRewards(rewardsArray[i]["TemplateId"].Value<string>(), itemQuantity);
                                    }
                                    else
                                    {
                                        string rewardId = Path.GetFileName(assetPathName.Value<string>().Substring(0, assetPathName.Value<string>().LastIndexOf(".", StringComparison.Ordinal)));

                                        if (!assetPathName.Value<string>().Contains("/Game/Items/Tokens/") && !rewardId.Contains("Quest_BR_")) //no more fortbyte for weekly challenges
                                        {
                                            theY += 90;
                                            drawCompletionText(myBitmap, compCount);
                                            DrawingRewards.getRewards(rewardId, itemQuantity);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private static void drawCompletionText(Bitmap myBitmap, string count)
        {
            string all = "Complete ALL CHALLENGES to earn the reward item";
            string any = "Complete ANY " + count + " CHALLENGES to earn the reward item";

            if (!Settings.Default.IconLanguage.Equals("English"))
            {
                all = SearchResource.getTextByKey("CompletionRewardFormat_All", "Complete ALL CHALLENGES to earn the reward item", "AthenaChallengeDetailsEntry");
                any = SearchResource.getTextByKey("CompletionRewardFormat", "Complete ANY " + count + " CHALLENGES to earn the reward item", "AthenaChallengeDetailsEntry");

                //because HtmlAgilityPack fail to detect the end of the tag when it's </>
                if (all.Contains("</>")) { all = all.Replace("</>", "</text>"); }
                if (any.Contains("</>")) { any = any.Replace("</>", "</text>"); }

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(all);
                if (doc.DocumentNode.InnerText.Contains(" {0}")) //avoid white space
                {
                    if (all.Contains("</text>"))
                    {
                        all = doc.DocumentNode.InnerText.Replace(doc.DocumentNode.SelectSingleNode("text").InnerText, doc.DocumentNode.SelectSingleNode("text").InnerText.ToUpper());
                        all = all.Replace(" {0}", string.Empty);
                    }
                    else { all = doc.DocumentNode.InnerText.Replace(" {0}", string.Empty); }
                }
                else
                {
                    if (all.Contains("</text>"))
                    {
                        all = doc.DocumentNode.InnerText.Replace(doc.DocumentNode.SelectSingleNode("text").InnerText, doc.DocumentNode.SelectSingleNode("text").InnerText.ToUpper());
                        all = all.Replace("{0}", string.Empty);
                    }
                    else { all = doc.DocumentNode.InnerText.Replace("{0}", string.Empty); }
                }

                doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(any);
                if (doc.DocumentNode.InnerText.Contains("{QuestNumber}")) //russian
                {
                    if (any.Contains("</text>"))
                    {
                        any = doc.DocumentNode.InnerText.Replace(doc.DocumentNode.SelectSingleNode("text").InnerText, doc.DocumentNode.SelectSingleNode("text").InnerText.ToUpper());
                        any = any.Replace("{QuestNumber}", count);
                    }
                    else { any = doc.DocumentNode.InnerText.Replace("{QuestNumber}", count); }
                }
                else
                {
                    if (any.Contains("</text>"))
                    {
                        any = doc.DocumentNode.InnerText.Replace(doc.DocumentNode.SelectSingleNode("text").InnerText, doc.DocumentNode.SelectSingleNode("text").InnerText.ToUpper());
                        any = string.Format(any, count);
                    }
                    else { any = string.Format(doc.DocumentNode.InnerText, count); }
                }

                if (all.Contains("  ")) { all = all.Replace("  ", " "); } //double space in Spanish (LA)               i.e. with QuestBundle_PirateParty
                if (any.Contains("  ")) { any = any.Replace("  ", " "); }
            }

            drawQuestBackground(myBitmap, false);
            Font goodFont = FontUtilities.FindFont(toDrawOn, count == "-1" ? all : any, new Rectangle(57, theY + 7, myBitmap.Width - 227, 45).Size, new Font(Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1], 30)); //size in "new Font()" is never check
            toDrawOn.DrawString(count == "-1" ? all : any, goodFont, new SolidBrush(Color.White), new Point(55, theY + 15));
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
                text = text.Replace("{Bundle_Name}", SearchResource.getTextByKey(myItem["DisplayName"]["key"].Value<string>(), myItem["DisplayName"]["source_string"].Value<string>()));
            }
            if (text.Contains("{Date}"))
            {
                text = text.Replace("{Date}", DateTime.Now.ToString("dd/MM/yyyy"));
            }

            toDrawOn.DrawString(text, new Font(FontUtilities.pfc.Families[0], 15), new SolidBrush(Color.FromArgb(150, 255, 255, 255)), new Point(myBitmap.Width - 10, 210), FontUtilities.rightString);
        }

        public static void drawSeparator(string unlockType)
        {
            switch (unlockType)
            {
                case "EChallengeBundleQuestUnlockType::BundleLevelup":
                    if (!isBundleLevelup) { drawIconSeparator("T-FNBR-MissionIcon-L"); isBundleLevelup = true; }
                    break;
                case "EChallengeBundleQuestUnlockType::RequiresBattlePass":
                    if (!isRequiresBattlePass) { drawIconSeparator("T-FNBR-BattlePass-L"); isRequiresBattlePass = true; }
                    break;
                /*case "EChallengeBundleQuestUnlockType::GrantWithBundle": //GrantWithBundle doesn't mean free or paid battle pass so idk i just leave this here
                    if (!isGrantWithBundle) { drawPrestigeSeparator("T-FNBR-BattlePassChallenge-Silver-L"); isGrantWithBundle = true; }
                    break;*/
            }
        }

        private static void drawIconSeparator(string iconName)
        {
            string texture = JohnWick.AssetToTexture2D(iconName);
            if (File.Exists(texture))
            {
                Image itemIcon;
                using (var bmpTemp = new Bitmap(texture))
                {
                    itemIcon = new Bitmap(bmpTemp);
                }

                toDrawOn.FillRectangle(new SolidBrush(Color.FromArgb(100, headerColor.R, headerColor.G, headerColor.B)), new Rectangle(25, theY, 50, 35));

                toDrawOn.DrawImage(ImageUtilities.ResizeImage(itemIcon, 32, 32), new Point(50 - 15, theY + 2));

                theY += 40;
            }
        }
    }
}
