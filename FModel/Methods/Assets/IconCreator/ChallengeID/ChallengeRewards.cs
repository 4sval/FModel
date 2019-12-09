using FModel.Methods.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FModel.Methods.Assets.IconCreator.ChallengeID
{
    static class ChallengeRewards
    {
        private const string UNKNOWN_ICON = "pack://application:,,,/Resources/unknown512.png";

        public static void DrawRewards(string path, string quantity, int y)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (path.Contains(":"))
                {
                    string[] parts = path.Split(':');
                    if (string.Equals(parts[0], "HomebaseBannerIcon", StringComparison.InvariantCultureIgnoreCase))
                    {
                        DrawBannerIcon(parts[1], y);
                    }
                    else
                    {
                        //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
                        string assetPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.ToLowerInvariant().Contains("/" + parts[1].ToLowerInvariant() + ".uasset")).Select(d => d.Key).FirstOrDefault();
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            DrawNormalIcon(assetPath.Substring(0, assetPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase)), quantity, y);
                        }
                    }
                }
                else
                {
                    switch (path)
                    {
                        case "/FortniteGame/Content/Items/PersistentResources/AthenaBattleStar": //mode 1
                            DrawNormalIcon(path, quantity, y, 1);
                            break;
                        case "/FortniteGame/Content/Items/PersistentResources/AthenaSeasonalXP": //mode 2
                            DrawNormalIcon(path, quantity, y, 2);
                            break;
                        case "/FortniteGame/Content/Items/Currency/MtxGiveaway": //mode 3
                            DrawNormalIcon(path, quantity, y, 3);
                            break;
                        default:
                            DrawNormalIcon(path, quantity, y);
                            break;
                    }
                }
            }
        }

        private static void DrawBannerIcon(string bannerName, int y)
        {
            //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
            string assetPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.Contains("/BannerIcons.uasset")).Select(d => d.Key).FirstOrDefault();
            if (!string.IsNullOrEmpty(assetPath))
            {
                string jsonData = AssetsUtility.GetAssetJsonDataByPath(assetPath.Substring(0, assetPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase)));
                if (jsonData != null && AssetsUtility.IsValidJson(jsonData))
                {
                    JToken AssetMainToken = AssetsUtility.ConvertJson2Token(jsonData);
                    if (AssetMainToken != null)
                    {
                        JArray propertiesArray = AssetMainToken["rows"].Value<JArray>();
                        if (propertiesArray != null)
                        {
                            JArray target = AssetsUtility.GetPropertyTagItemData<JArray>(propertiesArray, bannerName, "properties");
                            if (target != null)
                            {
                                JToken largeImage = target.FirstOrDefault(x => string.Equals(x["name"].Value<string>(), "LargeImage"));
                                JToken smallImage = target.FirstOrDefault(x => string.Equals(x["name"].Value<string>(), "SmallImage"));
                                if (largeImage != null || smallImage != null)
                                {
                                    JToken assetPathName =
                                        largeImage != null ? largeImage["tag_data"]["asset_path_name"] :
                                        smallImage != null ? smallImage["tag_data"]["asset_path_name"] : null;

                                    if (assetPathName != null)
                                    {
                                        string texturePath = FoldersUtility.FixFortnitePath(assetPathName.Value<string>());
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

                                                IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(902, y + 3, 64, 64));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = Application.GetResourceStream(new Uri(UNKNOWN_ICON)).Stream;
                bmp.EndInit();
                bmp.Freeze();
                IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(902, y + 3, 64, 64));
            }
        }

        private static void DrawNormalIcon(string path, string quantity, int y, int mode = 0)
        {
            string jsonData = AssetsUtility.GetAssetJsonDataByPath(path);
            if (jsonData != null)
            {
                if (AssetsUtility.IsValidJson(jsonData))
                {
                    JToken AssetMainToken = AssetsUtility.ConvertJson2Token(jsonData);
                    if (AssetMainToken != null)
                    {
                        JArray propertiesArray = AssetMainToken["properties"].Value<JArray>();
                        if (propertiesArray != null)
                        {
                            JToken heroToken = AssetsUtility.GetPropertyTagImport<JToken>(propertiesArray, "HeroDefinition");
                            JToken weaponToken = AssetsUtility.GetPropertyTagImport<JToken>(propertiesArray, "WeaponDefinition");
                            if (heroToken != null)
                            {
                                //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
                                string assetPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.ToLowerInvariant().Contains("/" + heroToken.Value<string>().ToLowerInvariant() + ".uasset")).Select(d => d.Key).FirstOrDefault();
                                if (!string.IsNullOrEmpty(assetPath))
                                {
                                    DrawImageFromTagData(assetPath.Substring(0, assetPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase)), quantity, y, mode);
                                }
                            }
                            else if (weaponToken != null)
                            {
                                string weaponName = weaponToken.Value<string>();
                                if (weaponToken.Value<string>().Equals("WID_Harvest_Pickaxe_STWCosmetic_Tier")) //STW PICKAXES MANUAL FIX
                                {
                                    weaponName = "WID_Harvest_Pickaxe_STWCosmetic_Tier_" + FWindow.FCurrentAsset.Substring(FWindow.FCurrentAsset.Length - 1);
                                }

                                //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
                                string assetPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.ToLowerInvariant().Contains("/" + weaponName.ToLowerInvariant() + ".uasset")).Select(d => d.Key).FirstOrDefault();
                                if (!string.IsNullOrEmpty(assetPath))
                                {
                                    DrawImageFromTagData(assetPath.Substring(0, assetPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase)), quantity, y, mode);
                                }
                            }
                            else
                            {
                                DrawLargeSmallImage(propertiesArray, quantity, y, mode);
                            }
                        }
                    }
                }
            }
            else
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = Application.GetResourceStream(new Uri(UNKNOWN_ICON)).Stream;
                bmp.EndInit();
                bmp.Freeze();
                IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(902, y + 3, 64, 64));
            }
        }

        private static void DrawImageFromTagData(string assetPath, string quantity, int y, int mode = 0)
        {
            string jsonData = AssetsUtility.GetAssetJsonDataByPath(assetPath);
            if (jsonData != null && AssetsUtility.IsValidJson(jsonData))
            {
                JToken AssetMainToken = AssetsUtility.ConvertJson2Token(jsonData);
                if (AssetMainToken != null)
                {
                    JArray AssetProperties = AssetMainToken["properties"].Value<JArray>();
                    DrawLargeSmallImage(AssetProperties, quantity, y, mode);
                }
            }
        }

        private static void DrawLargeSmallImage(JArray propertiesArray, string quantity, int y, int mode = 0)
        {
            JToken largePreviewImage = propertiesArray.FirstOrDefault(x => string.Equals(x["name"].Value<string>(), "LargePreviewImage"));
            JToken smallPreviewImage = propertiesArray.FirstOrDefault(x => string.Equals(x["name"].Value<string>(), "SmallPreviewImage"));
            if (largePreviewImage != null || smallPreviewImage != null)
            {
                JToken assetPathName =
                    largePreviewImage != null ? largePreviewImage["tag_data"]["asset_path_name"] :
                    smallPreviewImage != null ? smallPreviewImage["tag_data"]["asset_path_name"] : null;

                if (assetPathName != null)
                {
                    string texturePath = FoldersUtility.FixFortnitePath(assetPathName.Value<string>());
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

                            Rect rect = new Rect(902, y + 3, 64, 64);
                            if (mode > 0)
                            {
                                rect = new Rect(947, y + 12, 48, 48);

                                SolidColorBrush color = null;
                                SolidColorBrush border = null;
                                switch (mode)
                                {
                                    case 1:
                                        color = new SolidColorBrush(Color.FromRgb(255, 219, 103));
                                        border = new SolidColorBrush(Color.FromRgb(143, 74, 32));
                                        break;
                                    case 2:
                                        color = new SolidColorBrush(Color.FromRgb(230, 253, 177));
                                        border = new SolidColorBrush(Color.FromRgb(81, 131, 15));
                                        break;
                                    case 3:
                                        color = new SolidColorBrush(Color.FromRgb(220, 230, 255));
                                        border = new SolidColorBrush(Color.FromRgb(100, 160, 175));
                                        break;
                                }

                                Typeface typeface = new Typeface(TextsUtility.Burbank, FontStyles.Normal, FontWeights.Black, FontStretches.Normal);
                                FormattedText formattedText = 
                                    new FormattedText(
                                        quantity,
                                        CultureInfo.CurrentUICulture,
                                        FlowDirection.LeftToRight,
                                        typeface,
                                        30,
                                        color,
                                        IconCreator.PPD);

                                formattedText.TextAlignment = TextAlignment.Right;
                                formattedText.MaxTextWidth = 945;
                                formattedText.MaxLineCount = 1;
                                Point textLocation = new Point(0, y + 23);
                                Geometry geometry = formattedText.BuildGeometry(textLocation);
                                Pen pen = new Pen(border, 1);
                                pen.LineJoin = PenLineJoin.Round;
                                IconCreator.ICDrawingContext.DrawGeometry(color, pen, geometry);
                            }

                            IconCreator.ICDrawingContext.DrawImage(bmp, rect);
                        }
                    }
                }
            }
        }
    }
}
