using FModel.Creator.Bases;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace FModel.Chic.Creator
{
    static class ChicImage
    {
        public static void DrawPreviewImage(SKCanvas c, IBase icon) =>
            c.DrawBitmap(icon.IconImage ?? icon.FallbackImage, new SKRect(icon.Margin, icon.Margin, icon.Width - icon.Margin, icon.Height - icon.Margin),
                new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 5, 5, SKColors.Black)
                });
    }
}
