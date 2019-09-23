using FModel.Methods.Utilities;
using Newtonsoft.Json.Linq;
using PakReader;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets.IconCreator
{
    class Rarity
    {
        public static void DrawRarityBackground(JArray AssetProperties)
        {
            JToken serieToken = AssetsUtility.GetPropertyTagImport<JToken>(AssetProperties, "Series");
            JToken rarityToken = AssetsUtility.GetPropertyTag<JToken>(AssetProperties, "Rarity");

            if (serieToken != null)
            {
                switch (serieToken.Value<string>())
                {
                    case "MarvelSeries":
                        DrawBackground(SKColor.Parse("#CB232D"), SKColor.Parse("#7F0E1D"), SKColor.Parse("#FF433D"));
                        break;
                    case "CUBESeries":
                        DrawBackground(SKColor.Parse("#9D006C"), SKColor.Parse("#610064"), SKColor.Parse("#AF1BB9"));
                        DrawSerieImage("/FortniteGame/Content/Athena/UI/Series/Art/DCU-Series/T-Cube-Background.uasset");
                        break;
                    case "DCUSeries":
                        DrawBackground(SKColor.Parse("#2D445D"), SKColor.Parse("#101928"), SKColor.Parse("#3E5E7A"));
                        DrawSerieImage("/FortniteGame/Content/Athena/UI/Series/Art/DCU-Series/T-BlackMonday-Background.uasset");
                        break;
                }
            }
            else
            {
                switch (rarityToken != null ? rarityToken.Value<string>() : string.Empty)
                {
                    case "EFortRarity::Transcendent":
                        DrawBackground(SKColor.Parse("#D51944"), SKColor.Parse("#86072D"), SKColor.Parse("#FF3F58"));
                        break;
                    case "EFortRarity::Mythic":
                        DrawBackground(SKColor.Parse("#BA9C36"), SKColor.Parse("#73581A"), SKColor.Parse("#EED951"));
                        break;
                    case "EFortRarity::Legendary":
                        DrawBackground(SKColor.Parse("#C06A38"), SKColor.Parse("#73331A"), SKColor.Parse("#EC9650"));
                        break;
                    case "EFortRarity::Epic":
                    case "EFortRarity::Quality":
                        DrawBackground(SKColor.Parse("#8138C2"), SKColor.Parse("#421A73"), SKColor.Parse("#B251ED"));
                        break;
                    case "EFortRarity::Rare":
                        DrawBackground(SKColor.Parse("#3669BB"), SKColor.Parse("#1A4473"), SKColor.Parse("#5180EE"));
                        break;
                    case "EFortRarity::Common":
                        DrawBackground(SKColor.Parse("#6D6D6D"), SKColor.Parse("#464646"), SKColor.Parse("#9E9E9E"));
                        break;
                    default:
                        DrawBackground(SKColor.Parse("#5EBC36"), SKColor.Parse("#3C731A"), SKColor.Parse("#74EF52"));
                        break;
                }
            }
        }

        private static void DrawBackground(SKColor background, SKColor backgroundUpDown, SKColor border)
        {
            switch (FProp.Default.FRarity_Design)
            {
                case "Flat":
                    using (SKPaint paint = new SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SKFilterQuality.High;

                        SKRect rect = new SKRect(0, 0, 515, 515);

                        paint.Style = SKPaintStyle.Fill;
                        paint.Color = background;
                        IconCreator.ICCanvas.DrawRect(rect, paint);

                        paint.Color = backgroundUpDown.WithAlpha((byte)(0xFF * 0.55));
                        SKPath path = new SKPath();
                        path.MoveTo(0, 440);
                        path.LineTo(515, 380);
                        path.LineTo(515, 380 + 135);
                        path.LineTo(0, 380 + 135);
                        path.LineTo(0, 440);
                        IconCreator.ICCanvas.DrawPath(path, paint);

                        path = new SKPath();
                        path.MoveTo(0, 0);
                        path.LineTo(0, 35);
                        path.LineTo(335, 0);
                        IconCreator.ICCanvas.DrawPath(path, paint);

                        paint.Style = SKPaintStyle.Stroke;
                        paint.Shader = null;
                        paint.Color = border;
                        paint.StrokeWidth = 6;
                        IconCreator.ICCanvas.DrawRect(rect, paint);
                    }
                    break;
                case "Default":
                case "Minimalist":
                    using (SKPaint paint = new SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SKFilterQuality.High;

                        SKRect rect = new SKRect(0, 0, 515, 515);

                        paint.Style = SKPaintStyle.Fill;
                        paint.Shader = SKShader.CreateRadialGradient(
                                            new SKPoint(rect.MidX, rect.MidY),
                                            500, //offset where backgroundUpDown will stop and background will fill the rest
                                            new SKColor[] { background, backgroundUpDown },
                                            null,
                                            SKShaderTileMode.Clamp);
                        IconCreator.ICCanvas.DrawRect(rect, paint);

                        paint.Style = SKPaintStyle.Stroke;
                        paint.Shader = null;
                        paint.Color = border;
                        paint.StrokeWidth = 6;
                        IconCreator.ICCanvas.DrawRect(rect, paint);
                    }
                    break;
            }
        }

        private static void DrawSerieImage(string AssetPath)
        {
            using (Stream image = AssetsUtility.GetStreamImageFromPath(AssetPath))
            {
                if (image != null)
                {
                    using (SKPaint paint = new SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SKFilterQuality.High;

                        SKRect rect = new SKRect(3, 3, 512, 512);
                        paint.Color = paint.Color.WithAlpha((byte)(0xFF * 0.4));
                        IconCreator.ICCanvas.DrawBitmap(SKBitmap.Decode(image), rect, paint);
                    }
                }
            }
            
        }
    }
}
