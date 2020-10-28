using System.Collections.Generic;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Textures
{
    public class TextureFormatHelper
    {
        private enum TargetBuffer
        {
            Color = 1,
            Depth = 2,
            Stencil = 3,
            DepthStencil = 4,
        }

        public static uint GetBlockWidth(EPixelFormat Format)
        {
            if (!HasFormatTableKey(Format)) return 8;
            return FormatTable[GetBaseFormat(Format)].BlockWidth;
        }

        public static uint GetBlockHeight(EPixelFormat Format)
        {
            if (!HasFormatTableKey(Format)) return 8;
            return FormatTable[GetBaseFormat(Format)].BlockHeight;
        }

        public static uint GetBytesPerPixel(EPixelFormat Format)
        {
            if (!HasFormatTableKey(Format)) return 8;
            return FormatTable[GetBaseFormat(Format)].BytesPerPixel;
        }

        public static uint GetBlockDepth(EPixelFormat Format)
        {
            if (!HasFormatTableKey(Format)) return 8;
            return FormatTable[GetBaseFormat(Format)].BlockDepth;
        }

        public static bool IsBCNCompressed(EPixelFormat Format)
        {
            return Format.ToString().StartsWith("BC");
        }

        public static bool HasFormatTableKey(EPixelFormat Format)
        {
            return FormatTable.ContainsKey(GetBaseFormat(Format));
        }

        private static string GetBaseFormat(EPixelFormat format)
        {
            string[] items = { "_UNORM", "_SRGB", "_SINT", "_SNORM", "_UINT", "_UFLOAT", "_SFLOAT", "_FLOAT", "H_SF16", "H_UF16" };
            string output = format.ToString();
            for (int i = 0; i < items.Length; i++)
                output = output.Replace(items[i], string.Empty);
            return output;
        }

        private static readonly Dictionary<string, FormatInfo> FormatTable =
                   new Dictionary<string, FormatInfo>()
         {
            { "RGBA32",    new FormatInfo(16, 1,  1, 1, TargetBuffer.Color) },
            { "RGBA16",    new FormatInfo(8, 1,  1, 1, TargetBuffer.Color) },
            { "RGBA8",     new FormatInfo(4, 1,  1, 1, TargetBuffer.Color) },
            { "RGB8",      new FormatInfo(3, 1,  1, 1, TargetBuffer.Color) },
            { "RGBA4",     new FormatInfo(2, 1,  1, 1, TargetBuffer.Color) },
            { "RGB32",     new FormatInfo(8, 1,  1, 1, TargetBuffer.Color) },
            { "RG32",      new FormatInfo(8, 1,  1, 1, TargetBuffer.Color) },
            { "RG16",      new FormatInfo(4, 1,  1, 1, TargetBuffer.Color) },
            { "RG8",       new FormatInfo(2, 1,  1, 1, TargetBuffer.Color) },
            { "RG4",       new FormatInfo(1, 1,  1, 1, TargetBuffer.Color) },
            { "R32",       new FormatInfo(4, 1,  1, 1, TargetBuffer.Color) },
            { "R16",       new FormatInfo(2, 1,  1, 1, TargetBuffer.Color) },
            { "R8",        new FormatInfo(1, 1,  1, 1, TargetBuffer.Color) },

            { "R32G8X24",  new FormatInfo(8, 1,  1, 1, TargetBuffer.Color) },
            { "RGBG8",     new FormatInfo(4, 1,  1, 1, TargetBuffer.Color) },
            { "BGRX8",     new FormatInfo(4, 1,  1, 1, TargetBuffer.Color) },
            { "BGR5A1",    new FormatInfo(2, 1,  1, 1, TargetBuffer.Color) },
            { "RGB5A1",    new FormatInfo(2, 1,  1, 1, TargetBuffer.Color) },
            { "BGRA8",     new FormatInfo(4, 1,  1, 1, TargetBuffer.Color) },

            { "R5G5B5",    new FormatInfo(2, 1,  1, 1, TargetBuffer.Color) },
            { "RGBB10A2",  new FormatInfo(4, 1,  1, 1, TargetBuffer.Color) },
            { "RG11B10",   new FormatInfo(4, 1,  1, 1, TargetBuffer.Color) },

            { "BGRA4",     new FormatInfo(2, 1,  1, 1, TargetBuffer.Color) },
            { "B5G6R5",    new FormatInfo(2, 1,  1, 1, TargetBuffer.Color) },

            { "BC1",      new FormatInfo(8,  4,  4, 1,  TargetBuffer.Color) },
            { "BC2",      new FormatInfo(16, 4,  4, 1,  TargetBuffer.Color) },
            { "BC3",      new FormatInfo(16, 4,  4, 1,  TargetBuffer.Color) },
            { "BC4",      new FormatInfo(8,  4,  4, 1,  TargetBuffer.Color) },
            { "BC5",      new FormatInfo(16, 4,  4, 1,  TargetBuffer.Color) },
            { "BC6",      new FormatInfo(16, 4,  4, 1,  TargetBuffer.Color) },
            { "BC7",      new FormatInfo(16, 4,  4, 1,  TargetBuffer.Color) },

            { "ASTC_4x4",              new FormatInfo(16, 4,  4, 1,  TargetBuffer.Color) },
            { "ASTC_5x4",              new FormatInfo(16, 5,  4, 1, TargetBuffer.Color) },
            { "ASTC_5x5",              new FormatInfo(16, 5,  5, 1,  TargetBuffer.Color) },
            { "ASTC_6x5",              new FormatInfo(16, 6,  5, 1, TargetBuffer.Color) },
            { "ASTC_6x6",              new FormatInfo(16, 6,  6, 1,  TargetBuffer.Color) },
            { "ASTC_8x5",              new FormatInfo(16, 8,  5,  1, TargetBuffer.Color) },
            { "ASTC_8x6",              new FormatInfo(16, 8,  6, 1, TargetBuffer.Color) },
            { "ASTC_8x8",              new FormatInfo(16, 8,  8, 1,  TargetBuffer.Color) },
            { "ASTC_10x5",             new FormatInfo(16, 10, 5, 1, TargetBuffer.Color) },
            { "ASTC_10x6",             new FormatInfo(16, 10, 6, 1, TargetBuffer.Color) },
            { "ASTC_10x8",             new FormatInfo(16, 10, 8, 1, TargetBuffer.Color) },
            { "ASTC_10x10",            new FormatInfo(16, 10, 10, 1, TargetBuffer.Color) },
            { "ASTC_12x10",            new FormatInfo(16, 12, 10, 1, TargetBuffer.Color) },
            { "ASTC_12x12",            new FormatInfo(16, 12, 12, 1, TargetBuffer.Color) },

            { "ETC1",                  new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { "ETC1_A4",                new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { "HIL08",                  new FormatInfo(16, 1, 1, 1, TargetBuffer.Color) },
            { "L4",                     new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { "LA4",                    new FormatInfo(4, 1, 1, 1, TargetBuffer.Color) },
            { "L8",                     new FormatInfo(8, 1, 1, 1, TargetBuffer.Color) },
            { "LA8",                    new FormatInfo(16, 1, 1, 1, TargetBuffer.Color) },
            { "A4",                     new FormatInfo(4, 1,  1, 1, TargetBuffer.Color) },
            { "A8",                     new FormatInfo(8,  1,  1, 1,  TargetBuffer.Color) },

            { "D16",                   new FormatInfo(2, 1, 1, 1, TargetBuffer.Depth) },
            { "D24S8",                 new FormatInfo(4, 1, 1, 1, TargetBuffer.Depth) },
            { "D32",                   new FormatInfo(4, 1, 1, 1, TargetBuffer.Depth) },
            { "D24_UNORM_S8",       new FormatInfo(8, 1, 1, 1, TargetBuffer.Depth) },
            { "D32_FLOAT_S8X24",       new FormatInfo(8, 1, 1, 1, TargetBuffer.Depth) },
            { "I4",                    new FormatInfo(4,  8, 8, 1, TargetBuffer.Color) },
            { "I8",                    new FormatInfo(8,  8, 4, 1, TargetBuffer.Color) },
            { "IA4",                   new FormatInfo(8,  8, 4, 1, TargetBuffer.Color) },
            { "IA8",                   new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { "RGB565",                new FormatInfo(2, 4, 4, 1, TargetBuffer.Color) },
            { "RGB5A3",                new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { "C4",                    new FormatInfo(4,  8, 8, 1, TargetBuffer.Color) },
            { "C8",                    new FormatInfo(8,  8, 4, 1, TargetBuffer.Color) },
            { "C14X2",                 new FormatInfo(16, 4, 4, 1, TargetBuffer.Color) },
            { "CMPR",                  new FormatInfo(4,  8, 8, 1, TargetBuffer.Color) }
        };

        class FormatInfo
        {
            public uint BytesPerPixel { get; private set; }
            public uint BlockWidth { get; private set; }
            public uint BlockHeight { get; private set; }
            public uint BlockDepth { get; private set; }

            public TargetBuffer TargetBuffer;

            public FormatInfo(uint bytesPerPixel, uint blockWidth, uint blockHeight, uint blockDepth, TargetBuffer targetBuffer)
            {
                BytesPerPixel = bytesPerPixel;
                BlockWidth = blockWidth;
                BlockHeight = blockHeight;
                BlockDepth = blockDepth;
                TargetBuffer = targetBuffer;
            }
        }
    }
}
