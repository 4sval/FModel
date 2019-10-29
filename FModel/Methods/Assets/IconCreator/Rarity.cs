using FModel.Methods.Utilities;
using Newtonsoft.Json.Linq;
using System.IO;
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
                switch (serieToken.Value<string>())
                {
                    case "MarvelSeries":
                        DrawBackground(ImagesUtility.ParseColorFromHex("#CB232D"), ImagesUtility.ParseColorFromHex("#7F0E1D"), ImagesUtility.ParseColorFromHex("#FF433D"));
                        break;
                    case "CUBESeries":
                        DrawBackground(ImagesUtility.ParseColorFromHex("#9D006C"), ImagesUtility.ParseColorFromHex("#610064"), ImagesUtility.ParseColorFromHex("#AF1BB9"));
                        DrawSerieImage("/FortniteGame/Content/Athena/UI/Series/Art/DCU-Series/T-Cube-Background");
                        break;
                    case "DCUSeries":
                        DrawBackground(ImagesUtility.ParseColorFromHex("#2D445D"), ImagesUtility.ParseColorFromHex("#101928"), ImagesUtility.ParseColorFromHex("#3E5E7A"));
                        DrawSerieImage("/FortniteGame/Content/Athena/UI/Series/Art/DCU-Series/T-BlackMonday-Background");
                        break;
                    case "CreatorCollabSeries":
                        DrawBackground(ImagesUtility.ParseColorFromHex("#158588"), ImagesUtility.ParseColorFromHex("#073A4A"), ImagesUtility.ParseColorFromHex("#3FB3AA"));
                        DrawSerieImage("/FortniteGame/Content/Athena/UI/Series/Art/DCU-Series/T_Ui_CreatorsCollab_Bg");
                        break;
                    case "FrozenSeries":
                        DrawBackground(ImagesUtility.ParseColorFromHex("#5D9BC9"), ImagesUtility.ParseColorFromHex("#77C2E5"), ImagesUtility.ParseColorFromHex("#0296C9"));
                        DrawSerieImage("/FortniteGame/Content/Athena/UI/Series/Art/DCU-Series/T_Ui_LavaSeries_Frozen");
                        break;
                    case "LavaSeries":
                        DrawBackground(ImagesUtility.ParseColorFromHex("#5E0536"), ImagesUtility.ParseColorFromHex("#4D065F"), ImagesUtility.ParseColorFromHex("#A61835"));
                        DrawSerieImage("/FortniteGame/Content/Athena/UI/Series/Art/DCU-Series/T_Ui_LavaSeries_Bg");
                        break;
                    default:
                        DrawNormalRarity(rarityToken);
                        break;
                }
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

        private static void DrawBackground(Color background, Color backgroundUpDown, Color border)
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


                    //background + border
                    IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(border), null, new Rect(0, 0, 515, 515));
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

                    //background + border
                    IconCreator.ICDrawingContext.DrawRectangle(new SolidColorBrush(border), null, new Rect(0, 0, 515, 515));
                    IconCreator.ICDrawingContext.DrawRectangle(radialGradient, null, new Rect(3, 3, 509, 509));
                    break;
                default:
                    break;
            }
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

                    IconCreator.ICDrawingContext.DrawImage(ImagesUtility.CreateTransparency(bmp, 100), new Rect(3, 3, 509, 509));
                }
            }
            
        }
    }
}
