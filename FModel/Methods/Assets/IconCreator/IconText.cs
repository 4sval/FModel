using FModel.Methods.Assets.IconCreator.AthenaID;
using FModel.Methods.Utilities;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets.IconCreator
{
    static class IconText
    {
        public static void DrawIconText(JArray AssetProperties)
        {
            DrawTextBackground();

            JToken nameToken = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "DisplayName", "source_string");
            JToken descriptionToken = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "Description", "source_string");
            JToken shortDescriptionToken = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "ShortDescription", "source_string");
            JArray gTagsArray = AssetsUtility.GetPropertyTagStruct<JArray>(AssetProperties, "GameplayTags", "gameplay_tags");

            if (nameToken != null)
            {
                DrawDisplayName(nameToken.Value<string>());
            }

            if (descriptionToken != null)
            {
                DrawDescription(descriptionToken.Value<string>());
            }

            if (gTagsArray != null)
            {
                JToken cSourceToken = gTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Cosmetics.Source."));
                if (cSourceToken != null)
                {
                    DrawToBottom("Right", cSourceToken.Value<string>().Substring("Cosmetics.Source.".Length));
                }

                IEnumerable<JToken> uFacingFlagsToken = gTagsArray.Children<JToken>().Where(x => x.ToString().StartsWith("Cosmetics.UserFacingFlags."));
                if (uFacingFlagsToken != null)
                {
                    foreach (JToken uFF in uFacingFlagsToken)
                    {
                        IconUserFacingFlags.DrawUserFacingFlag(uFF);
                    }
                    IconUserFacingFlags.xCoords = 4 - 25; //reset uFF coords
                }
            }

            if (shortDescriptionToken != null)
            {
                DrawToBottom("Left", shortDescriptionToken.Value<string>());
            }
        }

        private static void DrawDisplayName(string DisplayName)
        {
            Typeface typeface = new Typeface(TextsUtility.FakeFNFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            FormattedText formattedText =
                new FormattedText(
                    DisplayName,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    string.Equals(FProp.Default.FRarity_Design, "Flat") ? 50 : 45,
                    Brushes.White,
                    IconCreator.PPD
                    );
            if (string.Equals(FProp.Default.FRarity_Design, "Flat"))
            {
                formattedText.TextAlignment = TextAlignment.Right;
                formattedText.MaxTextWidth = 515;
                formattedText.MaxLineCount = 1;
            }
            else
            {
                formattedText.TextAlignment = TextAlignment.Center;
                formattedText.MaxTextWidth = 515;
                formattedText.MaxLineCount = 1;
            }

            Point textLocation =
                string.Equals(FProp.Default.FRarity_Design, "Flat") ?
                new Point(-5, 450 - formattedText.Height) :
                new Point(0, 435 - formattedText.Height);

            IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
        }

        private static void DrawDescription(string Description)
        {
            Typeface typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            FormattedText formattedText =
                new FormattedText(
                    Description,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    12,
                    Brushes.White,
                    IconCreator.PPD
                    );
            if (string.Equals(FProp.Default.FRarity_Design, "Flat"))
            {
                formattedText.TextAlignment = TextAlignment.Right;
                formattedText.MaxTextWidth = 515;
                formattedText.MaxLineCount = 4;
            }
            else
            {
                formattedText.TextAlignment = TextAlignment.Center;
                formattedText.MaxTextWidth = 515;
                formattedText.MaxLineCount = 4;
            }

            Point textLocation =
                string.Equals(FProp.Default.FRarity_Design, "Flat") ?
                new Point(-5, 462 - (5 * Description.Split('\n').Length)) : //(5 * Description.Split('\n').Length)
                new Point(0, 457 - (5 * Description.Split('\n').Length));   //^^^ home made horizontal alignment, this may not be 100% accurate

            IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
        }

        private static void DrawToBottom(string side, string text)
        {
            Typeface typeface = new Typeface(TextsUtility.FakeFNFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            FormattedText formattedText =
                new FormattedText(
                    text,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    14,
                    Brushes.White,
                    IconCreator.PPD
                    );

            Point textLocation = new Point();
            if (string.Equals(side, "Right"))
            {
                formattedText.TextAlignment = TextAlignment.Right;
                textLocation = new Point(510, 513 - formattedText.Height);
            }
            else if (string.Equals(side, "Left"))
            {
                formattedText.TextAlignment = TextAlignment.Left;
                textLocation = new Point(5, 513 - formattedText.Height);
            }
            IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
        }

        private static void DrawTextBackground()
        {
            switch (FProp.Default.FRarity_Design)
            {
                case "Flat":
                    Point dStart = new Point(3, 440);
                    LineSegment[] dSegments = new[]
                    {
                        new LineSegment(new Point(512, 380), true),
                        new LineSegment(new Point(512, 380 + 132), true),
                        new LineSegment(new Point(3, 380 + 132), true),
                        new LineSegment(new Point(3, 440), true)
                    };
                    PathFigure dFigure = new PathFigure(dStart, dSegments, true);
                    PathGeometry dGeo = new PathGeometry(new[] { dFigure });
                    IconCreator.ICDrawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(70, 0, 0, 50)), null, dGeo);
                    break;
                case "Default":
                case "Minimalist":
                    IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(70, 0, 0, 50)), null, new Rect(3, 380, 509, 132));
                    break;
                default:
                    break;
            }
        }
    }
}
