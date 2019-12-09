using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FModel.Methods.Assets.IconCreator.WeaponID
{
    static class IconAmmoData
    {
        public static void DrawIconAmmoData(string path)
        {
            string jsonData = AssetsUtility.GetAssetJsonDataByPath(path);
            if (jsonData != null && AssetsUtility.IsValidJson(jsonData))
            {
                dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                JToken AssetAmmo = JArray.FromObject(AssetData)[0];

                JToken largePreviewImage = AssetAmmo["properties"].Value<JArray>().FirstOrDefault(x => string.Equals(x["name"].Value<string>(), "LargePreviewImage"));
                JToken smallPreviewImage = AssetAmmo["properties"].Value<JArray>().FirstOrDefault(x => string.Equals(x["name"].Value<string>(), "SmallPreviewImage"));
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

                                //RESIZE
                                IconCreator.ICDrawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Size(515, 560)));

                                //background
                                IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(ImagesUtility.ParseColorFromHex("#6D6D6D")), null, new Rect(0, 518, 515, 34));

                                JToken name_namespace = AssetsUtility.GetPropertyTagText<JToken>(AssetAmmo["properties"].Value<JArray>(), "DisplayName", "namespace");
                                JToken name_key = AssetsUtility.GetPropertyTagText<JToken>(AssetAmmo["properties"].Value<JArray>(), "DisplayName", "key");
                                JToken name_source_string = AssetsUtility.GetPropertyTagText<JToken>(AssetAmmo["properties"].Value<JArray>(), "DisplayName", "source_string");
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
                                    formattedText.TextAlignment = TextAlignment.Center;
                                    formattedText.MaxTextWidth = 515;
                                    formattedText.MaxLineCount = 1;

                                    Point textLocation = new Point(0, 550 - formattedText.Height);

                                    IconCreator.ICDrawingContext.DrawText(formattedText, textLocation);
                                }

                                IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(9, 519, 32, 32));
                            }
                        }
                    }
                }
            }
        }
    }
}
