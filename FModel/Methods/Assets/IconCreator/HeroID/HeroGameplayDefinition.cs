using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PakReader;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FModel.Methods.Assets.IconCreator.HeroID
{
    class HeroGameplayDefinition
    {
        private static int _borderY = 518;
        private static int _textY = 550;
        private static int _imageY = 519;

        public static void GetHeroPerk(JArray AssetProperties)
        {
            JToken heroGameplayDefinitionToken = AssetsUtility.GetPropertyTagImport<JToken>(AssetProperties, "HeroGameplayDefinition");
            if (heroGameplayDefinitionToken != null)
            {
                string assetPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.ToLowerInvariant().Contains("/" + heroGameplayDefinitionToken.Value<string>().ToLowerInvariant() + ".")).Select(d => d.Key).FirstOrDefault();
                if (!string.IsNullOrEmpty(assetPath))
                {
                    PakReader.PakReader reader = AssetsUtility.GetPakReader(assetPath);
                    if (reader != null)
                    {
                        List<FPakEntry> entriesList = AssetsUtility.GetPakEntries(assetPath.Substring(0, assetPath.Length - ".uasset".Length));
                        string jsonData = AssetsUtility.GetAssetJsonData(reader, entriesList);

                        if (AssetsUtility.IsValidJson(jsonData))
                        {
                            JToken AssetMainToken = AssetsUtility.ConvertJson2Token(jsonData);
                            if (AssetMainToken != null)
                            {
                                JArray heroGameplayProperties = AssetMainToken["properties"].Value<JArray>();
                                if (heroGameplayProperties != null)
                                {
                                    _borderY = 518;
                                    _textY = 550;
                                    _imageY = 519;

                                    DrawHeroPerk(heroGameplayProperties);
                                    DrawTierAbilityKits(heroGameplayProperties);

                                    //RESIZE
                                    IconCreator.ICDrawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Size(515, 560 + 35 * 3)));
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void DrawHeroPerk(JArray AssetProperties)
        {
            JArray heroPerkArray = AssetsUtility.GetPropertyTagStruct<JArray>(AssetProperties, "HeroPerk", "properties");
            if (heroPerkArray != null)
            {
                JToken grantedAbilityKitToken = AssetsUtility.GetPropertyTagText<JToken>(heroPerkArray, "GrantedAbilityKit", "asset_path_name");
                if (grantedAbilityKitToken != null)
                {
                    string path = FoldersUtility.FixFortnitePath(grantedAbilityKitToken.Value<string>());
                    DrawAbilityKit(path);

                    JToken cRequirements_namespace = AssetsUtility.GetPropertyTagText<JToken>(heroPerkArray, "CommanderRequirementsText", "namespace");
                    JToken cRequirements_key = AssetsUtility.GetPropertyTagText<JToken>(heroPerkArray, "CommanderRequirementsText", "key");
                    JToken cRequirements_source_string = AssetsUtility.GetPropertyTagText<JToken>(heroPerkArray, "CommanderRequirementsText", "source_string");

                    if (cRequirements_namespace != null && cRequirements_key != null && cRequirements_source_string != null)
                    {
                        string cRequirements = AssetTranslations.SearchTranslation(cRequirements_namespace.Value<string>(), cRequirements_key.Value<string>(), cRequirements_source_string.Value<string>());

                        Typeface typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                        FormattedText formattedText =
                            new FormattedText(
                                cRequirements,
                                CultureInfo.CurrentUICulture,
                                FlowDirection.LeftToRight,
                                typeface,
                                13,
                                Brushes.White,
                                IconCreator.PPD
                                );
                        formattedText.TextAlignment = TextAlignment.Right;
                        formattedText.MaxTextWidth = 515;
                        formattedText.MaxLineCount = 1;

                        Point textLocation = new Point(-5, 543 - formattedText.Height);

                        IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
                    }
                }
            }
        }

        private static void DrawTierAbilityKits(JArray AssetProperties)
        {
            JArray tierAbilityDataArray = AssetsUtility.GetPropertyTagText<JArray>(AssetProperties, "TierAbilityKits", "data");
            if (tierAbilityDataArray != null)
            {
                foreach (JToken token in tierAbilityDataArray)
                {
                    JArray HeroTierAbilityArray = token["struct_type"]["properties"].Value<JArray>();
                    if (HeroTierAbilityArray != null)
                    {
                        JToken grantedAbilityKitToken = AssetsUtility.GetPropertyTagText<JToken>(HeroTierAbilityArray, "GrantedAbilityKit", "asset_path_name");
                        if (grantedAbilityKitToken != null)
                        {
                            string path = FoldersUtility.FixFortnitePath(grantedAbilityKitToken.Value<string>());
                            DrawAbilityKit(path);
                        }
                    }
                }
            }
        }

        private static void DrawAbilityKit(string assetPath)
        {
            if (!string.IsNullOrEmpty(assetPath))
            {
                PakReader.PakReader reader = AssetsUtility.GetPakReader(assetPath);
                if (reader != null)
                {
                    List<FPakEntry> entriesList = AssetsUtility.GetPakEntries(assetPath);
                    string jsonData = AssetsUtility.GetAssetJsonData(reader, entriesList);

                    if (AssetsUtility.IsValidJson(jsonData))
                    {
                        JToken AssetMainToken = AssetsUtility.ConvertJson2Token(jsonData);
                        if (AssetMainToken != null)
                        {
                            JArray abilityKitProperties = AssetMainToken["properties"].Value<JArray>();
                            if (abilityKitProperties != null)
                            {
                                JToken name_namespace = AssetsUtility.GetPropertyTagText<JToken>(abilityKitProperties, "DisplayName", "namespace");
                                JToken name_key = AssetsUtility.GetPropertyTagText<JToken>(abilityKitProperties, "DisplayName", "key");
                                JToken name_source_string = AssetsUtility.GetPropertyTagText<JToken>(abilityKitProperties, "DisplayName", "source_string");

                                JArray iconBrushArray = AssetsUtility.GetPropertyTagStruct<JArray>(abilityKitProperties, "IconBrush", "properties");
                                if (iconBrushArray != null)
                                {
                                    JToken resourceObjectToken = AssetsUtility.GetPropertyTagOuterImport<JToken>(iconBrushArray, "ResourceObject");
                                    if (resourceObjectToken != null)
                                    {
                                        string texturePath = FoldersUtility.FixFortnitePath(resourceObjectToken.Value<string>());
                                        using (Stream image = AssetsUtility.GetStreamImageFromPath(texturePath))
                                        {
                                            if (image != null)
                                            {
                                                BitmapImage bmp = new BitmapImage();
                                                bmp.BeginInit();
                                                bmp.CacheOption = BitmapCacheOption.OnLoad;
                                                bmp.StreamSource = image;
                                                bmp.EndInit();
                                                bmp.Freeze();

                                                //background
                                                IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(ImagesUtility.ParseColorFromHex("#6D6D6D")), null, new Rect(0, _borderY, 515, 34));

                                                if (name_namespace != null && name_key != null && name_source_string != null)
                                                {
                                                    string displayName = AssetTranslations.SearchTranslation(name_namespace.Value<string>(), name_key.Value<string>(), name_source_string.Value<string>());

                                                    Typeface typeface = new Typeface(TextsUtility.FBurbank, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                                                    FormattedText formattedText =
                                                        new FormattedText(
                                                            displayName.ToUpperInvariant(),
                                                            CultureInfo.CurrentUICulture,
                                                            FlowDirection.LeftToRight,
                                                            typeface,
                                                            25,
                                                            Brushes.White,
                                                            IconCreator.PPD
                                                            );
                                                    formattedText.TextAlignment = TextAlignment.Left;
                                                    formattedText.MaxTextWidth = 515;
                                                    formattedText.MaxLineCount = 1;

                                                    Point textLocation = new Point(50, _textY - formattedText.Height);

                                                    IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
                                                }

                                                IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(9, _imageY, 32, 32));

                                                _borderY += 37;
                                                _textY += 37;
                                                _imageY += 37;
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
    }
}
