using FModel.Methods.Assets.IconCreator.ChallengeID;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PakReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace FModel.Methods.Utilities
{
    class ChallengesUtility
    {
        private static readonly Random _Random = new Random(Environment.TickCount);

        private static int RandomNext(int minValue, int maxValue)
        {
            return _Random.Next(minValue, maxValue);
        }
        public static SolidColorBrush RandomSolidColorBrush()
        {
            int blue = RandomNext(50, 200);
            int green = RandomNext(50, 200);
            int red = RandomNext(50, 200);

            return new SolidColorBrush(Color.FromRgb((byte)red, (byte)green, (byte)blue));
        }

        private static Color InterpolateColors(Color color1, Color color2, float percentage)
        {
            double a1 = color1.A / 255.0;
            double r1 = color1.R / 255.0;
            double g1 = color1.G / 255.0;
            double b1 = color1.B / 255.0;

            double a2 = color2.A / 255.0;
            double r2 = color2.R / 255.0;
            double g2 = color2.G / 255.0;
            double b2 = color2.B / 255.0;

            byte a3 = Convert.ToByte((a1 + (a2 - a1) * percentage) * 255);
            byte r3 = Convert.ToByte((r1 + (r2 - r1) * percentage) * 255);
            byte g3 = Convert.ToByte((g1 + (g2 - g1) * percentage) * 255);
            byte b3 = Convert.ToByte((b1 + (b2 - b1) * percentage) * 255);

            return Color.FromArgb(a3, r3, g3, b3);
        }

        public static SolidColorBrush LightBrush(SolidColorBrush brush, float percentage)
        {
            SolidColorBrush lighterBrush = new SolidColorBrush(brush.Color);
            lighterBrush.Color = InterpolateColors(brush.Color, ImagesUtility.ParseColorFromHex("#FFFFFF"), percentage);
            return lighterBrush;
        }
        public static SolidColorBrush DarkBrush(SolidColorBrush brush, float percentage)
        {
            SolidColorBrush darkerBrush = new SolidColorBrush(brush.Color);
            darkerBrush.Color = InterpolateColors(brush.Color, ImagesUtility.ParseColorFromHex("#000000"), percentage);
            return darkerBrush;
        }
        
        public static SolidColorBrush GetPrimaryColor(JArray displayStyleArray)
        {
            JToken pRedColorToken = AssetsUtility.GetPropertyTagStruct<JToken>(displayStyleArray, "PrimaryColor", "r");
            JToken pGreenColorToken = AssetsUtility.GetPropertyTagStruct<JToken>(displayStyleArray, "PrimaryColor", "g");
            JToken pBlueColorToken = AssetsUtility.GetPropertyTagStruct<JToken>(displayStyleArray, "PrimaryColor", "b");
            if (pRedColorToken != null && pGreenColorToken != null && pBlueColorToken != null)
            {
                int r = (int)(pRedColorToken.Value<double>() * 255);
                int g = (int)(pGreenColorToken.Value<double>() * 255);
                int b = (int)(pBlueColorToken.Value<double>() * 255);

                return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
            }
            return RandomSolidColorBrush();
        }
        public static SolidColorBrush GetSecondaryColor(JArray displayStyleArray, string lastfolder)
        {
            JToken sRedColorToken = AssetsUtility.GetPropertyTagStruct<JToken>(displayStyleArray, "SecondaryColor", "r");
            JToken sGreenColorToken = AssetsUtility.GetPropertyTagStruct<JToken>(displayStyleArray, "SecondaryColor", "g");
            JToken sBlueColorToken = AssetsUtility.GetPropertyTagStruct<JToken>(displayStyleArray, "SecondaryColor", "b");
            if (sRedColorToken != null && sGreenColorToken != null && sBlueColorToken != null)
            {
                int r = (int)(sRedColorToken.Value<double>() * 255);
                int g = (int)(sGreenColorToken.Value<double>() * 255);
                int b = (int)(sBlueColorToken.Value<double>() * 255);
                if (r + g + b <= 75 || string.Equals(lastfolder, "LTM"))
                {
                    JToken aRedColorToken = AssetsUtility.GetPropertyTagStruct<JToken>(displayStyleArray, "AccentColor", "r");
                    JToken aGreenColorToken = AssetsUtility.GetPropertyTagStruct<JToken>(displayStyleArray, "AccentColor", "g");
                    JToken aBlueColorToken = AssetsUtility.GetPropertyTagStruct<JToken>(displayStyleArray, "AccentColor", "b");
                    if (aRedColorToken != null && aGreenColorToken != null && aBlueColorToken != null)
                    {
                        r = (int)(aRedColorToken.Value<double>() * 255);
                        g = (int)(aGreenColorToken.Value<double>() * 255);
                        b = (int)(aBlueColorToken.Value<double>() * 255);

                        return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
                    }
                }
                else
                {
                    return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
                }
            }
            return RandomSolidColorBrush();
        }

        public static Stream GetChallengeBundleImage(JArray displayStyleArray)
        {
            JToken customBackgroundToken = AssetsUtility.GetPropertyTag<JToken>(displayStyleArray, "CustomBackground");
            JToken displayImageToken = AssetsUtility.GetPropertyTag<JToken>(displayStyleArray, "DisplayImage");
            if (customBackgroundToken != null && customBackgroundToken["asset_path_name"] != null && customBackgroundToken["asset_path_name"].Value<string>().EndsWith("_Details"))
            {
                string path = FoldersUtility.FixFortnitePath(customBackgroundToken["asset_path_name"].Value<string>());
                if (!string.IsNullOrEmpty(path))
                {
                    ChallengeIconDesign.isBanner = true;
                    return AssetsUtility.GetStreamImageFromPath(path);
                }
            }
            else if (displayImageToken != null && displayImageToken["asset_path_name"] != null)
            {
                string path = FoldersUtility.FixFortnitePath(displayImageToken["asset_path_name"].Value<string>());
                if (string.Equals(path, "/FortniteGame/Content/Athena/UI/Challenges/Art/TileImages/M_UI_ChallengeTile_PCB"))
                {
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
                                JArray renderSwitchProperties = AssetMainToken["properties"].Value<JArray>();
                                if (renderSwitchProperties != null)
                                {
                                    JArray textureParameterArray = AssetsUtility.GetPropertyTagText<JArray>(renderSwitchProperties, "TextureParameterValues", "data")[0]["struct_type"]["properties"].Value<JArray>();
                                    if (textureParameterArray != null)
                                    {
                                        JToken parameterValueToken = AssetsUtility.GetPropertyTagOuterImport<JToken>(textureParameterArray, "ParameterValue");
                                        if (parameterValueToken != null)
                                        {
                                            string texturePath = FoldersUtility.FixFortnitePath(parameterValueToken.Value<string>());
                                            return AssetsUtility.GetStreamImageFromPath(texturePath);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(path))
                {
                    return AssetsUtility.GetStreamImageFromPath(path);
                }
            }
            return Application.GetResourceStream(new Uri("pack://application:,,,/Resources/unknown512.png")).Stream;
        }
    }
}
