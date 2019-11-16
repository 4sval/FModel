using FModel.Methods.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PakReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets.IconCreator
{
    static class Rarity
    {
        public static void DrawRarityBackground(JArray AssetProperties)
        {
            JToken serieToken = AssetsUtility.GetPropertyTagImport<JToken>(AssetProperties, "Series");
            JToken rarityToken = AssetsUtility.GetPropertyTag<JToken>(AssetProperties, "Rarity");

            if (AssetsLoader.ExportType == "FortAmmoItemDefinition")
            {
                DrawBackground(ImagesUtility.ParseColorFromHex("#6D6D6D"), ImagesUtility.ParseColorFromHex("#464646"), ImagesUtility.ParseColorFromHex("#9E9E9E"));
            }
            else if (serieToken != null)
            {
                GetSerieAsset(serieToken, rarityToken);
            }
            else
            {
                DrawNormalRarity(rarityToken);
            }
        }

        private static void DrawNormalRarity(JToken rarityToken)
        {
            switch (rarityToken != null ? rarityToken.Value<string>() : string.Empty)
            {
                case "EFortRarity::Transcendent":
                    DrawBackground(ImagesUtility.ParseColorFromHex("#D51944"), ImagesUtility.ParseColorFromHex("#86072D"), ImagesUtility.ParseColorFromHex("#FF3F58"));
                    break;
                case "EFortRarity::Mythic":
                    DrawBackground(ImagesUtility.ParseColorFromHex("#BA9C36"), ImagesUtility.ParseColorFromHex("#73581A"), ImagesUtility.ParseColorFromHex("#EED951"));
                    break;
                case "EFortRarity::Legendary":
                    DrawBackground(ImagesUtility.ParseColorFromHex("#C06A38"), ImagesUtility.ParseColorFromHex("#73331A"), ImagesUtility.ParseColorFromHex("#EC9650"));
                    break;
                case "EFortRarity::Epic":
                case "EFortRarity::Quality":
                    DrawBackground(ImagesUtility.ParseColorFromHex("#8138C2"), ImagesUtility.ParseColorFromHex("#421A73"), ImagesUtility.ParseColorFromHex("#B251ED"));
                    break;
                case "EFortRarity::Rare":
                    DrawBackground(ImagesUtility.ParseColorFromHex("#3669BB"), ImagesUtility.ParseColorFromHex("#1A4473"), ImagesUtility.ParseColorFromHex("#5180EE"));
                    break;
                case "EFortRarity::Common":
                    DrawBackground(ImagesUtility.ParseColorFromHex("#6D6D6D"), ImagesUtility.ParseColorFromHex("#464646"), ImagesUtility.ParseColorFromHex("#9E9E9E"));
                    break;
                default:
                    DrawBackground(ImagesUtility.ParseColorFromHex("#5EBC36"), ImagesUtility.ParseColorFromHex("#3C731A"), ImagesUtility.ParseColorFromHex("#74EF52"));
                    break;
            }
        }

        private static void DrawBackground(Color background, Color backgroundUpDown, Color border, bool series = false)
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

                    Point uStart = new Point(3, 3);
                    LineSegment[] uSegments = new[]
                    {
                        new LineSegment(new Point(3, 33), true),
                        new LineSegment(new Point(335, 3), true)
                    };
                    PathFigure uFigure = new PathFigure(uStart, uSegments, true);
                    PathGeometry uGeo = new PathGeometry(new[] { uFigure });

                    //border
                    if (!series)
                        IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(border), null, new Rect(0, 0, 515, 515));
                    else
                    {
                        LinearGradientBrush linearGradient = new LinearGradientBrush();
                        linearGradient.StartPoint = new Point(0, 1);
                        linearGradient.EndPoint = new Point(1, 0);
                        linearGradient.GradientStops.Add(new GradientStop(border, 0.3));
                        linearGradient.GradientStops.Add(new GradientStop(backgroundUpDown, 1.5));
                        linearGradient.Freeze();

                        IconCreator.ICDrawingContext.DrawRectangle(linearGradient, null, new Rect(0, 0, 515, 515));
                    }

                    //background
                    IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(background), null, new Rect(3, 3, 509, 509));
                    
                    //up & down
                    IconCreator.ICDrawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(125, backgroundUpDown.R, backgroundUpDown.G, backgroundUpDown.B)), null, uGeo);
                    IconCreator.ICDrawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(125, backgroundUpDown.R, backgroundUpDown.G, backgroundUpDown.B)), null, dGeo);
                    break;
                case "Default":
                case "Minimalist":
                    RadialGradientBrush radialGradient = new RadialGradientBrush();
                    radialGradient.GradientOrigin = new Point(0.5, 0.5);
                    radialGradient.Center = new Point(0.5, 0.5);

                    radialGradient.RadiusX = 0.5;
                    radialGradient.RadiusY = 0.5;

                    radialGradient.GradientStops.Add(new GradientStop(background, 0.0));
                    radialGradient.GradientStops.Add(new GradientStop(backgroundUpDown, 1.5));

                    // Freeze the brush (make it unmodifiable) for performance benefits.
                    radialGradient.Freeze();

                    //border
                    if (!series)
                        IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(border), null, new Rect(0, 0, 515, 515));
                    else
                    {
                        LinearGradientBrush linearGradient = new LinearGradientBrush();
                        linearGradient.StartPoint = new Point(0, 1);
                        linearGradient.EndPoint = new Point(1, 0);
                        linearGradient.GradientStops.Add(new GradientStop(border, 0.3));
                        linearGradient.GradientStops.Add(new GradientStop(backgroundUpDown, 1.5));
                        linearGradient.Freeze();

                        IconCreator.ICDrawingContext.DrawRectangle(linearGradient, null, new Rect(0, 0, 515, 515));
                    }

                    //background
                    IconCreator.ICDrawingContext.DrawRectangle(radialGradient, null, new Rect(3, 3, 509, 509));
                    break;
                default:
                    break;
            }
        }

        private static void GetSerieAsset(JToken serieToken, JToken rarityToken)
        {
            //this will catch the full path if asset exists to be able to grab his PakReader and List<FPakEntry>
            string seriesFullPath = AssetEntries.AssetEntriesDict.Where(x => x.Key.ToLowerInvariant().Contains("/" + serieToken.Value<string>().ToLowerInvariant() + ".uasset")).Select(d => d.Key).FirstOrDefault();
            if (!string.IsNullOrEmpty(seriesFullPath))
            {
                string path = seriesFullPath.Substring(0, seriesFullPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase));
                PakReader.PakReader reader = AssetsUtility.GetPakReader(path);
                if (reader != null)
                {
                    List<FPakEntry> entriesList = AssetsUtility.GetPakEntries(path);
                    string jsonData = AssetsUtility.GetAssetJsonData(reader, entriesList);

                    if (AssetsUtility.IsValidJson(jsonData))
                    {
                        dynamic AssetData = JsonConvert.DeserializeObject(jsonData);
                        JToken AssetMainToken = null;
                        if (jsonData.StartsWith("[") && jsonData.EndsWith("]"))
                        {
                            JArray AssetArray = JArray.FromObject(AssetData);
                            AssetMainToken = AssetArray[0];
                        }
                        else if (jsonData.StartsWith("{") && jsonData.EndsWith("}"))
                        {
                            AssetMainToken = AssetData;
                        }

                        if (AssetMainToken != null)
                        {
                            JArray propertiesArray = AssetMainToken["properties"].Value<JArray>();
                            if (propertiesArray != null)
                            {
                                JArray colorsArray = AssetsUtility.GetPropertyTagStruct<JArray>(propertiesArray, "Colors", "properties");
                                if (colorsArray != null)
                                {
                                    DrawSerieBackground(colorsArray);
                                }

                                JToken backgroundTextureToken = AssetsUtility.GetPropertyTagText<JToken>(propertiesArray, "BackgroundTexture", "asset_path_name");
                                if (backgroundTextureToken != null)
                                {
                                    string imagePath = FoldersUtility.FixFortnitePath(backgroundTextureToken.Value<string>());
                                    DrawSerieImage(imagePath);
                                }
                            }
                        }
                    }
                }
                else { DrawNormalRarity(rarityToken); }
            }
            else { DrawNormalRarity(rarityToken); }
        }

        private static void DrawSerieImage(string AssetPath)
        {
            using (Stream image = AssetsUtility.GetStreamImageFromPath(AssetPath))
            {
                if (image != null)
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = image;
                    bmp.EndInit();
                    bmp.Freeze();

                    IconCreator.ICDrawingContext.DrawImage(bmp, new Rect(3, 3, 509, 509));
                }
            }
            
        }

        private static void DrawSerieBackground(JArray colorsArray)
        {
            Color background = new Color();
            Color backgroundupdown = new Color();
            Color border = new Color();

            JToken backgroundRed = AssetsUtility.GetPropertyTagStruct<JToken>(colorsArray, "Color3", "r");
            JToken backgroundGreen = AssetsUtility.GetPropertyTagStruct<JToken>(colorsArray, "Color3", "g");
            JToken backgroundBlue = AssetsUtility.GetPropertyTagStruct<JToken>(colorsArray, "Color3", "b");
            if (backgroundRed != null && backgroundGreen != null && backgroundBlue != null)
            {
                int r = (int)(backgroundRed.Value<double>() * 255);
                int g = (int)(backgroundGreen.Value<double>() * 255);
                int b = (int)(backgroundBlue.Value<double>() * 255);

                background = Color.FromRgb((byte)r, (byte)g, (byte)b);
            }

            JToken backgroundupdownRed = AssetsUtility.GetPropertyTagStruct<JToken>(colorsArray, "Color1", "r");
            JToken backgroundupdownGreen = AssetsUtility.GetPropertyTagStruct<JToken>(colorsArray, "Color1", "g");
            JToken backgroundupdownBlue = AssetsUtility.GetPropertyTagStruct<JToken>(colorsArray, "Color1", "b");
            if (backgroundupdownRed != null && backgroundupdownGreen != null && backgroundupdownBlue != null)
            {
                int r = (int)(backgroundupdownRed.Value<double>() * 255);
                int g = (int)(backgroundupdownGreen.Value<double>() * 255);
                int b = (int)(backgroundupdownBlue.Value<double>() * 255);

                backgroundupdown = Color.FromRgb((byte)r, (byte)g, (byte)b);
            }

            JToken borderRed = AssetsUtility.GetPropertyTagStruct<JToken>(colorsArray, "Color2", "r");
            JToken borderGreen = AssetsUtility.GetPropertyTagStruct<JToken>(colorsArray, "Color2", "g");
            JToken borderBlue = AssetsUtility.GetPropertyTagStruct<JToken>(colorsArray, "Color2", "b");
            if (borderRed != null && borderGreen != null && borderBlue != null)
            {
                int r = (int)(borderRed.Value<double>() * 255);
                int g = (int)(borderGreen.Value<double>() * 255);
                int b = (int)(borderBlue.Value<double>() * 255);

                border = Color.FromRgb((byte)r, (byte)g, (byte)b);
            }

            DrawBackground(background, backgroundupdown, ChangeColorBrightness(border, 0.25f), true);
        }

        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = color.R;
            float green = color.G;
            float blue = color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromRgb((byte)red, (byte)green, (byte)blue);
        }
    }
}
