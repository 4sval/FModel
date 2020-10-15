using System;
using PakReader.Parsers.Objects;
using PakReader.Textures.ASTC;
using PakReader.Textures.BC;
using PakReader.Textures.DXT;
using SkiaSharp;

namespace PakReader.Textures
{
    static class TextureDecoder
    {
        public static SKImage DecodeImage(byte[] sequence, int width, int height, int depth, EPixelFormat format)
        {
            byte[] data;
            SKColorType colorType;
            switch (format)
            {
                case EPixelFormat.PF_DXT5:
                    data = DXTDecoder.DecodeDXT5(sequence, width, height, depth);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_DXT1:
                    data = DXTDecoder.DecodeDXT1(sequence, width, height, depth);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_ASTC_8x8:
                    var x = (int)TextureFormatHelper.GetBlockWidth(format);
                    var y = (int)TextureFormatHelper.GetBlockHeight(format);
                    var z = (int)TextureFormatHelper.GetBlockDepth(format);
                    data = ASTCDecoder.DecodeToRGBA8888(sequence, x, y, z, width, height, 1);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_B8G8R8A8:
                    data = sequence;
                    colorType = SKColorType.Bgra8888;
                    break;
                case EPixelFormat.PF_BC5:
                    data = BCDecoder.DecodeBC5(sequence, width, height);
                    colorType = SKColorType.Rgb888x;
                    break;
                case EPixelFormat.PF_BC4:
                    data = BCDecoder.DecodeBC4(sequence, width, height);
                    colorType = SKColorType.Rgb888x;
                    break;
                case EPixelFormat.PF_G8:
                    data = sequence;
                    colorType = SKColorType.Gray8;
                    break;
                case EPixelFormat.PF_FloatRGBA:
                    data = sequence;
                    colorType = SKColorType.RgbaF16;
                    break;
                case EPixelFormat.PF_BC7:
                    data = Detex.DecodeDetexLinear(sequence, width, height, isFloat: false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_BC6H:
                    data = Detex.DecodeDetexLinear(sequence, width, height, isFloat: true,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC_FLOAT,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBX8); // Not sure whether that works, would actually be DETEX_PIXEL_FORMAT_FLOAT_RGBX32
                    data = Detex.DecodeBC6H(sequence, width, height);
                    colorType = SKColorType.Rgb888x;
                    break;
                case EPixelFormat.PF_ETC1:
                    data = Detex.DecodeDetexLinear(sequence, width, height, isFloat: false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC1,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_ETC2_RGB:
                    data = Detex.DecodeDetexLinear(sequence, width, height, isFloat: false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgba8888;
                    break;
                case EPixelFormat.PF_ETC2_RGBA:
                    data = Detex.DecodeDetexLinear(sequence, width, height, isFloat: false,
                        inputFormat: DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2_EAC,
                        outputPixelFormat: DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgba8888;
                    break;
                default:
                    throw new NotImplementedException($"Cannot decode {format} format");
            }

            using var bitmap = new SKBitmap(new SKImageInfo(width, height, colorType, SKAlphaType.Unpremul));
            unsafe
            {
                fixed (byte* p = data)
                {
                    bitmap.SetPixels(new IntPtr(p));
                }
            }
            
            return SKImage.FromBitmap(bitmap);
        }
    }
}
