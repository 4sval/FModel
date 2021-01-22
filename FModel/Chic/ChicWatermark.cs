using FModel.Creator;
using FModel.Logger;
using FModel.Properties;
using FModel.Utils;
using SkiaSharp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace FModel.Chic
{
    static class ChicWatermark
    {
        public static void DrawWatermark(SKCanvas c, int width, int sizePercent = 4, bool shadow = false)
        {
            if (Settings.Default.UseIconWatermark && !string.IsNullOrEmpty(Settings.Default.IconWatermarkPath))
            {
                using SKBitmap watermarkBase = SKBitmap.Decode(new FileInfo(Settings.Default.IconWatermarkPath).Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                int sizeX = (int)(watermarkBase.Width * GetMultiplier(width, watermarkBase, sizePercent));
                int sizeY = (int)(watermarkBase.Height * GetMultiplier(width, watermarkBase, sizePercent));
                SKBitmap watermark = watermarkBase.Resize(sizeX, sizeY);

                float left = width - watermark.Width - 2;
                float top = 2;
                float right = left + watermark.Width;
                float bottom = top + watermark.Height;
                c.DrawBitmap(watermark, new SKRect(left, top, right, bottom),
                    new SKPaint
                    {
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true,
                        Color = SKColors.Transparent.WithAlpha((byte)Properties.Settings.Default.IconWatermarkOpacity)
                    });
            }

            static float GetMultiplier(int width, SKBitmap watermark, int size)
            {
                return (float)width / size / watermark.Width;
            }
        }
    }
}
