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
            if (icon.Margin > 0)
                c.DrawRect(new SKRect(0, 0, icon.Width + icon.Margin, icon.Height + icon.Margin),
                    new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High,
                        Color = new SKColor(20, 20, 20)
                    });

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
                        Shader = SKShader.CreateRadialGradient(
                            new SKPoint(icon.Width / 2, icon.Height / 2),
                            icon.Width / 5 * 4,
                            new SKColor[] {
                                new SKColor(30, 30, 30),
                                new SKColor(50, 50, 50)
                            },
                            SKShaderTileMode.Clamp)
                    });
            }
        }
    }
}
