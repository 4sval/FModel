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
        private static string _displayName;
        private static string _description;
        private static string _shortDescription;
        private static string _cosmeticSource;
        private static IEnumerable<JToken> _userFacingFlagsToken;

        public static void DrawIconText(JArray AssetProperties)
        {
            DrawTextBackground();

            SetTextVariables(AssetProperties);
            DrawTextVariables();
        }

        private static void SetTextVariables(JArray AssetProperties)
        {
            _displayName = string.Empty;
            _description = string.Empty;
            _shortDescription = string.Empty;
            _cosmeticSource = string.Empty;
            _userFacingFlagsToken = null;

            JToken name_namespace = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "DisplayName", "namespace");
            JToken name_key = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "DisplayName", "key");
            JToken name_source_string = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "DisplayName", "source_string");

            JToken description_namespace = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "Description", "namespace");
            JToken description_key = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "Description", "key");
            JToken description_source_string = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "Description", "source_string");

            JToken short_description_namespace = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "ShortDescription", "namespace");
            JToken short_description_key = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "ShortDescription", "key");
            JToken short_description_source_string = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "ShortDescription", "source_string");

            JArray gTagsArray = AssetsUtility.GetPropertyTagStruct<JArray>(AssetProperties, "GameplayTags", "gameplay_tags");

            if (name_namespace != null && name_key != null && name_source_string != null)
            {
                _displayName = AssetTranslations.SearchTranslation(name_namespace.Value<string>(), name_key.Value<string>(), name_source_string.Value<string>());
            }

            if (description_namespace != null && description_key != null && description_source_string != null)
            {
                _description = AssetTranslations.SearchTranslation(description_namespace.Value<string>(), description_key.Value<string>(), description_source_string.Value<string>());
            }

            if (short_description_namespace != null && short_description_key != null && short_description_source_string != null)
            {
                _shortDescription = AssetTranslations.SearchTranslation(short_description_namespace.Value<string>(), short_description_key.Value<string>(), short_description_source_string.Value<string>());
            }

            if (gTagsArray != null)
            {
                JToken cSetToken = gTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Cosmetics.Set."));
                if (cSetToken != null)
                {
                    string cosmeticSet = CosmeticSet.GetCosmeticSet(cSetToken.Value<string>());
                    if (!string.IsNullOrEmpty(cosmeticSet)) { _description += cosmeticSet; }
                }

                JToken cSourceToken = gTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Cosmetics.Source."));
                if (cSourceToken != null)
                {
                    _cosmeticSource = cSourceToken.Value<string>().Substring("Cosmetics.Source.".Length);
                }

                _userFacingFlagsToken = gTagsArray.Children<JToken>().Where(x => x.ToString().StartsWith("Cosmetics.UserFacingFlags."));
            }
        }

        private static void DrawTextVariables()
        {
            DrawDisplayName(_displayName);
            DrawDescription(_description);
            DrawToBottom("Left", _shortDescription);
            DrawToBottom("Right", _cosmeticSource);

            if (_userFacingFlagsToken != null)
            {
                foreach (JToken uFF in _userFacingFlagsToken)
                {
                    IconUserFacingFlags.DrawUserFacingFlag(uFF);
                }
                IconUserFacingFlags.xCoords = 4 - 25; //reset uFF coords
            }
        }

        private static void DrawDisplayName(string DisplayName)
        {
            Typeface typeface = new Typeface(TextsUtility.FakeFNFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            FormattedText formattedText =
                new FormattedText(
                    string.Equals(FProp.Default.FRarity_Design, "Minimalist") ? DisplayName.ToUpperInvariant() : DisplayName,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    string.Equals(FProp.Default.FRarity_Design, "Flat") || string.Equals(FProp.Default.FRarity_Design, "Minimalist") ? 50 : 45,
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
                string.Equals(FProp.Default.FRarity_Design, "Flat") ? new Point(-5, 450 - formattedText.Height) :
                string.Equals(FProp.Default.FRarity_Design, "Minimalist") ? new Point(0, 445 - formattedText.Height) :
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
                    string.Equals(FProp.Default.FRarity_Design, "Minimalist") ? 18 : 13,
                    Brushes.White,
                    IconCreator.PPD
                    );
            if (string.Equals(FProp.Default.FRarity_Design, "Flat"))
            {
                formattedText.TextAlignment = TextAlignment.Right;
                formattedText.MaxTextWidth = 515;
                formattedText.MaxLineCount = 3;
            }
            else
            {
                formattedText.TextAlignment = TextAlignment.Center;
                formattedText.MaxTextWidth = 515;
                formattedText.MaxLineCount = string.Equals(FProp.Default.FRarity_Design, "Minimalist") ? 3 : 4;
            }

            Point textLocation =
                string.Equals(FProp.Default.FRarity_Design, "Flat") ?
                new Point(-5, 465 - (5 * Description.Split('\n').Length)) : //(5 * Description.Split('\n').Length)
                new Point(0, 457 - (5 * Description.Split('\n').Length));   //^^^ home made horizontal alignment, this may not be 100% accurate

            IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
        }

        private static void DrawToBottom(string side, string text)
        {
            if (!string.Equals(FProp.Default.FRarity_Design, "Minimalist"))
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
