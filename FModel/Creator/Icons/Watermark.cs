using SkiaSharp;
using System.IO;

namespace FModel.Creator.Icons
{
    static class Watermark
    {
        public static void DrawWatermark(SKCanvas c)
        {
            if (Properties.Settings.Default.UseIconWatermark && !string.IsNullOrWhiteSpace(Properties.Settings.Default.IconWatermarkPath))
            {
                using SKBitmap watermarkBase = SKBitmap.Decode(new FileInfo(Properties.Settings.Default.IconWatermarkPath).Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                int sizeX = watermarkBase.Width * (int)Properties.Settings.Default.IconWatermarkScale / 512;
                int sizeY = watermarkBase.Height * (int)Properties.Settings.Default.IconWatermarkScale / 512;
                SKBitmap watermark = watermarkBase.Resize(sizeX, sizeY);

                float left = Properties.Settings.Default.IconWatermarkX;
                float top = Properties.Settings.Default.IconWatermarkY;
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
        }
    }
}
