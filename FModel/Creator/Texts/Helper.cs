using FModel.Creator.Bases;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace FModel.Creator.Texts
{
    static class Helper
    {
        public class Line
        {
            public string Value { get; set; }
            public float Width { get; set; }
        }

        public static void DrawCenteredMultilineText(SKCanvas canvas, string text, int maxLineCount, BaseIcon icon, ETextSide side, SKRect area, SKPaint paint)
            => DrawCenteredMultilineText(canvas, text, maxLineCount, icon.Size, icon.Margin, side, area, paint);
        public static void DrawCenteredMultilineText(SKCanvas canvas, string text, int maxLineCount, int size, int margin, ETextSide side, SKRect area, SKPaint paint)
        {
            float lineHeight = paint.TextSize * 1.2f;
            Line[] lines = SplitLines(text, paint, area.Width - margin);

            if (lines == null)
                return;
            if (lines.Length <= maxLineCount)
                maxLineCount = lines.Length;

            float height = maxLineCount * lineHeight;
            float y = area.MidY - height / 2;
            for (int i = 0; i < maxLineCount; i++)
            {
                y += lineHeight;
                float x = side switch
                {
                    ETextSide.Center => area.MidX - lines[i].Width / 2,
                    ETextSide.Right => size - margin - lines[i].Width,
                    ETextSide.Left => margin,
                    _ => area.MidX - lines[i].Width / 2
                };
                canvas.DrawText(lines[i].Value.TrimEnd(), x, y, paint);
            }
        }

        public static void DrawMultilineText(SKCanvas canvas, string text, int size, int margin, ETextSide side, SKRect area, SKPaint paint, out int yPos)
        {
            float lineHeight = paint.TextSize * 1.2f;
            Line[] lines = SplitLines(text, paint, area.Width);
            if (lines == null)
            {
                yPos = (int)area.Top;
                return;
            }

            float y = area.Top;
            for (int i = 0; i < lines.Length; i++)
            {
                float x = side switch
                {
                    ETextSide.Center => area.MidX - lines[i].Width / 2,
                    ETextSide.Right => size - margin - lines[i].Width,
                    ETextSide.Left => area.Left,
                    _ => area.MidX - lines[i].Width / 2
                };
                canvas.DrawText(lines[i].Value.TrimEnd(), x, y, paint);
                y += lineHeight;
            }
            yPos = (int)area.Top + ((int)lineHeight * lines.Length);
        }

        public static Line[] SplitLines(string text, SKPaint paint, float maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            float spaceWidth = paint.MeasureText(" ");
            string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            List<Line> ret = new List<Line>(lines.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                float width = 0;
                var lineResult = new StringBuilder();
                string[] words = lines[i].Split(' ', StringSplitOptions.None);
                foreach (var word in words)
                {
                    float wordWidth = paint.MeasureText(word);
                    float wordWithSpaceWidth = wordWidth + spaceWidth;
                    string wordWithSpace = word + " ";

                    if (width + wordWidth > maxWidth)
                    {
                        ret.Add(new Line { Value = lineResult.ToString(), Width = width });
                        lineResult = new StringBuilder(wordWithSpace);
                        width = wordWithSpaceWidth;
                    }
                    else
                    {
                        lineResult.Append(wordWithSpace);
                        width += wordWithSpaceWidth;
                    }
                }
                ret.Add(new Line { Value = lineResult.ToString(), Width = width });
            }
            return ret.ToArray();
        }
    }
}
