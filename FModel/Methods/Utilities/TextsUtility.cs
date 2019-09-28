using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FModel.Methods.Utilities
{
    static class TextsUtility
    {
        public static void DrawText(SKCanvas canvas, string text, SKRect area, SKPaint paint)
        {
            float lineHeight = paint.TextSize * 1.2f;
            Line[] lines = SplitLines(text, paint, area.Width);
            float height = lines.Count() * lineHeight;

            float y = area.MidY - height / 2;

            foreach (Line line in lines)
            {
                float x = area.MidX;
                canvas.DrawText(line.Value, x, y, paint);
                y += lineHeight;
            }
        }

        private static Line[] SplitLines(string text, SKPaint paint, float maxWidth)
        {
            if (text.Contains("\r")) { text = text.Replace("\r", string.Empty); }

            float spaceWidth = paint.MeasureText(" ");
            string[] lines = text.Split('\n');

            return lines.SelectMany((line) =>
            {
                List<Line> result = new List<Line>();

                string[] words = line.Split(new[] { " " }, StringSplitOptions.None);

                StringBuilder lineResult = new StringBuilder();
                float width = 0;
                foreach (string word in words)
                {
                    if (string.IsNullOrEmpty(word)) { continue; }

                    float wordWidth = paint.MeasureText(word);
                    float wordWithSpaceWidth = wordWidth + spaceWidth;
                    string wordWithSpace = word + " ";

                    if (width + wordWidth > maxWidth)
                    {
                        result.Add(new Line() { Value = lineResult.ToString(), Width = width });
                        lineResult = new StringBuilder(wordWithSpace);
                        width = wordWithSpaceWidth;
                    }
                    else
                    {
                        lineResult.Append(wordWithSpace);
                        width += wordWithSpaceWidth;
                    }
                }

                result.Add(new Line() { Value = lineResult.ToString(), Width = width });

                return result.ToArray();
            }).ToArray();
        }

        public class Line
        {
            public string Value { get; set; }

            public float Width { get; set; }
        }
    }
}
