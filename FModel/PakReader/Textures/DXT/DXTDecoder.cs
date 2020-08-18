namespace PakReader.Textures.DXT
{
    public static class DXTDecoder
    {
        struct Colour8888
        {
            public byte red;
            public byte green;
            public byte blue;
            public byte alpha;
        }

        public static byte[] DecodeDXT1(byte[] inp, int width, int height, int depth)
        {
            var bpp = 4;
            var bps = width * bpp * 1;
            var sizeofplane = bps * height;

            byte[] rawData = new byte[depth * sizeofplane + height * bps + width * bpp];
            var colours = new Colour8888[4];
            colours[0].alpha = 0xFF;
            colours[1].alpha = 0xFF;
            colours[2].alpha = 0xFF;

            unsafe
            {
                fixed (byte* bytePtr = inp)
                {
                    byte* temp = bytePtr;
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < height; y += 4)
                        {
                            for (int x = 0; x < width; x += 4)
                            {
                                ushort colour0 = *((ushort*)temp);
                                ushort colour1 = *((ushort*)(temp + 2));
                                DxtcReadColor(colour0, ref colours[0]);
                                DxtcReadColor(colour1, ref colours[1]);

                                uint bitmask = ((uint*)temp)[1];
                                temp += 8;

                                if (colour0 > colour1)
                                {
                                    // Four-color block: derive the other two colors.
                                    // 00 = color_0, 01 = color_1, 10 = color_2, 11 = color_3
                                    // These 2-bit codes correspond to the 2-bit fields
                                    // stored in the 64-bit block.
                                    colours[2].blue = (byte)((2 * colours[0].blue + colours[1].blue + 1) / 3);
                                    colours[2].green = (byte)((2 * colours[0].green + colours[1].green + 1) / 3);
                                    colours[2].red = (byte)((2 * colours[0].red + colours[1].red + 1) / 3);
                                    //colours[2].alpha = 0xFF;

                                    colours[3].blue = (byte)((colours[0].blue + 2 * colours[1].blue + 1) / 3);
                                    colours[3].green = (byte)((colours[0].green + 2 * colours[1].green + 1) / 3);
                                    colours[3].red = (byte)((colours[0].red + 2 * colours[1].red + 1) / 3);
                                    colours[3].alpha = 0xFF;
                                }
                                else
                                {
                                    // Three-color block: derive the other color.
                                    // 00 = color_0,  01 = color_1,  10 = color_2,
                                    // 11 = transparent.
                                    // These 2-bit codes correspond to the 2-bit fields 
                                    // stored in the 64-bit block. 
                                    colours[2].blue = (byte)((colours[0].blue + colours[1].blue) / 2);
                                    colours[2].green = (byte)((colours[0].green + colours[1].green) / 2);
                                    colours[2].red = (byte)((colours[0].red + colours[1].red) / 2);
                                    //colours[2].alpha = 0xFF;

                                    colours[3].blue = (byte)((colours[0].blue + 2 * colours[1].blue + 1) / 3);
                                    colours[3].green = (byte)((colours[0].green + 2 * colours[1].green + 1) / 3);
                                    colours[3].red = (byte)((colours[0].red + 2 * colours[1].red + 1) / 3);
                                    colours[3].alpha = 0x00;
                                }

                                for (int j = 0, k = 0; j < 4; j++)
                                {
                                    for (int i = 0; i < 4; i++, k++)
                                    {
                                        int select = (int)((bitmask & (0x03 << k * 2)) >> k * 2);
                                        Colour8888 col = colours[select];
                                        if (((x + i) < width) && ((y + j) < height))
                                        {
                                            uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp);
                                            rawData[offset + 0] = col.red;
                                            rawData[offset + 1] = col.green;
                                            rawData[offset + 2] = col.blue;
                                            rawData[offset + 3] = col.alpha;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return rawData;
        }

        public static byte[] DecodeDXT5(byte[] inp, int width, int height, int depth)
        {
            var bpp = 4;
            var bps = width * bpp * 1;
            var sizeofplane = bps * height;

            byte[] rawData = new byte[depth * sizeofplane + height * bps + width * bpp];
            var colours = new Colour8888[4];
            ushort[] alphas = new ushort[8];

            unsafe
            {
                fixed (byte* bytePtr = inp)
                {
                    byte* temp = bytePtr;
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < height; y += 4)
                        {
                            for (int x = 0; x < width; x += 4)
                            {
                                if (y >= height || x >= width)
                                    break;

                                alphas[0] = temp[0];
                                alphas[1] = temp[1];
                                byte* alphamask = (temp + 2);
                                temp += 8;

                                DxtcReadColors(temp, colours);
                                uint bitmask = ((uint*)temp)[1];
                                temp += 8;

                                // Four-color block: derive the other two colors.
                                // 00 = color_0, 01 = color_1, 10 = color_2, 11	= color_3
                                // These 2-bit codes correspond to the 2-bit fields
                                // stored in the 64-bit block.
                                colours[2].blue = (byte)((2 * colours[0].blue + colours[1].blue + 1) / 3);
                                colours[2].green = (byte)((2 * colours[0].green + colours[1].green + 1) / 3);
                                colours[2].red = (byte)((2 * colours[0].red + colours[1].red + 1) / 3);
                                //colours[2].alpha = 0xFF;

                                colours[3].blue = (byte)((colours[0].blue + 2 * colours[1].blue + 1) / 3);
                                colours[3].green = (byte)((colours[0].green + 2 * colours[1].green + 1) / 3);
                                colours[3].red = (byte)((colours[0].red + 2 * colours[1].red + 1) / 3);
                                //colours[3].alpha = 0xFF;

                                int k = 0;
                                for (int j = 0; j < 4; j++)
                                {
                                    for (int i = 0; i < 4; k++, i++)
                                    {
                                        int select = (int)((bitmask & (0x03 << k * 2)) >> k * 2);
                                        Colour8888 col = colours[select];
                                        // only put pixels out < width or height
                                        if (((x + i) < width) && ((y + j) < height))
                                        {
                                            uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp);
                                            rawData[offset] = col.red;
                                            rawData[offset + 1] = col.green;
                                            rawData[offset + 2] = col.blue;
                                        }
                                    }
                                }

                                // 8-alpha or 6-alpha block?
                                if (alphas[0] > alphas[1])
                                {
                                    // 8-alpha block:  derive the other six alphas.
                                    // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                                    alphas[2] = (ushort)((6 * alphas[0] + 1 * alphas[1] + 3) / 7); // bit code 010
                                    alphas[3] = (ushort)((5 * alphas[0] + 2 * alphas[1] + 3) / 7); // bit code 011
                                    alphas[4] = (ushort)((4 * alphas[0] + 3 * alphas[1] + 3) / 7); // bit code 100
                                    alphas[5] = (ushort)((3 * alphas[0] + 4 * alphas[1] + 3) / 7); // bit code 101
                                    alphas[6] = (ushort)((2 * alphas[0] + 5 * alphas[1] + 3) / 7); // bit code 110
                                    alphas[7] = (ushort)((1 * alphas[0] + 6 * alphas[1] + 3) / 7); // bit code 111
                                }
                                else
                                {
                                    // 6-alpha block.
                                    // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                                    alphas[2] = (ushort)((4 * alphas[0] + 1 * alphas[1] + 2) / 5); // Bit code 010
                                    alphas[3] = (ushort)((3 * alphas[0] + 2 * alphas[1] + 2) / 5); // Bit code 011
                                    alphas[4] = (ushort)((2 * alphas[0] + 3 * alphas[1] + 2) / 5); // Bit code 100
                                    alphas[5] = (ushort)((1 * alphas[0] + 4 * alphas[1] + 2) / 5); // Bit code 101
                                    alphas[6] = 0x00; // Bit code 110
                                    alphas[7] = 0xFF; // Bit code 111
                                }

                                // Note: Have to separate the next two loops,
                                // it operates on a 6-byte system.

                                // First three bytes
                                //uint bits = (uint)(alphamask[0]);
                                uint bits = (uint)((alphamask[0]) | (alphamask[1] << 8) | (alphamask[2] << 16));
                                for (int j = 0; j < 2; j++)
                                {
                                    for (int i = 0; i < 4; i++)
                                    {
                                        // only put pixels out < width or height
                                        if (((x + i) < width) && ((y + j) < height))
                                        {
                                            uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp + 3);
                                            rawData[offset] = (byte)alphas[bits & 0x07];
                                        }
                                        bits >>= 3;
                                    }
                                }

                                // Last three bytes
                                //bits = (uint)(alphamask[3]);
                                bits = (uint)((alphamask[3]) | (alphamask[4] << 8) | (alphamask[5] << 16));
                                for (int j = 2; j < 4; j++)
                                {
                                    for (int i = 0; i < 4; i++)
                                    {
                                        // only put pixels out < width or height
                                        if (((x + i) < width) && ((y + j) < height))
                                        {
                                            uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp + 3);
                                            rawData[offset] = (byte)alphas[bits & 0x07];
                                        }
                                        bits >>= 3;
                                    }
                                }
                            }
                        }
                    }
                }
                return rawData;
            }
        }

        static unsafe void DxtcReadColors(byte* data, Colour8888[] op)
        {
            byte buf = (byte)((data[1] & 0xF8) >> 3);
            op[0].red = (byte)(buf << 3 | buf >> 2);
            buf = (byte)(((data[0] & 0xE0) >> 5) | ((data[1] & 0x7) << 3));
            op[0].green = (byte)(buf << 2 | buf >> 3);
            buf = (byte)(data[0] & 0x1F);
            op[0].blue = (byte)(buf << 3 | buf >> 2);

            buf = (byte)((data[3] & 0xF8) >> 3);
            op[1].red = (byte)(buf << 3 | buf >> 2);
            buf = (byte)(((data[2] & 0xE0) >> 5) | ((data[3] & 0x7) << 3));
            op[1].green = (byte)(buf << 2 | buf >> 3);
            buf = (byte)(data[2] & 0x1F);
            op[1].blue = (byte)(buf << 3 | buf >> 2);
        }

        static void DxtcReadColor(ushort data, ref Colour8888 op)
        {
            byte buf = (byte)((data & 0xF800) >> 11);
            op.red = (byte)(buf << 3 | buf >> 2);
            buf = (byte)((data & 0x7E0) >> 5);
            op.green = (byte)(buf << 2 | buf >> 3);
            buf = (byte)(data & 0x1f);
            op.blue = (byte)(buf << 3 | buf >> 2);
        }
    }
}
