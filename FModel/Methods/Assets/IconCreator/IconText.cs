using FModel.Methods.Utilities;
using FModel.Properties;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System.IO;
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
            using (SKPaint paint = new SKPaint())
            {
                paint.IsAntialias = true;
                paint.FilterQuality = SKFilterQuality.High;
                paint.TextAlign = SKTextAlign.Center;
                paint.Color = SKColors.White;

                SKRect rect = new SKRect(3, 3, 512, 512);

                if (nameToken != null)
                {
                    paint.TextSize = 43;
                    paint.Typeface = SKTypeface.FromStream(new MemoryStream(Resources.BurbankBigCondensed_Bold), 0);
                    IconCreator.ICCanvas.DrawText(nameToken.Value<string>(), rect.MidX, 425, paint);
                }

                if (descriptionToken != null)
                {
                    paint.TextSize = 12;
                    paint.Typeface = SKTypeface.FromFamilyName("Arial");
                    TextsUtility.DrawText(IconCreator.ICCanvas, descriptionToken.Value<string>() + "\nPart of the FOV Slider set.", SKRect.Create(3, 438, 509, 59), paint);
                }

                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1;

                paint.Color = SKColors.Red;
                IconCreator.ICCanvas.DrawRect(SKRect.Create(3, 383, 509, 55), paint);
                paint.Color = SKColors.Blue;
                IconCreator.ICCanvas.DrawRect(SKRect.Create(3, 438, 509, 59), paint);
                paint.Color = SKColors.Yellow;
                IconCreator.ICCanvas.DrawRect(SKRect.Create(3, 497, 150, 15), paint);
                IconCreator.ICCanvas.DrawRect(SKRect.Create(362, 497, 150, 15), paint);
            }
        }

        private static void DrawTextBackground()
        {
            switch (FProp.Default.FRarity_Design)
            {
                case "Flat":
                    using (SKPaint paint = new SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SKFilterQuality.High;

                        paint.Color = SKColor.Parse("#45000032");
                        SKPath path = new SKPath();
                        path.MoveTo(3, 440);
                        path.LineTo(512, 380);
                        path.LineTo(512, 380 + 132);
                        path.LineTo(3, 380 + 132);
                        path.LineTo(3, 440);
                        IconCreator.ICCanvas.DrawPath(path, paint);
                    }
                    break;
                case "Default":
                case "Minimalist":
                    using (SKPaint paint = new SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SKFilterQuality.High;

                        SKRect rect = new SKRect(3, 383, 512, 512);

                        paint.Style = SKPaintStyle.Fill;
                        paint.Color = SKColor.Parse("#45000032");
                        IconCreator.ICCanvas.DrawRect(rect, paint);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
