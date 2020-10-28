using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace FModel.PakReader.Textures.BC
{
    public static class Detex
    {
        private const string DETEX_DLL_NAME = "Detex.dll";

        static Detex()
        {
            PrepareDllFile();
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct detexTexture
        {
            public uint format;
            public byte* data;
            public int width;
            public int height;
            public int width_in_blocks;
            public int height_in_blocks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] DecodeDetexLinear(byte[] inp, int width, int height, bool isFloat, DetexTextureFormat inputFormat,
            DetexPixelFormat outputPixelFormat, int blockSizeX = 4, int blockSizeY = 4)
        {
            var dst = new byte[width * height * (isFloat ? 16 : 4)];
            DecodeDetexLinear(inp, dst, width, height, inputFormat, outputPixelFormat, blockSizeX, blockSizeY);
            return dst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DecodeDetexLinear(byte[] inp, byte[] dst, int width, int height, DetexTextureFormat inputFormat, DetexPixelFormat outputPixelFormat, int blockSizeX = 4, int blockSizeY = 4)
        {
            unsafe
            {
                detexTexture tex;
                tex.format = (uint) inputFormat;
                tex.data = (byte*) Unsafe.AsPointer(ref inp[0]);
                tex.width = width;
                tex.height = height;
                tex.width_in_blocks = width / 4;
                tex.height_in_blocks = height / 4;
                return detexDecompressTextureLinear(&tex, (byte*) Unsafe.AsPointer(ref dst[0]),
                    (uint) outputPixelFormat);
            }
        }
        
        public static byte[] DecodeBC6H(byte[] inp, int width, int height)
        {
            const int PIXEL_SIZE = 16;
            var dst = new byte[width * height * PIXEL_SIZE];
            
            DecodeDetexLinear(inp, dst, width, height, DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC_FLOAT,
                DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBX8);
            
            return dst;
        }

        [DllImport(DETEX_DLL_NAME)]
        private static extern unsafe bool detexDecompressTextureLinear(detexTexture* texture, byte* pixelBuffer,
            uint pixelFormat);

        private static void PrepareDllFile()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FModel.Resources.Detex.dll");
            if (stream == null)
                throw new MissingManifestResourceException("Couldn't find Detex.dll in Embedded Resources");
            var ba = new byte[(int) stream.Length];
            stream.Read(ba, 0, (int) stream.Length);

            bool fileOk;
            var dllFile = DETEX_DLL_NAME;

            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var fileHash = BitConverter.ToString(sha1.ComputeHash(ba)).Replace("-", string.Empty);

                if (File.Exists(dllFile))
                {
                    var bb = File.ReadAllBytes(dllFile);
                    var fileHash2 = BitConverter.ToString(sha1.ComputeHash(bb)).Replace("-", string.Empty);

                    fileOk = fileHash == fileHash2;
                }
                else
                {
                    fileOk = false;
                }
            }

            if (!fileOk)
            {
                File.WriteAllBytes(dllFile, ba);
            }
        }
    }
}