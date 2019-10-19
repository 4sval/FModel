using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PakReader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FModel.Methods.Assets.IconCreator.WeaponID
{
    class WeaponStats
    {
        private static JArray WeaponsStatsArray { get; set; }

        public static void DrawWeaponStats(string file, string rowname)
        {
            if (WeaponsStatsArray == null)
            {
                //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
                string filepath = AssetEntries.AssetEntriesDict.Where(x => x.Key.Contains("/" + file + ".uasset")).Select(d => d.Key).FirstOrDefault();
                if (!string.IsNullOrEmpty(filepath))
                {
                    PakReader.PakReader reader = AssetsUtility.GetPakReader(filepath.Substring(0, filepath.LastIndexOf(".")));
                    if (reader != null)
                    {
                        List<FPakEntry> entriesList = AssetsUtility.GetPakEntries(filepath.Substring(0, filepath.LastIndexOf(".")));
                        string jsonData = AssetsUtility.GetAssetJsonData(reader, entriesList, true);
                        if (AssetsUtility.IsValidJson(jsonData))
                        {
                            dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                            JArray AssetArray = JArray.FromObject(AssetData);
                            WeaponsStatsArray = AssetArray[0]["rows"].Value<JArray>();
                            SearchWeaponStats(rowname);
                        }
                    }
                }
            }
            else
            {
                SearchWeaponStats(rowname);
            }
        }

        private static void SearchWeaponStats(string rowname)
        {
            JArray weaponPropertiesArray = AssetsUtility.GetPropertyTagItemData<JArray>(WeaponsStatsArray, rowname, "properties");
            if (weaponPropertiesArray != null)
            {
                int count = 0;
                int borderY = 518;
                JToken ReloadTime = AssetsUtility.GetPropertyTag<JToken>(weaponPropertiesArray, "ReloadTime");
                JToken ClipSize = AssetsUtility.GetPropertyTag<JToken>(weaponPropertiesArray, "ClipSize");
                JToken DmgPB = AssetsUtility.GetPropertyTag<JToken>(weaponPropertiesArray, "DmgPB");

                if (ReloadTime != null)
                {
                    count++;
                    borderY += 37;
                    IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(ImagesUtility.ParseColorFromHex("#6D6D6D")), null, new Rect(0, borderY, 515, 34));

                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri("pack://application:,,,/Resources/reload64.png");
                    bmp.EndInit();
                    bmp.Freeze();

                    Typeface typeface = new Typeface(TextsUtility.FakeFNFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                    FormattedText formattedText =
                        new FormattedText(
                            $"{AssetTranslations.SearchTranslation("", "6EA26D1A4252034FBD869A90F9A6E49A", "Reload Time")} ({AssetTranslations.SearchTranslation("", "6BA53D764BA5CC13E821D2A807A72365", "seconds")}) : {ReloadTime.Value<string>()}".ToUpperInvariant(),
                            CultureInfo.CurrentUICulture,
                            FlowDirection.LeftToRight,
                            typeface,
                            25,
                            Brushes.White,
                            IconCreator.PPD
                            );
                    formattedText.TextAlignment = TextAlignment.Center;
                    formattedText.MaxTextWidth = 515;
                    formattedText.MaxLineCount = 1;

                    Point textLocation = new Point(0, 587 - formattedText.Height);

                    IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
                    IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(12, 560, 24, 24));
                }

                if (ClipSize != null)
                {
                    count++;
                    borderY += 37;
                    IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(ImagesUtility.ParseColorFromHex("#6D6D6D")), null, new Rect(0, borderY, 515, 34));

                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri("pack://application:,,,/Resources/clipSize64.png");
                    bmp.EndInit();
                    bmp.Freeze();

                    Typeface typeface = new Typeface(TextsUtility.FakeFNFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                    FormattedText formattedText =
                        new FormattedText(
                            $"{AssetTranslations.SearchTranslation("", "068239DD4327B36124498C9C5F61C038", "Magazine Size")} : {ClipSize.Value<string>()}".ToUpperInvariant(),
                            CultureInfo.CurrentUICulture,
                            FlowDirection.LeftToRight,
                            typeface,
                            25,
                            Brushes.White,
                            IconCreator.PPD
                            );
                    formattedText.TextAlignment = TextAlignment.Center;
                    formattedText.MaxTextWidth = 515;
                    formattedText.MaxLineCount = 1;

                    Point textLocation = new Point(0, 624 - formattedText.Height);

                    IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
                    IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(12, 598, 24, 24));
                }

                if (DmgPB != null)
                {
                    count++;
                    borderY += 37;
                    IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(ImagesUtility.ParseColorFromHex("#6D6D6D")), null, new Rect(0, borderY, 515, 34));

                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri("pack://application:,,,/Resources/dmg64.png");
                    bmp.EndInit();
                    bmp.Freeze();

                    Typeface typeface = new Typeface(TextsUtility.FakeFNFont, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                    FormattedText formattedText =
                        new FormattedText(
                            $"{AssetTranslations.SearchTranslation("", "BF7E3CF34A9ACFF52E95EAAD4F09F133", "Damage to Player")} : {DmgPB.Value<string>()}".ToUpperInvariant(),
                            CultureInfo.CurrentUICulture,
                            FlowDirection.LeftToRight,
                            typeface,
                            25,
                            Brushes.White,
                            IconCreator.PPD
                            );
                    formattedText.TextAlignment = TextAlignment.Center;
                    formattedText.MaxTextWidth = 515;
                    formattedText.MaxLineCount = 1;

                    Point textLocation = new Point(0, 661 - formattedText.Height);

                    IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
                    IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(12, 634, 24, 24));
                }

                //RESIZE
                IconCreator.ICDrawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Size(515, 560 + 35 * count)));
            }
        }
    }
}
