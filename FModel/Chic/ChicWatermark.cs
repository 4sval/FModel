using FModel.Creator;
using FModel.Logger;
using FModel.Properties;
using FModel.Utils;
using SkiaSharp;
using System;
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
                using SKBitmap watermarkBase = SKBitmap.Decode(Settings.Default.IconWatermarkPath);
                {
                    float sizeMultiplier = GetMultiplier(width, watermarkBase, sizePercent);
                    
                    float sizeX = watermarkBase.Width * sizeMultiplier;
                    float sizeY = watermarkBase.Height * sizeMultiplier;
                    SKBitmap watermark = watermarkBase.Resize((int)sizeX, (int)sizeY);

                    FConsole.AppendText(GetMultiplier(width, watermarkBase, sizePercent).ToString(), FColors.Green, true);
                    FConsole.AppendText(sizeMultiplier.ToString(), FColors.Green, true);
                    FConsole.AppendText(sizeX.ToString(), FColors.Green, true);
                    FConsole.AppendText(sizeY.ToString(), FColors.Green, true);
                    FConsole.AppendText(((int)sizeY).ToString(), FColors.Green, true);
                    FConsole.AppendText(((int)sizeY).ToString(), FColors.Green, true);

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

        static float GetMultiplier(int width, SKBitmap watermark, int size)
        {
            return (float)width / size / watermark.Width;
        }
    }
}
