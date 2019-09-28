using Newtonsoft.Json.Linq;
using SkiaSharp;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets.IconCreator
{
    static class IconText
    {
        public static void DrawIconText(JArray AssetProperties)
        {
            DrawTextBackground();
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
