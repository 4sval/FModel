using FModel.Creator.Bases;
using FModel.PakReader.Parsers.PropertyTagData;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace FModel.Chic
{
    static class ChicRarity
    {
        public static void DrawRarity(SKCanvas c, IBase icon)
        {
            if (icon is BaseIcon i && i.RarityBackgroundImage != null)
                c.DrawBitmap(i.RarityBackgroundImage, new SKRect(icon.Margin, icon.Margin, icon.Width - icon.Margin, icon.Height - icon.Margin),
                    new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
            else
            {
                c.DrawRect(new SKRect(icon.Margin, icon.Margin, icon.Width - icon.Margin, icon.Height - icon.Margin),
                    new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High,
                        Color = icon.RarityBackgroundColors[0]
                    });

                var paint = new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    Color = icon.RarityBackgroundColors[1].WithAlpha(75)
                };
                var pathTop = new SKPath { FillType = SKPathFillType.EvenOdd };
                pathTop.MoveTo(icon.Margin, icon.Margin);
                pathTop.LineTo(icon.Margin + (icon.Width / 17 * 10), icon.Margin);
                pathTop.LineTo(icon.Margin, icon.Margin + (icon.Height / 17));
                pathTop.Close();
                c.DrawPath(pathTop, paint);

                var pathBottom = new SKPath { FillType = SKPathFillType.EvenOdd };
                pathBottom.MoveTo(icon.Margin, icon.Height - icon.Margin);
                pathBottom.LineTo(icon.Margin, icon.Height - icon.Margin - (icon.Height / 17 * 2.5f));
                pathBottom.LineTo(icon.Width - icon.Margin, icon.Height - icon.Margin - (icon.Height / 17 * 4.5f));
                pathBottom.LineTo(icon.Width - icon.Margin, icon.Height - icon.Margin);
                pathBottom.Close();
                c.DrawPath(pathBottom, paint);
            }
        }
    }
}
