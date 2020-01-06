using FModel.Methods.Assets.IconCreator.AthenaID;
using FModel.Methods.Assets.IconCreator.HeroID;
using FModel.Methods.Assets.IconCreator.WeaponID;
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
        private static string _cosmeticItemDefinition;
        private static string _itemAction;
        private static string _maxStackSize;
        private static string _miniMapIconBrushPath;
        private static IEnumerable<JToken> _userFacingFlagsToken;
        private static IEnumerable<JToken> _userHeroFlagsToken;

        public static void DrawIconText(JArray AssetProperties)
        {
            DrawTextBackground();

            SetTextVariables(AssetProperties);
            DrawTextVariables(AssetProperties);
        }

        private static void SetTextVariables(JArray AssetProperties)
        {
            _displayName = string.Empty;
            _description = string.Empty;
            _shortDescription = string.Empty;
            _cosmeticSource = string.Empty;
            _cosmeticItemDefinition = string.Empty;
            _itemAction = string.Empty;
            _maxStackSize = string.Empty;
            _userFacingFlagsToken = null;
            _userHeroFlagsToken = null;

            JToken name_namespace = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "DisplayName", "namespace");
            JToken name_key = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "DisplayName", "key");
            JToken name_source_string = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "DisplayName", "source_string");

            JToken description_namespace = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "Description", "namespace");
            JToken description_key = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "Description", "key");
            JToken description_source_string = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "Description", "source_string");

            JToken short_description_namespace = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "ShortDescription", "namespace");
            JToken short_description_key = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "ShortDescription", "key");
            JToken short_description_source_string = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "ShortDescription", "source_string");

            JToken cosmetic_item = AssetsUtility.GetPropertyTagImport<JToken>(AssetProperties, "cosmetic_item");
            JToken max_stack_size = AssetsUtility.GetPropertyTag<JToken>(AssetProperties, "MaxStackSize");
            JToken ammo_data = AssetsUtility.GetPropertyTagText<JToken>(AssetProperties, "AmmoData", "asset_path_name");

            JArray gTagsArray = AssetsUtility.GetPropertyTagStruct<JArray>(AssetProperties, "GameplayTags", "gameplay_tags");
            JArray hTagsArray = AssetsUtility.GetPropertyTagStruct<JArray>(AssetProperties, "RequiredGPTags", "gameplay_tags");
            JArray wTagsArray = AssetsUtility.GetPropertyTagStruct<JArray>(AssetProperties, "MiniMapIconBrush", "properties");

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
            else if (AssetsLoader.ExportType == "AthenaItemWrapDefinition")
            {
                _shortDescription = AssetTranslations.SearchTranslation("Fort.Cosmetics", "ItemWrapShortDescription", "Wrap");
            }

            if (cosmetic_item != null)
            {
                _cosmeticItemDefinition = cosmetic_item.Value<string>();
            }
            if (max_stack_size != null)
            {
                _maxStackSize = "Max Stack Size: " + max_stack_size.Value<string>();
            }
            if (ammo_data != null && ammo_data.Value<string>().Contains("Ammo"))
            {
                string path = FoldersUtility.FixFortnitePath(ammo_data.Value<string>());
                IconAmmoData.DrawIconAmmoData(path);

                JArray weapon_stat_handle = AssetsUtility.GetPropertyTagStruct<JArray>(AssetProperties, "WeaponStatHandle", "properties");
                if (weapon_stat_handle != null)
                {
                    JToken stats_file = AssetsUtility.GetPropertyTagImport<JToken>(weapon_stat_handle, "DataTable");
                    JToken row_name = AssetsUtility.GetPropertyTag<JToken>(weapon_stat_handle, "RowName");
                    if (stats_file != null && row_name != null)
                    {
                        WeaponStats.DrawWeaponStats(stats_file.Value<string>(), row_name.Value<string>());
                    }
                }
            }

            if (gTagsArray != null)
            {
                JToken cSetToken = gTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Cosmetics.Set."));
                if (cSetToken != null)
                {
                    string cosmeticSet = CosmeticSet.GetCosmeticSet(cSetToken.Value<string>());
                    if (!string.IsNullOrEmpty(cosmeticSet)) { _description += cosmeticSet; }
                }

                JToken cFilterToken = gTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Cosmetics.Filter.Season."));
                if (cFilterToken != null)
                {
                    string cosmeticFilter = CosmeticSeason.GetCosmeticSeason(cFilterToken.Value<string>().Substring("Cosmetics.Filter.Season.".Length));
                    if (!string.IsNullOrEmpty(cosmeticFilter)) { _description += cosmeticFilter; }
                }

                JToken cSourceToken = gTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Cosmetics.Source."));
                if (cSourceToken != null)
                {
                    _cosmeticSource = cSourceToken.Value<string>().Substring("Cosmetics.Source.".Length);
                }

                JToken cActionToken = gTagsArray.Children<JToken>().FirstOrDefault(x => x.ToString().StartsWith("Athena.ItemAction."));
                if (cActionToken != null)
                {
                    _itemAction = cActionToken.Value<string>().Substring("Athena.ItemAction.".Length);
                }

                _userFacingFlagsToken = gTagsArray.Children<JToken>().Where(x => x.ToString().StartsWith("Cosmetics.UserFacingFlags."));
            }

            if (hTagsArray != null)
                _userHeroFlagsToken = hTagsArray.Children<JToken>().Where(x => x.ToString().StartsWith("Unlocks.Class."));

            if (wTagsArray != null)
            {
                JToken resourceObjectToken = AssetsUtility.GetPropertyTagOuterImport<JToken>(wTagsArray, "ResourceObject");
                if (resourceObjectToken != null)
                    _miniMapIconBrushPath = FoldersUtility.FixFortnitePath(resourceObjectToken.Value<string>());
            }
        }

        private static void DrawTextVariables(JArray AssetProperties)
        {
            DrawDisplayName(_displayName);
            DrawDescription(_description);

            switch (AssetsLoader.ExportType)
            {
                case "AthenaBackpackItemDefinition":
                case "AthenaBattleBusItemDefinition":
                case "AthenaCharacterItemDefinition":
                case "AthenaConsumableEmoteItemDefinition":
                case "AthenaSkyDiveContrailItemDefinition":
                case "AthenaDanceItemDefinition":
                case "AthenaEmojiItemDefinition":
                case "AthenaGliderItemDefinition":
                case "AthenaItemWrapDefinition":
                case "AthenaLoadingScreenItemDefinition":
                case "AthenaMusicPackItemDefinition":
                case "AthenaPetCarrierItemDefinition":
                case "AthenaPickaxeItemDefinition":
                case "AthenaSprayItemDefinition":
                case "AthenaToyItemDefinition":
                case "AthenaVictoryPoseItemDefinition":
                case "FortBannerTokenType":
                    DrawToBottom("Left", _shortDescription);
                    DrawToBottom("Right", _cosmeticSource);
                    break;
                case "FortWeaponRangedItemDefinition":
                case "AthenaGadgetItemDefinition":
                    DrawToBottom("Left", _maxStackSize);
                    DrawToBottom("Right", _itemAction);
                    break;
                case "FortVariantTokenType":
                    DrawToBottom("Left", _shortDescription);
                    DrawToBottom("Right", _cosmeticItemDefinition);
                    break;
                case "FortHeroType":
                    HeroGameplayDefinition.GetHeroPerk(AssetProperties);
                    break;
            }

            if (_userFacingFlagsToken != null)
            {
                foreach (JToken uFF in _userFacingFlagsToken)
                {
                    IconUserFacingFlags.DrawUserFacingFlag(uFF);
                }
                IconUserFacingFlags.xCoords = 4 - 25; //reset uFF coords
            }

            if (_userHeroFlagsToken != null)
            {
                foreach (JToken uFF in _userHeroFlagsToken)
                {
                    IconUserFacingFlags.DrawHeroFacingFlag(uFF);
                }
                IconUserFacingFlags.xCoords = 4 - 25; //reset uFF coords
            }

            if (!string.IsNullOrEmpty(_miniMapIconBrushPath))
                using (System.IO.Stream image = AssetsUtility.GetStreamImageFromPath(_miniMapIconBrushPath))
                {
                    if (image != null)
                    {
                        System.Windows.Media.Imaging.BitmapImage bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.StreamSource = image;
                        bmp.EndInit();
                        bmp.Freeze();

                        IconUserFacingFlags.xCoords += 25;
                        IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(IconUserFacingFlags.xCoords, 4, 25, 25));
                        IconUserFacingFlags.xCoords = 4 - 25; //reset uFF coords
                    }
                }
        }

        private static void DrawDisplayName(string DisplayName)
        {
            Typeface typeface = new Typeface(TextsUtility.FBurbank, FontStyles.Normal, string.Equals(FProp.Default.FLanguage, "Japanese") ? FontWeights.Black : FontWeights.Normal, FontStretches.Normal);
            double size = string.Equals(FProp.Default.FRarity_Design, "Flat") || string.Equals(FProp.Default.FRarity_Design, "Minimalist") ? 50 : 45;

            FormattedText formattedText =
                new FormattedText(
                    string.Equals(FProp.Default.FRarity_Design, "Minimalist") || string.Equals(FProp.Default.FLanguage, "Russian") ? DisplayName.ToUpperInvariant() : DisplayName,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    size,
                    Brushes.White,
                    IconCreator.PPD
                    );
            if (string.Equals(FProp.Default.FRarity_Design, "Flat"))
            {
                formattedText.TextAlignment = TextAlignment.Right;
                formattedText.MaxLineCount = 1;
            }
            else
            {
                formattedText.TextAlignment = TextAlignment.Center;
                formattedText.MaxLineCount = 1;
            }

            while (formattedText.Width > 515)
            {
                size -= 1;
                formattedText.SetFontSize(size);
            }

            Point textLocation =
                string.Equals(FProp.Default.FRarity_Design, "Flat") ? new Point(510, 450 - formattedText.Height) :
                string.Equals(FProp.Default.FRarity_Design, "Minimalist") ? new Point(515 / 2, 445 - formattedText.Height) :
                new Point(515 / 2, 435 - formattedText.Height);

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
                Typeface typeface = new Typeface(TextsUtility.FBurbank, FontStyles.Normal, string.Equals(FProp.Default.FLanguage, "Japanese") ? FontWeights.Black : FontWeights.Normal, FontStretches.Normal);

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
                    textLocation = new Point(509, 512 - formattedText.Height);
                }
                else if (string.Equals(side, "Left"))
                {
                    formattedText.TextAlignment = TextAlignment.Left;
                    textLocation = new Point(6, 512 - formattedText.Height);
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
                case "Accurate Colors":
                    IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(70, 0, 0, 50)), null, new Rect(3, 380, 509, 132));
                    break;
                default:
                    break;
            }
        }
    }
}
