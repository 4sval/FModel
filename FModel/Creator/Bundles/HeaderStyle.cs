using FModel.Creator.Bases;
using FModel.Creator.Texts;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System;
using System.Linq;

namespace FModel.Creator.Bundles
{
    static class HeaderStyle
    {
        public static void DrawHeaderPaint(SKCanvas c, BaseBundle icon)
        {
            c.DrawRect(new SKRect(0, 0, icon.Width, icon.HeaderHeight), new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = icon.DisplayStyle.PrimaryColor
            });

            if (icon.DisplayStyle.CustomBackground != null && icon.DisplayStyle.CustomBackground.Height != icon.DisplayStyle.CustomBackground.Width)
            {
                icon.IsDisplayNameShifted = false;
                var bgPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High, BlendMode = SKBlendMode.Screen };
                if (Properties.Settings.Default.UseChallengeBanner) bgPaint.Color = SKColors.Transparent.WithAlpha((byte)Properties.Settings.Default.ChallengeBannerOpacity);
                c.DrawBitmap(icon.DisplayStyle.CustomBackground, new SKRect(0, 0, 1024, 256), bgPaint);
            }
            else if (icon.DisplayStyle.DisplayImage != null)
            {
                icon.IsDisplayNameShifted = true;
                if (icon.DisplayStyle.CustomBackground != null && icon.DisplayStyle.CustomBackground.Height == icon.DisplayStyle.CustomBackground.Width)
                    c.DrawBitmap(icon.DisplayStyle.CustomBackground, new SKRect(0, 0, icon.HeaderHeight, icon.HeaderHeight),
                        new SKPaint { 
                            IsAntialias = true, FilterQuality = SKFilterQuality.High, BlendMode = SKBlendMode.Screen,
                            ImageFilter = SKImageFilter.CreateDropShadow(2.5F, 0, 20, 0, icon.DisplayStyle.SecondaryColor.WithAlpha(25))
                        });

                c.DrawBitmap(icon.DisplayStyle.DisplayImage, new SKRect(0, 0, icon.HeaderHeight, icon.HeaderHeight),
                    new SKPaint {
                        IsAntialias = true, FilterQuality = SKFilterQuality.High,
                        ImageFilter = SKImageFilter.CreateDropShadow(-2.5F, 0, 20, 0, icon.DisplayStyle.SecondaryColor.WithAlpha(50))
                    });
            }

            SKPath pathTop = new SKPath { FillType = SKPathFillType.EvenOdd };
            pathTop.MoveTo(0, icon.HeaderHeight);
            pathTop.LineTo(icon.Width, icon.HeaderHeight);
            pathTop.LineTo(icon.Width, icon.HeaderHeight - 19);
            pathTop.LineTo(icon.Width / 2 + 7, icon.HeaderHeight - 23);
            pathTop.LineTo(icon.Width / 2 + 13, icon.HeaderHeight - 7);
            pathTop.LineTo(0, icon.HeaderHeight - 19);
            pathTop.Close();
            c.DrawPath(pathTop, new SKPaint {
                IsAntialias = true, FilterQuality = SKFilterQuality.High, Color = icon.DisplayStyle.SecondaryColor,
                ImageFilter = SKImageFilter.CreateDropShadow(-5, -5, 0, 0, icon.DisplayStyle.AccentColor.WithAlpha(75))
            });

            c.DrawRect(new SKRect(0, icon.HeaderHeight, icon.Width, icon.HeaderHeight + icon.AdditionalSize), new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = icon.DisplayStyle.PrimaryColor.WithAlpha(200) // default background is black, so i'm kinda lowering the brightness here and that's what i want
            });
        }

        public static void DrawHeaderText(SKCanvas c, BaseBundle icon)
        {
            using SKPaint paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = Text.TypeFaces.BundleDisplayNameTypeface,
                TextSize = 50,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Left,
            };

            string text = icon.DisplayName.ToUpper();
            int x = icon.IsDisplayNameShifted ? 300 : 50;
            if ((ELanguage)Properties.Settings.Default.AssetsLanguage == ELanguage.Arabic)
            {
                SKShaper shaper = new SKShaper(paint.Typeface);
                float shapedTextWidth;

                while (true)
                {
                    SKShaper.Result shapedText = shaper.Shape(text, paint);
                    shapedTextWidth = shapedText.Points[^1].X + paint.TextSize / 2f;

                    if (shapedTextWidth > (icon.Width - x))
                    {
                        paint.TextSize -= 1;
                    }
                    else
                    {
                        break;
                    }
                }
                if (text.Any(char.IsDigit))
                {
                    int s = text.Count(k => Char.IsDigit(k));
                    c.DrawShapedText(shaper, text.Substring(text.Length - s), x, 155, paint);

                    c.DrawShapedText(shaper, text.Substring(0, text.Length - s), x + 60, 155, paint);
                }
                else
                {
                    //feels bad man 
                    c.DrawShapedText(shaper, text, x, 155, paint);

                }
            }
            else
            {
                while (paint.MeasureText(text) > (icon.Width - x))
                {
                    paint.TextSize -= 2;
                }
                c.DrawText(text, x, 155, paint);
            }

            paint.Color = SKColors.White.WithAlpha(150);
            paint.TextAlign = SKTextAlign.Right;
            paint.TextSize = 23;
            paint.Typeface = Text.TypeFaces.DefaultTypeface;
            c.DrawText(icon.Watermark
                .Replace("{BundleName}", text)
                .Replace("{Date}", DateTime.Now.ToString("dd/MM/yyyy")),
                icon.Width - 25, icon.HeaderHeight - 40, paint);

            paint.Typeface = Text.TypeFaces.BundleDefaultTypeface;
            paint.Color = icon.DisplayStyle.SecondaryColor;
            paint.TextAlign = SKTextAlign.Left;
            paint.TextSize = 30;
            c.DrawText(icon.FolderName.ToUpper(), x, 95, paint);
        }
    }
}
