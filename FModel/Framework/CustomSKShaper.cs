using System;
using HarfBuzzSharp;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using Buffer = HarfBuzzSharp.Buffer;

namespace FModel.Framework
{
    public class CustomSKShaper : SKShaper
    {
        private const int _FONT_SIZE_SCALE = 512;
        private readonly Font _font;
        private readonly Buffer _buffer;

        public CustomSKShaper(SKTypeface typeface) : base(typeface)
        {
            using (var blob = Typeface.OpenStream(out var index).ToHarfBuzzBlob())
            using (var face = new Face(blob, index))
            {
                face.Index = index;
                face.UnitsPerEm = Typeface.UnitsPerEm;

                _font = new Font(face);
                _font.SetScale(_FONT_SIZE_SCALE, _FONT_SIZE_SCALE);
                _font.SetFunctionsOpenType();
            }

            _buffer = new Buffer();
        }

        public new Result Shape(Buffer buffer, float xOffset, float yOffset, SKPaint paint)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (paint == null)
                throw new ArgumentNullException(nameof(paint));

            // do the shaping
            _font.Shape(buffer);

            // get the shaping results
            var len = buffer.Length;
            var info = buffer.GlyphInfos;
            var pos = buffer.GlyphPositions;

            // get the sizes
            var textSizeY = paint.TextSize / _FONT_SIZE_SCALE;
            var textSizeX = textSizeY * paint.TextScaleX;

            var points = new SKPoint[len];
            var clusters = new uint[len];
            var codepoints = new uint[len];

            for (var i = 0; i < len; i++)
            {
                // move the cursor
                xOffset += pos[i].XAdvance * textSizeX;
                yOffset += pos[i].YAdvance * textSizeY;

                codepoints[i] = info[i].Codepoint;
                clusters[i] = info[i].Cluster;
                points[i] = new SKPoint(xOffset + pos[i].XOffset * textSizeX, yOffset - pos[i].YOffset * textSizeY);
            }

            return new Result(codepoints, clusters, points);
        }

        public new Result Shape(string text, SKPaint paint) => Shape(text, 0, 0, paint);

        public new Result Shape(string text, float xOffset, float yOffset, SKPaint paint)
        {
            if (string.IsNullOrEmpty(text))
                return new Result();

            using var buffer = new Buffer();
            switch (paint.TextEncoding)
            {
                case SKTextEncoding.Utf8:
                    buffer.AddUtf8(text);
                    break;
                case SKTextEncoding.Utf16:
                    buffer.AddUtf16(text);
                    break;
                case SKTextEncoding.Utf32:
                    buffer.AddUtf32(text);
                    break;
                default:
                    throw new NotSupportedException("TextEncoding of type GlyphId is not supported.");
            }

            buffer.GuessSegmentProperties();
            return Shape(buffer, xOffset, yOffset, paint);
        }
    }
}