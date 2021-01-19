using FModel.Creator.Bases;
using FModel.Creator.Texts;
using SkiaSharp;
using System.Printing;

namespace FModel.Chic
{
    static class ChicText
    {
        private const int _STARTER_TEXT_POSITION = 380;
        private static int _BOTTOM_TEXT_SIZE = 15;
        private static int _NAME_TEXT_SIZE = 47;

        public static void DrawBackground(SKCanvas c, IBase icon)
        {
            var paint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = new SKColor(206, 155, 181),
                ImageFilter = SKImageFilter.CreateDropShadow(0, 3, 5, 5, SKColors.Black, null, new SKImageFilter.CropRect(SKRect.Create(icon.Margin, icon.Margin, icon.Width - icon.Margin, icon.Height - icon.Margin)))
            };

            var pathTop = new SKPath { FillType = SKPathFillType.EvenOdd };
            pathTop.MoveTo(icon.Margin, icon.Margin);
            pathTop.LineTo(icon.Width + icon.Margin, icon.Margin);
            pathTop.LineTo(icon.Width + icon.Margin, icon.Margin + 20);
            pathTop.LineTo(icon.Margin, icon.Margin + 30);
            pathTop.Close();
            c.DrawPath(pathTop, paint);

            var pathRarity = new SKPath { FillType = SKPathFillType.EvenOdd };
            pathRarity.MoveTo(icon.Margin, icon.Height - icon.Margin);
            pathRarity.LineTo(icon.Margin, icon.Height - icon.Margin - 75);
            pathRarity.LineTo(icon.Width - icon.Margin, icon.Height - icon.Margin - 85);
            pathRarity.LineTo(icon.Width - icon.Margin, icon.Height - icon.Margin);
            pathRarity.Close();
            c.DrawPath(pathRarity, new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = icon.RarityBackgroundColors[0],
                ImageFilter = SKImageFilter.CreateDropShadow(0, -3, 5, 5, SKColors.Black, null, new SKImageFilter.CropRect(SKRect.Create(icon.Margin, icon.Margin, icon.Width - icon.Margin, icon.Height - icon.Margin)))
            });

            var pathBottom = new SKPath { FillType = SKPathFillType.EvenOdd };
            pathBottom.MoveTo(icon.Margin, icon.Height - icon.Margin);
            pathBottom.LineTo(icon.Margin, icon.Height - icon.Margin - 65);
            pathBottom.LineTo(icon.Width - icon.Margin, icon.Height - icon.Margin - 75);
            pathBottom.LineTo(icon.Width - icon.Margin, icon.Height - icon.Margin);
            pathBottom.Close();
            c.DrawPath(pathBottom, new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Color = new SKColor(30, 30, 30),
            });
        }

        public static void DrawDisplayName(SKCanvas c, IBase icon)
        {
            string text = icon.DisplayName;

            if (string.IsNullOrEmpty(text)) return;

            int x = icon.Margin + 5;
            int y = _STARTER_TEXT_POSITION + _NAME_TEXT_SIZE;

            SKPaint namePaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High,
                Typeface = Text.TypeFaces.DisplayNameTypeface,
                TextSize = _NAME_TEXT_SIZE,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Left,
                ImageFilter = SKImageFilter.CreateDropShadow(0, 0, 5, 5, SKColors.Black)
            };

            while (namePaint.MeasureText(text) > icon.Width - icon.Margin * 2)
            {
                namePaint.TextSize--;
            }

            c.DrawText(text, x, y, namePaint);
        }
    }
}
