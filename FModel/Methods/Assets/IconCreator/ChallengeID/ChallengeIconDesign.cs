using FModel.Methods.Utilities;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FModel.Methods.Assets.IconCreator.ChallengeID
{
    class ChallengeIconDesign
    {
        public static bool isBanner { get; set; }
        private static bool hasDisplayStyle { get; set; }

        public static void DrawChallenge(JArray AssetProperties, string lastfolder)
        {
            isBanner = false;
            hasDisplayStyle = false;
            SolidColorBrush PrimaryColor;
            SolidColorBrush SecondaryColor;
            Stream image = null;
            string displayName = string.Empty;

            JArray displayStyleArray = AssetsUtility.GetPropertyTagStruct<JArray>(AssetProperties, "DisplayStyle", "properties");
            if (displayStyleArray != null)
            {
                hasDisplayStyle = true;
                PrimaryColor = ChallengesUtility.GetPrimaryColor(displayStyleArray);
                SecondaryColor = ChallengesUtility.GetSecondaryColor(displayStyleArray, lastfolder);
                image = ChallengesUtility.GetChallengeBundleImage(displayStyleArray);
            }
            else
            {
                PrimaryColor = ChallengesUtility.RandomSolidColorBrush();
                SecondaryColor = ChallengesUtility.LightBrush(PrimaryColor, 0.4f);
            }

            JToken name_namespace = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "DisplayName", "namespace");
            JToken name_key = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "DisplayName", "key");
            JToken name_source_string = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "DisplayName", "source_string");
            if (name_namespace != null && name_key != null && name_source_string != null)
            {
                displayName = AssetTranslations.SearchTranslation(name_namespace.Value<string>(), name_key.Value<string>(), name_source_string.Value<string>());
            }

            DrawHeader(displayName, lastfolder, PrimaryColor, SecondaryColor, image);
            DrawQuests(PrimaryColor, SecondaryColor);
        }

        private static void DrawHeader(string displayName, string lastfolder, SolidColorBrush PrimaryColor, SolidColorBrush SecondaryColor, Stream image)
        {
            Point dStart = new Point(0, 256);
            LineSegment[] dSegments = new[]
            {
                        new LineSegment(new Point(1024, 256), true),
                        new LineSegment(new Point(1024, 241), true),
                        new LineSegment(new Point(537, 236), true),
                        new LineSegment(new Point(547, 249), true),
                        new LineSegment(new Point(0, 241), true)
                    };
            PathFigure dFigure = new PathFigure(dStart, dSegments, true);
            PathGeometry dGeo = new PathGeometry(new[] { dFigure });

            Typeface typeface = new Typeface(TextsUtility.OffFNFont, FontStyles.Normal, FontWeights.Black, FontStretches.Normal);
            FormattedText formattedText =
                new FormattedText(
                    displayName.ToUpperInvariant(),
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    55,
                    Brushes.White,
                    IconCreator.PPD
                    );
            formattedText.TextAlignment = TextAlignment.Left;
            formattedText.MaxTextWidth = 768;
            formattedText.MaxLineCount = 1;
            Point textLocation = new Point(isBanner || !hasDisplayStyle ? 50 : 310, 165 - formattedText.Height);

            IconCreator.ICDrawingContext.DrawRectangle(PrimaryColor, null, new Rect(0, 0, 1024, 256));
            if (image != null)
            {
                using (image)
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = image;
                    bmp.EndInit();
                    bmp.Freeze();

                    IconCreator.ICDrawingContext.DrawImage(isBanner ? ImagesUtility.CreateTransparency(bmp, 50) : bmp, new Rect(0, 0, isBanner ? 1024 : 256, 256));
                }
            }
            IconCreator.ICDrawingContext.DrawGeometry(SecondaryColor, null, dGeo);
            IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);

            formattedText =
                new FormattedText(
                    lastfolder.ToUpperInvariant(),
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    30,
                    SecondaryColor,
                    IconCreator.PPD
                    );
            formattedText.TextAlignment = TextAlignment.Left;
            formattedText.MaxTextWidth = 768;
            formattedText.MaxLineCount = 1;
            textLocation = new Point(isBanner || !hasDisplayStyle ? 50 : 310, 100 - formattedText.Height);
            Geometry geometry = formattedText.BuildGeometry(textLocation);
            Pen pen = new Pen(ChallengesUtility.DarkBrush(SecondaryColor, 0.3f), 1);
            IconCreator.ICDrawingContext.DrawGeometry(SecondaryColor, pen, geometry);
        }

        private static void DrawQuests(SolidColorBrush PrimaryColor, SolidColorBrush SecondaryColor)
        {
            LinearGradientBrush linGrBrush = new LinearGradientBrush();
            linGrBrush.StartPoint = new Point(0, 0);
            linGrBrush.EndPoint = new Point(0, 1);
            linGrBrush.GradientStops.Add(new GradientStop(Color.FromArgb(75, SecondaryColor.Color.R, SecondaryColor.Color.G, SecondaryColor.Color.B), 0));
            linGrBrush.GradientStops.Add(new GradientStop(Color.FromArgb(25, PrimaryColor.Color.R, PrimaryColor.Color.G, PrimaryColor.Color.B), 0.15));
            linGrBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));

            IconCreator.ICDrawingContext.DrawRectangle(ChallengesUtility.DarkBrush(PrimaryColor, 0.3f), null, new Rect(0, 256, 1024, 144));
            IconCreator.ICDrawingContext.DrawRectangle(linGrBrush, null, new Rect(0, 256, 1024, 144));

            Typeface typeface = new Typeface(TextsUtility.OffFNFont, FontStyles.Normal, FontWeights.Black, FontStretches.Normal);

            int y = 310;
            bool isBundleLevelup = false;
            bool isRequiresBattlePass = false;
            foreach (BundleInfosEntry entry in ChallengeBundleInfos.BundleData)
            {
                #region DESIGN

                #region UNLOCK TYPE
                if (!string.IsNullOrEmpty(entry.TheQuestUnlockType))
                {
                    switch (entry.TheQuestUnlockType)
                    {
                        case "EChallengeBundleQuestUnlockType::BundleLevelup":
                            if (!isBundleLevelup)
                            {
                                IconCreator.ICDrawingContext.DrawRectangle(ChallengesUtility.DarkBrush(PrimaryColor, 0.3f), null, new Rect(0, y, 1024, 40));
                                DrawUnlockType(PrimaryColor, SecondaryColor, "/FortniteGame/Content/UI/Foundation/Textures/Icons/Items/T-FNBR-MissionIcon-L", y);
                                isBundleLevelup = true;
                                y += 40;
                            }
                            break;
                        case "EChallengeBundleQuestUnlockType::RequiresBattlePass":
                            if (!isRequiresBattlePass)
                            {
                                IconCreator.ICDrawingContext.DrawRectangle(ChallengesUtility.DarkBrush(PrimaryColor, 0.3f), null, new Rect(0, y, 1024, 40));
                                DrawUnlockType(PrimaryColor, SecondaryColor, "/FortniteGame/Content/UI/Foundation/Textures/Icons/Items/T-FNBR-BattlePass-L", y);
                                isRequiresBattlePass = true;
                                y += 40;
                            }
                            break;
                    }
                }
                #endregion

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

                IconCreator.ICDrawingContext.DrawRectangle(SecondaryColor, null, new Rect(60, y + 47, 500, 7));

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

                #region DESCRIPTION
                FormattedText formattedText =
                    new FormattedText(
                        entry.TheQuestDescription,
                        CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        27,
                        Brushes.White,
                        IconCreator.PPD
                        );
                formattedText.TextAlignment = TextAlignment.Left;
                formattedText.MaxTextWidth = 800;
                formattedText.MaxLineCount = 1;
                Point textLocation = new Point(60, y + 15);
                IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
                #endregion

                #region COUNT
                formattedText =
                    new FormattedText(
                        "0 /",
                        CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        15,
                        new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                        IconCreator.PPD
                        );
                formattedText.TextAlignment = TextAlignment.Left;
                formattedText.MaxLineCount = 1;
                textLocation = new Point(565, y + 44);
                IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);

                formattedText =
                    new FormattedText(
                        entry.TheQuestCount.ToString(),
                        CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        15,
                        new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        IconCreator.PPD
                        );
                formattedText.TextAlignment = TextAlignment.Left;
                formattedText.MaxLineCount = 1;
                textLocation = new Point(584, y + 44);
                IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
                #endregion

                y += 90;
            }
        }

        private static void DrawUnlockType(SolidColorBrush PrimaryColor, SolidColorBrush SecondaryColor, string path, int y)
        {
            Stream image = AssetsUtility.GetStreamImageFromPath(path);
            if (image != null)
            {
                using (image)
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = image;
                    bmp.EndInit();
                    bmp.Freeze();

                    IconCreator.ICDrawingContext.DrawRectangle(ChallengesUtility.LightBrush(PrimaryColor, 0.04f), null, new Rect(20, y, 50, 35));

                    Point dStart = new Point(20, y);
                    LineSegment[] dSegments = new[]
                    {
                        new LineSegment(new Point(31, y), true),
                        new LineSegment(new Point(31, y + 20), true),
                        new LineSegment(new Point(25, y + 15), true),
                        new LineSegment(new Point(29, y + 35), true),
                        new LineSegment(new Point(20, y + 35), true),
                    };
                    PathFigure dFigure = new PathFigure(dStart, dSegments, true);
                    PathGeometry dGeo = new PathGeometry(new[] { dFigure });
                    IconCreator.ICDrawingContext.DrawGeometry(ChallengesUtility.DarkBrush(PrimaryColor, 0.3f), null, dGeo);

                    IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(50 - 15, y + 2, 32, 32));
                }
            }
        }
    }
}
