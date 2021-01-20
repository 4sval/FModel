using FModel.Creator;
using FModel.Properties;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace FModel.Chic
{
    static class ChicWatermark
    {
        public static void DrawWatermark(SKCanvas c, int width, bool shadow = false, bool sizeByWidth)
        {
            if (Settings.Default.UseIconWatermark && !string.IsNullOrEmpty(Settings.Default.IconWatermarkPath))
            {
                using SKBitmap watermarkBase = SKBitmap.Decode(Settings.Default.IconWatermarkPath);
                {
                    int sizeMultiplier = sizeByWidth ? 0 : (int)Settings.Default.IconWatermarkScale / width;

                    int sizeX = watermarkBase.Width * sizeMultiplier;
                    int sizeY = watermarkBase.Height * sizeMultiplier;
                    SKBitmap watermark = watermarkBase.Resize(sizeX, sizeY);

                    float x = width - watermark.Width - 2;
                    float y = 2;
                    float w = x + watermark.Width;
                    float h = y + watermark.Height;
                    c.DrawBitmap(watermark, new SKRect(x, y, w, h),
                        new SKPaint
                        {
                            FilterQuality = SKFilterQuality.High,
                            IsAntialias = true,
                            Color = SKColors.Transparent.WithAlpha((byte)Settings.Default.IconWatermarkOpacity),
                            ImageFilter = shadow ? SKImageFilter.CreateDropShadow(0, 0, 2, 2, SKColors.Black) : null
                        });
                }
            }
        }
    }
}
