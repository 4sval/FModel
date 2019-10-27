using FModel.Methods.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets.IconCreator.ChallengeID
{
    class ChallengeCompletionRewards
    {
        public static void DrawChallengeCompletion(JArray AssetProperties, SolidColorBrush PrimaryColor, SolidColorBrush SecondaryColor, int y)
        {
            JArray bundleCompletionRewardsArray = AssetsUtility.GetPropertyTagText<JArray>(AssetProperties, "BundleCompletionRewards", "data");
            if (bundleCompletionRewardsArray != null)
            {
                new UpdateMyProcessEvents("Completion Rewards...", "Waiting").Update();
                IconCreator.ICDrawingContext.DrawRectangle(ChallengesUtility.DarkBrush(PrimaryColor, 0.3f), null, new Rect(0, y, 1024, 40));
                y += 40;

                foreach (JToken data in bundleCompletionRewardsArray)
                {
                    JArray challengeBundleRewardsArray = data["struct_type"]["properties"].Value<JArray>();
                    if (challengeBundleRewardsArray != null)
                    {
                        string completionCount = string.Empty;
                        JToken completionCountToken = AssetsUtility.GetPropertyTag<JToken>(challengeBundleRewardsArray, "CompletionCount");
                        if (completionCountToken != null)
                        {
                            completionCount = completionCountToken.Value<string>();
                        }

                        JArray rewardsArray = AssetsUtility.GetPropertyTagText<JArray>(challengeBundleRewardsArray, "Rewards", "data");
                        if (rewardsArray != null)
                        {
                            foreach (JToken reward in rewardsArray)
                            {
                                if (reward["struct_name"] != null && reward["struct_type"] != null && string.Equals(reward["struct_name"].Value<string>(), "AthenaRewardItemReference"))
                                {
                                    JArray dataPropertiesArray = reward["struct_type"]["properties"].Value<JArray>();
                                    if (dataPropertiesArray != null)
                                    {
                                        string rewardPath = string.Empty;
                                        string rewardQuantity = string.Empty;

                                        JToken quantityToken = AssetsUtility.GetPropertyTag<JToken>(dataPropertiesArray, "Quantity");
                                        if (quantityToken != null)
                                        {
                                            rewardQuantity = quantityToken.Value<string>();
                                        }

                                        JToken questDefinitionToken = AssetsUtility.GetPropertyTagText<JToken>(dataPropertiesArray, "ItemDefinition", "asset_path_name");
                                        if (questDefinitionToken != null)
                                        {
                                            if (questDefinitionToken.Value<string>().Contains("/Game/Items/Tokens/") || questDefinitionToken.Value<string>().Contains("Quest_BR_"))
                                            {
                                                continue;
                                            }

                                            if (string.Equals(questDefinitionToken.Value<string>(), "None", StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                //banners
                                                JToken templateIdToken = AssetsUtility.GetPropertyTag<JToken>(dataPropertiesArray, "TemplateId");
                                                if (templateIdToken != null)
                                                {
                                                    DrawCompletionText(completionCount, PrimaryColor, SecondaryColor, y);
                                                    ChallengeRewards.DrawRewards(templateIdToken.Value<string>(), rewardQuantity, y);
                                                    y += 90;
                                                }
                                            }
                                            else
                                            {
                                                rewardPath = FoldersUtility.FixFortnitePath(questDefinitionToken.Value<string>());

                                                DrawCompletionText(completionCount, PrimaryColor, SecondaryColor, y);
                                                ChallengeRewards.DrawRewards(rewardPath, rewardQuantity, y);
                                                y += 90;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void DrawCompletionText(string completionCount, SolidColorBrush PrimaryColor, SolidColorBrush SecondaryColor, int y)
        {
            Typeface typeface = new Typeface(TextsUtility.Burbank, FontStyles.Normal, FontWeights.Black, FontStretches.Normal);

            #region DESIGN
            IconCreator.ICDrawingContext.DrawRectangle(ChallengesUtility.DarkBrush(PrimaryColor, 0.3f), null, new Rect(0, y, 1024, 90));
            IconCreator.ICDrawingContext.DrawRectangle(PrimaryColor, null, new Rect(25, y, 1024 - 50, 70));

            Point dStart = new Point(32, y + 5);
            LineSegment[] dSegments = new[]
            {
                        new LineSegment(new Point(29, y + 67), true),
                        new LineSegment(new Point(1024 - 160, y + 62), true),
                        new LineSegment(new Point(1024 - 150, y + 4), true)
                    };
            PathFigure dFigure = new PathFigure(dStart, dSegments, true);
            PathGeometry dGeo = new PathGeometry(new[] { dFigure });
            IconCreator.ICDrawingContext.DrawGeometry(ChallengesUtility.LightBrush(PrimaryColor, 0.04f), null, dGeo);

            dStart = new Point(39, y + 35);
            dSegments = new[]
            {
                        new LineSegment(new Point(45, y + 32), true),
                        new LineSegment(new Point(48, y + 37), true),
                        new LineSegment(new Point(42, y + 40), true)
                    };
            dFigure = new PathFigure(dStart, dSegments, true);
            dGeo = new PathGeometry(new[] { dFigure });
            IconCreator.ICDrawingContext.DrawGeometry(SecondaryColor, null, dGeo);
            #endregion

            string all = "Complete ALL CHALLENGES to earn the reward item";
            string any = "Complete ANY " + completionCount + " CHALLENGES to earn the reward item";
            if (!string.Equals(FProp.Default.FLanguage, "English"))
            {
                all = AssetTranslations.SearchTranslation("AthenaChallengeDetailsEntry", "CompletionRewardFormat_All", "Complete ALL CHALLENGES to earn the reward item");
                any = AssetTranslations.SearchTranslation("AthenaChallengeDetailsEntry", "CompletionRewardFormat", "Complete ANY " + completionCount + " CHALLENGES to earn the reward item");

                #region FIX TRANSLATIONS
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
                        any = any.Replace("{QuestNumber}", completionCount);
                    }
                    else { any = doc.DocumentNode.InnerText.Replace("{QuestNumber}", completionCount); }
                }
                else
                {
                    if (any.Contains("</text>"))
                    {
                        any = doc.DocumentNode.InnerText.Replace(doc.DocumentNode.SelectSingleNode("text").InnerText, doc.DocumentNode.SelectSingleNode("text").InnerText.ToUpper());
                        any = string.Format(any, completionCount);
                    }
                    else { any = string.Format(doc.DocumentNode.InnerText, completionCount); }
                }

                if (all.Contains("  ")) { all = all.Replace("  ", " "); } //double space in Spanish (LA)               i.e. with QuestBundle_PirateParty
                if (any.Contains("  ")) { any = any.Replace("  ", " "); }
                #endregion
            }

            FormattedText formattedText =
                new FormattedText(
                    string.Equals(completionCount, "-1") ? all : any,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    30,
                    Brushes.White,
                    IconCreator.PPD
                    );
            formattedText.TextAlignment = TextAlignment.Left;
            formattedText.MaxTextWidth = 800;
            formattedText.MaxLineCount = 1;
            Point textLocation = new Point(60, y + 23);
            IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
        }
    }
}
