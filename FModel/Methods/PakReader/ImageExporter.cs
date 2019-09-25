﻿using System;
using System.IO;
using SkiaSharp;
using static PakReader.AssetReader;

namespace PakReader
{
    static class ImageExporter
    {
        public static SKImage GetImage(FTexture2DMipMap inp, string pixel_format)
        {
            byte[] decoded;
            SKColorType color;
            switch (pixel_format)
            {
                case "PF_DXT5":
                    decoded = DDSImage.DecompressDXT5(inp.data.data, DDSImage.PixelFormat.DXT5, (uint)inp.size_x, (uint)inp.size_y, (uint)inp.size_z);
                    color = SKColorType.Rgba8888;
                    break;
                case "PF_DXT1":
                    decoded = DDSImage.DecompressDXT1(inp.data.data, DDSImage.PixelFormat.DXT1, (uint)inp.size_x, (uint)inp.size_y, (uint)inp.size_z);
                    color = SKColorType.Rgba8888;
                    break;
                case "PF_B8G8R8A8":
                    decoded = inp.data.data;
                    color = SKColorType.Bgra8888;
                    break;
                case "PF_BC5":
                    decoded = DecodeBC5(inp.data.data, inp.size_x, inp.size_y);
                    color = SKColorType.Rgb888x;
                    break;
                case "PF_BC4":
                    decoded = DecodeBC4(inp.data.data, inp.size_x, inp.size_y);
                    color = SKColorType.Rgb888x;
                    break;
                case "PF_G8":
                    decoded = inp.data.data;
                    color = SKColorType.Gray8;
                    break;
                case "PF_FloatRGBA":
                    decoded = inp.data.data;
                    color = SKColorType.RgbaF16;
                    break;
                default:
                    throw new IOException("Unknown image type: " + pixel_format);
            }
            var info = new SKImageInfo(inp.size_x, inp.size_y, color, SKAlphaType.Unpremul);
            using (SKBitmap bitmap = new SKBitmap(info))
            {
                unsafe
                {
                    fixed (byte* p = decoded)
                    {
                        bitmap.SetPixels(new IntPtr(p));
                    }
                }
                return SKImage.FromBitmap(bitmap);
            }
        }

        static byte[] DecodeBC4(byte[] inp, int width, int height)
        {
            byte[] ret = new byte[width * height * 4];
            BinaryReader reader = new BinaryReader(new MemoryStream(inp));
            for (int y_block = 0; y_block < height / 4; y_block++)
            {
                for (int x_block = 0; x_block < width / 4; x_block++)
                {
                    var r_bytes = DecodeBC3Block(reader);
                    for (int i = 0; i < 16; i++)
                    {
                        ret[GetPixelLoc(width, x_block * 4 + (i % 4), y_block * 4 + (i / 4), 4, 0)] = r_bytes[i];
                    }
                }
            }
            return ret;
        }

        static byte[] DecodeBC5(byte[] inp, int width, int height)
        {
            byte[] ret = new byte[width * height * 4];
            BinaryReader reader = new BinaryReader(new MemoryStream(inp));
            for (int y_block = 0; y_block < height / 4; y_block++)
            {
                for (int x_block = 0; x_block < width / 4; x_block++)
                {
                    var r_bytes = DecodeBC3Block(reader);
                    var g_bytes = DecodeBC3Block(reader);

                    for (int i = 0; i < 16; i++)
                    {
                        ret[GetPixelLoc(width, x_block * 4 + (i % 4), y_block * 4 + (i / 4), 4, 0)] = r_bytes[i];
                        ret[GetPixelLoc(width, x_block * 4 + (i % 4), y_block * 4 + (i / 4), 4, 1)] = g_bytes[i];
                        ret[GetPixelLoc(width, x_block * 4 + (i % 4), y_block * 4 + (i / 4), 4, 2)] = GetZNormal(r_bytes[i], g_bytes[i]);
                    }
                }
            }
            return ret;
        }

        static int GetPixelLoc(int width, int x, int y, int bpp, int off) => (y * width + x) * bpp + off;

        static byte GetZNormal(byte x, byte y)
        {
            var xf = (x / 127.5f) - 1;
            var yf = (y / 127.5f) - 1;
            var zval = 1 - xf * xf - yf * yf;
            var zval_ = (float)Math.Sqrt(zval > 0 ? zval : 0);
            zval = zval_ < 1 ? zval_ : 1;
            return (byte)((zval * 127) + 128);
        }

        static byte[] DecodeBC3Block(BinaryReader reader)
        {
            float ref0 = reader.ReadByte();
            float ref1 = reader.ReadByte();

            float[] ref_sl = new float[8];
            ref_sl[0] = ref0;
            ref_sl[1] = ref1;

            if (ref0 > ref1)
            {
                ref_sl[2] = (6 * ref0 + 1 * ref1) / 7;
                ref_sl[3] = (5 * ref0 + 2 * ref1) / 7;
                ref_sl[4] = (4 * ref0 + 3 * ref1) / 7;
                ref_sl[5] = (3 * ref0 + 4 * ref1) / 7;
                ref_sl[6] = (2 * ref0 + 5 * ref1) / 7;
                ref_sl[7] = (1 * ref0 + 6 * ref1) / 7;
            }
            else
            {
                ref_sl[2] = (4 * ref0 + 1 * ref1) / 5;
                ref_sl[3] = (3 * ref0 + 2 * ref1) / 5;
                ref_sl[4] = (2 * ref0 + 3 * ref1) / 5;
                ref_sl[5] = (1 * ref0 + 4 * ref1) / 5;
                ref_sl[6] = 0;
                ref_sl[7] = 255;
            }

            byte[] index_block1 = GetBC3Indices(reader.ReadBytes(3));

            byte[] index_block2 = GetBC3Indices(reader.ReadBytes(3));

            byte[] bytes = new byte[16];
            for (int i = 0; i < 8; i++)
            {
                bytes[7 - i] = (byte)ref_sl[index_block1[i]];
            }
            for (int i = 0; i < 8; i++)
            {
                bytes[15 - i] = (byte)ref_sl[index_block2[i]];
            }

            return bytes;
        }

        static byte[] GetBC3Indices(byte[] buf_block) =>
            new byte[] {
                (byte)((buf_block[2] & 0b1110_0000) >> 5),
                (byte)((buf_block[2] & 0b0001_1100) >> 2),
                (byte)(((buf_block[2] & 0b0000_0011) << 1) | ((buf_block[1] & 0b1 << 7) >> 7)),
                (byte)((buf_block[1] & 0b0111_0000) >> 4),
                (byte)((buf_block[1] & 0b0000_1110) >> 1),
                (byte)(((buf_block[1] & 0b0000_0001) << 2) | ((buf_block[0] & 0b11 << 6) >> 6)),
                (byte)((buf_block[0] & 0b0011_1000) >> 3),
                (byte)(buf_block[0] & 0b0000_0111)
            };
    }
}
