using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PakReader.Textures.ASTC
{
    public class ASTCDecoderException : Exception
    {
        public ASTCDecoderException(string ExMsg) : base(ExMsg) {/* Toolbox.Library.Forms.STErrorDialog.Show(ExMsg, "", ExMsg);*/ }
    }

    //https://github.com/GammaUNC/FasTC/blob/master/ASTCEncoder/src/Decompressor.cpp
    public static class ASTCDecoder
    {
        struct TexelWeightParams
        {
            public int Width;
            public int Height;
            public bool DualPlane;
            public int MaxWeight;
            public bool Error;
            public bool VoidExtentLDR;
            public bool VoidExtentHDR;

            public int GetPackedBitSize()
            {
                // How many indices do we have?
                int Indices = Height * Width;

                if (DualPlane)
                {
                    Indices *= 2;
                }

                IntegerEncoded IntEncoded = IntegerEncoded.CreateEncoding(MaxWeight);

                return IntEncoded.GetBitLength(Indices);
            }

            public int GetNumWeightValues()
            {
                int Ret = Width * Height;

                if (DualPlane)
                {
                    Ret *= 2;
                }

                return Ret;
            }
        }

        public static byte[] DecodeToRGBA8888(byte[] InputBuffer, int BlockX, int BlockY, int BlockZ, int X, int Y, int Z)
        {
            using MemoryStream InputStream = new MemoryStream(InputBuffer);
            BinaryReader BinReader = new BinaryReader(InputStream);

            if (BlockX > 12 || BlockY > 12 || BlockZ > 12)
            {
                throw new Exception("Block size unsupported!");
            }

            using MemoryStream OutputStream = new MemoryStream();
            int BlockIndex = 0;
            for (int k = 0; k < Z; k += BlockZ)
            {
                for (int j = 0; j < Y; j += BlockY)
                {
                    for (int i = 0; i < X; i += BlockX)
                    {
                        int[] DecompressedData = new int[144];

                        DecompressBlock(BinReader.ReadBytes(0x10), DecompressedData, BlockX, BlockY);

                        int DecompressedWidth = Math.Min(BlockX, X - i);
                        int DecompressedHeight = Math.Min(BlockY, Y - j);
                        int BaseOffsets = (j * X + i) * 4;

                        for (int jj = 0; jj < DecompressedHeight; jj++)
                        {
                            OutputStream.Seek(BaseOffsets + jj * X * 4, SeekOrigin.Begin);

                            byte[] OutputBuffer = new byte[DecompressedData.Length * sizeof(int)];
                            Buffer.BlockCopy(DecompressedData, 0, OutputBuffer, 0, OutputBuffer.Length);

                            OutputStream.Write(OutputBuffer, jj * BlockX * 4, DecompressedWidth * 4);
                        }

                        BlockIndex++;
                    }
                }
            }

            return OutputStream.ToArray();
        }

        public static bool DecompressBlock(
            byte[] InputBuffer,
            int[] OutputBuffer,
            int BlockWidth,
            int BlockHeight)
        {
            BitArrayStream BitStream = new BitArrayStream(new BitArray(InputBuffer));
            TexelWeightParams TexelParams = DecodeBlockInfo(BitStream);

            if (TexelParams.Error)
            {
                throw new Exception("Invalid block mode");
            }

            //  Console.WriteLine($"BlockWidth {BlockWidth} {BlockHeight} BlockHeight");
            //  Console.WriteLine($"TexelParams {TexelParams.Width} X {TexelParams.Height}");

            if (TexelParams.VoidExtentLDR)
            {
                FillVoidExtentLDR(BitStream, OutputBuffer, BlockWidth, BlockHeight);

                return true;
            }

            if (TexelParams.VoidExtentHDR)
            {
                throw new Exception("HDR void extent blocks are unsupported!");
            }

            if (TexelParams.Width > BlockWidth)
            {
                throw new Exception("Texel weight grid width should be smaller than block width");
            }

            if (TexelParams.Height > BlockHeight)
            {
                throw new Exception("Texel weight grid height should be smaller than block height");
            }

            // Read num partitions
            int NumberPartitions = BitStream.ReadBits(2) + 1;

            if (NumberPartitions == 4 && TexelParams.DualPlane)
            {
                throw new Exception("Dual plane mode is incompatible with four partition blocks");
            }

            // Based on the number of partitions, read the color endpoint mode for
            // each partition.

            // Determine partitions, partition index, and color endpoint modes
            int PartitionIndex;
            uint[] ColorEndpointMode = { 0, 0, 0, 0 };

            BitArrayStream ColorEndpointStream = new BitArrayStream(new BitArray(16 * 8));

            // Read extra config data...
            uint BaseColorEndpointMode = 0;

            if (NumberPartitions == 1)
            {
                ColorEndpointMode[0] = (uint)BitStream.ReadBits(4);
                PartitionIndex = 0;
            }
            else
            {
                PartitionIndex = BitStream.ReadBits(10);
                BaseColorEndpointMode = (uint)BitStream.ReadBits(6);
            }

            uint BaseMode = (BaseColorEndpointMode & 3);

            // Remaining bits are color endpoint data...
            int NumberWeightBits = TexelParams.GetPackedBitSize();
            int RemainingBits = 128 - NumberWeightBits - BitStream.Position;

            // Consider extra bits prior to texel data...
            uint ExtraColorEndpointModeBits = 0;

            if (BaseMode != 0)
            {
                switch (NumberPartitions)
                {
                    case 2: ExtraColorEndpointModeBits += 2; break;
                    case 3: ExtraColorEndpointModeBits += 5; break;
                    case 4: ExtraColorEndpointModeBits += 8; break;
                    default: break;
                }
            }

            RemainingBits -= (int)ExtraColorEndpointModeBits;

            // Do we have a dual plane situation?
            int PlaneSelectorBits = 0;

            if (TexelParams.DualPlane)
            {
                PlaneSelectorBits = 2;
            }

            RemainingBits -= PlaneSelectorBits;

            // Read color data...
            int ColorDataBits = RemainingBits;

            while (RemainingBits > 0)
            {
                int NumberBits = Math.Min(RemainingBits, 8);
                int Bits = BitStream.ReadBits(NumberBits);
                ColorEndpointStream.WriteBits(Bits, NumberBits);
                RemainingBits -= 8;
            }

            // Read the plane selection bits
            int PlaneIndices = BitStream.ReadBits(PlaneSelectorBits);

            // Read the rest of the CEM
            if (BaseMode != 0)
            {
                uint ExtraColorEndpointMode = (uint)BitStream.ReadBits((int)ExtraColorEndpointModeBits);
                uint TempColorEndpointMode = (ExtraColorEndpointMode << 6) | BaseColorEndpointMode;
                TempColorEndpointMode >>= 2;

                bool[] C = new bool[4];

                for (int i = 0; i < NumberPartitions; i++)
                {
                    C[i] = (TempColorEndpointMode & 1) != 0;
                    TempColorEndpointMode >>= 1;
                }

                byte[] M = new byte[4];

                for (int i = 0; i < NumberPartitions; i++)
                {
                    M[i] = (byte)(TempColorEndpointMode & 3);
                    TempColorEndpointMode >>= 2;
                }

                for (int i = 0; i < NumberPartitions; i++)
                {
                    ColorEndpointMode[i] = BaseMode;
                    if (!(C[i])) ColorEndpointMode[i] -= 1;
                    ColorEndpointMode[i] <<= 2;
                    ColorEndpointMode[i] |= M[i];
                }
            }
            else if (NumberPartitions > 1)
            {
                uint TempColorEndpointMode = BaseColorEndpointMode >> 2;

                for (uint i = 0; i < NumberPartitions; i++)
                {
                    ColorEndpointMode[i] = TempColorEndpointMode;
                }
            }

            // Decode both color data and texel weight data
            int[] ColorValues = new int[32]; // Four values * two endpoints * four maximum partitions
            DecodeColorValues(ColorValues, ColorEndpointStream.ToByteArray(), ColorEndpointMode, NumberPartitions, ColorDataBits);

            ASTCPixel[][] EndPoints = new ASTCPixel[4][];
            EndPoints[0] = new ASTCPixel[2];
            EndPoints[1] = new ASTCPixel[2];
            EndPoints[2] = new ASTCPixel[2];
            EndPoints[3] = new ASTCPixel[2];

            int ColorValuesPosition = 0;

            for (int i = 0; i < NumberPartitions; i++)
            {
                ComputeEndpoints(EndPoints[i], ColorValues, ColorEndpointMode[i], ref ColorValuesPosition);
            }

            // Read the texel weight data.
            byte[] TexelWeightData = (byte[])InputBuffer.Clone();

            // Reverse everything
            for (int i = 0; i < 8; i++)
            {
                byte a = ReverseByte(TexelWeightData[i]);
                byte b = ReverseByte(TexelWeightData[15 - i]);

                TexelWeightData[i] = b;
                TexelWeightData[15 - i] = a;
            }

            // Make sure that higher non-texel bits are set to zero
            int ClearByteStart = (TexelParams.GetPackedBitSize() >> 3) + 1;
            TexelWeightData[ClearByteStart - 1] &= (byte)((1 << (TexelParams.GetPackedBitSize() % 8)) - 1);

            int cLen = 16 - ClearByteStart;
            for (int i = ClearByteStart; i < ClearByteStart + cLen; i++) TexelWeightData[i] = 0;

            List<IntegerEncoded> TexelWeightValues = new List<IntegerEncoded>();
            BitArrayStream WeightBitStream = new BitArrayStream(new BitArray(TexelWeightData));

            IntegerEncoded.DecodeIntegerSequence(TexelWeightValues, WeightBitStream, TexelParams.MaxWeight, TexelParams.GetNumWeightValues());

            // Blocks can be at most 12x12, so we can have as many as 144 weights
            int[][] Weights = new int[2][];
            Weights[0] = new int[144];
            Weights[1] = new int[144];

            UnquantizeTexelWeights(Weights, TexelWeightValues, TexelParams, BlockWidth, BlockHeight);

            // Now that we have endpoints and weights, we can interpolate and generate
            // the proper decoding...
            for (int j = 0; j < BlockHeight; j++)
            {
                for (int i = 0; i < BlockWidth; i++)
                {
                    int Partition = Select2DPartition(PartitionIndex, i, j, NumberPartitions, ((BlockHeight * BlockWidth) < 32));

                    ASTCPixel Pixel = new ASTCPixel(0, 0, 0, 0);
                    for (int Component = 0; Component < 4; Component++)
                    {
                        int Component0 = EndPoints[Partition][0].GetComponent(Component);
                        Component0 = BitArrayStream.Replicate(Component0, 8, 16);
                        int Component1 = EndPoints[Partition][1].GetComponent(Component);
                        Component1 = BitArrayStream.Replicate(Component1, 8, 16);

                        int Plane = 0;

                        if (TexelParams.DualPlane && (((PlaneIndices + 1) & 3) == Component))
                        {
                            Plane = 1;
                        }

                        int Weight = Weights[Plane][j * BlockWidth + i];
                        int FinalComponent = (Component0 * (64 - Weight) + Component1 * Weight + 32) / 64;

                        if (FinalComponent == 65535)
                        {
                            Pixel.SetComponent(Component, 255);
                        }
                        else
                        {
                            double FinalComponentFloat = FinalComponent;
                            Pixel.SetComponent(Component, (int)(255.0 * (FinalComponentFloat / 65536.0) + 0.5));
                        }
                    }

                    OutputBuffer[j * BlockWidth + i] = Pixel.Pack();
                }
            }

            return true;
        }

        private static int Select2DPartition(int Seed, int X, int Y, int PartitionCount, bool IsSmallBlock)
        {
            return SelectPartition(Seed, X, Y, 0, PartitionCount, IsSmallBlock);
        }

        private static int SelectPartition(int Seed, int X, int Y, int Z, int PartitionCount, bool IsSmallBlock)
        {
            if (PartitionCount == 1)
            {
                return 0;
            }

            if (IsSmallBlock)
            {
                X <<= 1;
                Y <<= 1;
                Z <<= 1;
            }

            Seed += (PartitionCount - 1) * 1024;

            int RightNum = Hash52((uint)Seed);
            byte Seed01 = (byte)(RightNum & 0xF);
            byte Seed02 = (byte)((RightNum >> 4) & 0xF);
            byte Seed03 = (byte)((RightNum >> 8) & 0xF);
            byte Seed04 = (byte)((RightNum >> 12) & 0xF);
            byte Seed05 = (byte)((RightNum >> 16) & 0xF);
            byte Seed06 = (byte)((RightNum >> 20) & 0xF);
            byte Seed07 = (byte)((RightNum >> 24) & 0xF);
            byte Seed08 = (byte)((RightNum >> 28) & 0xF);
            byte Seed09 = (byte)((RightNum >> 18) & 0xF);
            byte Seed10 = (byte)((RightNum >> 22) & 0xF);
            byte Seed11 = (byte)((RightNum >> 26) & 0xF);
            byte Seed12 = (byte)(((RightNum >> 30) | (RightNum << 2)) & 0xF);

            Seed01 *= Seed01; Seed02 *= Seed02;
            Seed03 *= Seed03; Seed04 *= Seed04;
            Seed05 *= Seed05; Seed06 *= Seed06;
            Seed07 *= Seed07; Seed08 *= Seed08;
            Seed09 *= Seed09; Seed10 *= Seed10;
            Seed11 *= Seed11; Seed12 *= Seed12;

            int SeedHash1, SeedHash2, SeedHash3;

            if ((Seed & 1) != 0)
            {
                SeedHash1 = (Seed & 2) != 0 ? 4 : 5;
                SeedHash2 = (PartitionCount == 3) ? 6 : 5;
            }
            else
            {
                SeedHash1 = (PartitionCount == 3) ? 6 : 5;
                SeedHash2 = (Seed & 2) != 0 ? 4 : 5;
            }

            SeedHash3 = (Seed & 0x10) != 0 ? SeedHash1 : SeedHash2;

            Seed01 >>= SeedHash1; Seed02 >>= SeedHash2; Seed03 >>= SeedHash1; Seed04 >>= SeedHash2;
            Seed05 >>= SeedHash1; Seed06 >>= SeedHash2; Seed07 >>= SeedHash1; Seed08 >>= SeedHash2;
            Seed09 >>= SeedHash3; Seed10 >>= SeedHash3; Seed11 >>= SeedHash3; Seed12 >>= SeedHash3;

            int a = Seed01 * X + Seed02 * Y + Seed11 * Z + (RightNum >> 14);
            int b = Seed03 * X + Seed04 * Y + Seed12 * Z + (RightNum >> 10);
            int c = Seed05 * X + Seed06 * Y + Seed09 * Z + (RightNum >> 6);
            int d = Seed07 * X + Seed08 * Y + Seed10 * Z + (RightNum >> 2);

            a &= 0x3F; b &= 0x3F; c &= 0x3F; d &= 0x3F;

            if (PartitionCount < 4) d = 0;
            if (PartitionCount < 3) c = 0;

            if (a >= b && a >= c && a >= d) return 0;
            else if (b >= c && b >= d) return 1;
            else if (c >= d) return 2;
            return 3;
        }

        static int Hash52(uint Val)
        {
            Val ^= Val >> 15; Val -= Val << 17; Val += Val << 7; Val += Val << 4;
            Val ^= Val >> 5; Val += Val << 16; Val ^= Val >> 7; Val ^= Val >> 3;
            Val ^= Val << 6; Val ^= Val >> 17;

            return (int)Val;
        }

        static void UnquantizeTexelWeights(
            int[][] OutputBuffer,
            List<IntegerEncoded> Weights,
            TexelWeightParams TexelParams,
            int BlockWidth,
            int BlockHeight)
        {
            int WeightIndices = 0;
            int[][] Unquantized = new int[2][];
            Unquantized[0] = new int[144];
            Unquantized[1] = new int[144];

            for (int i = 0; i < Weights.Count; i++)
            {
                Unquantized[0][WeightIndices] = UnquantizeTexelWeight(Weights[i]);

                if (TexelParams.DualPlane)
                {
                    i++;
                    Unquantized[1][WeightIndices] = UnquantizeTexelWeight(Weights[i]);

                    if (i == Weights.Count)
                    {
                        break;
                    }
                }

                if (++WeightIndices >= (TexelParams.Width * TexelParams.Height)) break;
            }

            // Do infill if necessary (Section C.2.18) ...
            int Ds = (1024 + (BlockWidth / 2)) / (BlockWidth - 1);
            int Dt = (1024 + (BlockHeight / 2)) / (BlockHeight - 1);

            int PlaneScale = TexelParams.DualPlane ? 2 : 1;

            for (int Plane = 0; Plane < PlaneScale; Plane++)
            {
                for (int t = 0; t < BlockHeight; t++)
                {
                    for (int s = 0; s < BlockWidth; s++)
                    {
                        int cs = Ds * s;
                        int ct = Dt * t;

                        int gs = (cs * (TexelParams.Width - 1) + 32) >> 6;
                        int gt = (ct * (TexelParams.Height - 1) + 32) >> 6;

                        int js = gs >> 4;
                        int fs = gs & 0xF;

                        int jt = gt >> 4;
                        int ft = gt & 0x0F;

                        int w11 = (fs * ft + 8) >> 4;
                        int w10 = ft - w11;
                        int w01 = fs - w11;
                        int w00 = 16 - fs - ft + w11;

                        int v0 = js + jt * TexelParams.Width;

                        int p00 = 0;
                        int p01 = 0;
                        int p10 = 0;
                        int p11 = 0;

                        if (v0 < (TexelParams.Width * TexelParams.Height))
                        {
                            p00 = Unquantized[Plane][v0];
                        }

                        if (v0 + 1 < (TexelParams.Width * TexelParams.Height))
                        {
                            p01 = Unquantized[Plane][v0 + 1];
                        }

                        if (v0 + TexelParams.Width < (TexelParams.Width * TexelParams.Height))
                        {
                            p10 = Unquantized[Plane][v0 + TexelParams.Width];
                        }

                        if (v0 + TexelParams.Width + 1 < (TexelParams.Width * TexelParams.Height))
                        {
                            p11 = Unquantized[Plane][v0 + TexelParams.Width + 1];
                        }

                        OutputBuffer[Plane][t * BlockWidth + s] = (p00 * w00 + p01 * w01 + p10 * w10 + p11 * w11 + 8) >> 4;
                    }
                }
            }
        }

        static int UnquantizeTexelWeight(IntegerEncoded IntEncoded)
        {
            int BitValue = IntEncoded.BitValue;
            int BitLength = IntEncoded.NumberBits;

            int A = BitArrayStream.Replicate(BitValue & 1, 1, 7);
            int B = 0, C = 0, D = 0;

            int Result = 0;

            switch (IntEncoded.GetEncoding())
            {
                case IntegerEncoded.EIntegerEncoding.JustBits:
                    Result = BitArrayStream.Replicate(BitValue, BitLength, 6);
                    break;

                case IntegerEncoded.EIntegerEncoding.Trit:
                    {
                        D = IntEncoded.TritValue;
                        switch (BitLength)
                        {
                            case 0:
                                {
                                    int[] Results = { 0, 32, 63 };
                                    Result = Results[D];

                                    break;
                                }

                            case 1:
                                {
                                    C = 50;
                                    break;
                                }

                            case 2:
                                {
                                    C = 23;
                                    int b = (BitValue >> 1) & 1;
                                    B = (b << 6) | (b << 2) | b;

                                    break;
                                }

                            case 3:
                                {
                                    C = 11;
                                    int cb = (BitValue >> 1) & 3;
                                    B = (cb << 5) | cb;

                                    break;
                                }

                            default:
                                throw new ASTCDecoderException("Invalid trit encoding for texel weight");
                        }

                        break;
                    }

                case IntegerEncoded.EIntegerEncoding.Quint:
                    {
                        D = IntEncoded.QuintValue;
                        switch (BitLength)
                        {
                            case 0:
                                {
                                    int[] Results = { 0, 16, 32, 47, 63 };
                                    Result = Results[D];

                                    break;
                                }

                            case 1:
                                {
                                    C = 28;

                                    break;
                                }

                            case 2:
                                {
                                    C = 13;
                                    int b = (BitValue >> 1) & 1;
                                    B = (b << 6) | (b << 1);

                                    break;
                                }

                            default:
                                throw new ASTCDecoderException("Invalid quint encoding for texel weight");
                        }

                        break;
                    }
            }

            if (IntEncoded.GetEncoding() != IntegerEncoded.EIntegerEncoding.JustBits && BitLength > 0)
            {
                // Decode the value...
                Result = D * C + B;
                Result ^= A;
                Result = (A & 0x20) | (Result >> 2);
            }

            // Change from [0,63] to [0,64]
            if (Result > 32)
            {
                Result += 1;
            }

            return Result;
        }

        static byte ReverseByte(byte b)
        {
            // Taken from http://graphics.stanford.edu/~seander/bithacks.html#ReverseByteWith64Bits
            return (byte)((((b) * 0x80200802L) & 0x0884422110L) * 0x0101010101L >> 32);
        }

        static uint[] ReadUintColorValues(int Number, int[] ColorValues, ref int ColorValuesPosition)
        {
            uint[] Ret = new uint[Number];

            for (int i = 0; i < Number; i++)
            {
                Ret[i] = (uint)ColorValues[ColorValuesPosition++];
            }

            return Ret;
        }

        static int[] ReadIntColorValues(int Number, int[] ColorValues, ref int ColorValuesPosition)
        {
            int[] Ret = new int[Number];

            for (int i = 0; i < Number; i++)
            {
                Ret[i] = ColorValues[ColorValuesPosition++];
            }

            return Ret;
        }

        static void ComputeEndpoints(
            ASTCPixel[] EndPoints,
            int[] ColorValues,
            uint ColorEndpointMode,
            ref int ColorValuesPosition)
        {
            switch (ColorEndpointMode)
            {
                case 0:
                    {
                        uint[] Val = ReadUintColorValues(2, ColorValues, ref ColorValuesPosition);

                        EndPoints[0] = new ASTCPixel(0xFF, (short)Val[0], (short)Val[0], (short)Val[0]);
                        EndPoints[1] = new ASTCPixel(0xFF, (short)Val[1], (short)Val[1], (short)Val[1]);

                        break;
                    }


                case 1:
                    {
                        uint[] Val = ReadUintColorValues(2, ColorValues, ref ColorValuesPosition);
                        int L0 = (int)((Val[0] >> 2) | (Val[1] & 0xC0));
                        int L1 = (int)Math.Max(L0 + (Val[1] & 0x3F), 0xFFU);

                        EndPoints[0] = new ASTCPixel(0xFF, (short)L0, (short)L0, (short)L0);
                        EndPoints[1] = new ASTCPixel(0xFF, (short)L1, (short)L1, (short)L1);

                        break;
                    }

                case 4:
                    {
                        uint[] Val = ReadUintColorValues(4, ColorValues, ref ColorValuesPosition);

                        EndPoints[0] = new ASTCPixel((short)Val[2], (short)Val[0], (short)Val[0], (short)Val[0]);
                        EndPoints[1] = new ASTCPixel((short)Val[3], (short)Val[1], (short)Val[1], (short)Val[1]);

                        break;
                    }

                case 5:
                    {
                        int[] Val = ReadIntColorValues(4, ColorValues, ref ColorValuesPosition);

                        BitArrayStream.BitTransferSigned(ref Val[1], ref Val[0]);
                        BitArrayStream.BitTransferSigned(ref Val[3], ref Val[2]);

                        EndPoints[0] = new ASTCPixel((short)Val[2], (short)Val[0], (short)Val[0], (short)Val[0]);
                        EndPoints[1] = new ASTCPixel((short)(Val[2] + Val[3]), (short)(Val[0] + Val[1]), (short)(Val[0] + Val[1]), (short)(Val[0] + Val[1]));

                        EndPoints[0].ClampByte();
                        EndPoints[1].ClampByte();

                        break;
                    }

                case 6:
                    {
                        uint[] Val = ReadUintColorValues(4, ColorValues, ref ColorValuesPosition);

                        EndPoints[0] = new ASTCPixel(0xFF, (short)(Val[0] * Val[3] >> 8), (short)(Val[1] * Val[3] >> 8), (short)(Val[2] * Val[3] >> 8));
                        EndPoints[1] = new ASTCPixel(0xFF, (short)Val[0], (short)Val[1], (short)Val[2]);

                        break;
                    }

                case 8:
                    {
                        uint[] Val = ReadUintColorValues(6, ColorValues, ref ColorValuesPosition);

                        if (Val[1] + Val[3] + Val[5] >= Val[0] + Val[2] + Val[4])
                        {
                            EndPoints[0] = new ASTCPixel(0xFF, (short)Val[0], (short)Val[2], (short)Val[4]);
                            EndPoints[1] = new ASTCPixel(0xFF, (short)Val[1], (short)Val[3], (short)Val[5]);
                        }
                        else
                        {
                            EndPoints[0] = ASTCPixel.BlueContract(0xFF, (short)Val[1], (short)Val[3], (short)Val[5]);
                            EndPoints[1] = ASTCPixel.BlueContract(0xFF, (short)Val[0], (short)Val[2], (short)Val[4]);
                        }

                        break;
                    }

                case 9:
                    {
                        int[] Val = ReadIntColorValues(6, ColorValues, ref ColorValuesPosition);

                        BitArrayStream.BitTransferSigned(ref Val[1], ref Val[0]);
                        BitArrayStream.BitTransferSigned(ref Val[3], ref Val[2]);
                        BitArrayStream.BitTransferSigned(ref Val[5], ref Val[4]);

                        if (Val[1] + Val[3] + Val[5] >= 0)
                        {
                            EndPoints[0] = new ASTCPixel(0xFF, (short)Val[0], (short)Val[2], (short)Val[4]);
                            EndPoints[1] = new ASTCPixel(0xFF, (short)(Val[0] + Val[1]), (short)(Val[2] + Val[3]), (short)(Val[4] + Val[5]));
                        }
                        else
                        {
                            EndPoints[0] = ASTCPixel.BlueContract(0xFF, Val[0] + Val[1], Val[2] + Val[3], Val[4] + Val[5]);
                            EndPoints[1] = ASTCPixel.BlueContract(0xFF, Val[0], Val[2], Val[4]);
                        }

                        EndPoints[0].ClampByte();
                        EndPoints[1].ClampByte();

                        break;
                    }

                case 10:
                    {
                        uint[] Val = ReadUintColorValues(6, ColorValues, ref ColorValuesPosition);

                        EndPoints[0] = new ASTCPixel((short)Val[4], (short)(Val[0] * Val[3] >> 8), (short)(Val[1] * Val[3] >> 8), (short)(Val[2] * Val[3] >> 8));
                        EndPoints[1] = new ASTCPixel((short)Val[5], (short)Val[0], (short)Val[1], (short)Val[2]);

                        break;
                    }

                case 12:
                    {
                        uint[] Val = ReadUintColorValues(8, ColorValues, ref ColorValuesPosition);

                        if (Val[1] + Val[3] + Val[5] >= Val[0] + Val[2] + Val[4])
                        {
                            EndPoints[0] = new ASTCPixel((short)Val[6], (short)Val[0], (short)Val[2], (short)Val[4]);
                            EndPoints[1] = new ASTCPixel((short)Val[7], (short)Val[1], (short)Val[3], (short)Val[5]);
                        }
                        else
                        {
                            EndPoints[0] = ASTCPixel.BlueContract((short)Val[7], (short)Val[1], (short)Val[3], (short)Val[5]);
                            EndPoints[1] = ASTCPixel.BlueContract((short)Val[6], (short)Val[0], (short)Val[2], (short)Val[4]);
                        }

                        break;
                    }

                case 13:
                    {
                        int[] Val = ReadIntColorValues(8, ColorValues, ref ColorValuesPosition);

                        BitArrayStream.BitTransferSigned(ref Val[1], ref Val[0]);
                        BitArrayStream.BitTransferSigned(ref Val[3], ref Val[2]);
                        BitArrayStream.BitTransferSigned(ref Val[5], ref Val[4]);
                        BitArrayStream.BitTransferSigned(ref Val[7], ref Val[6]);

                        if (Val[1] + Val[3] + Val[5] >= 0)
                        {
                            EndPoints[0] = new ASTCPixel((short)Val[6], (short)Val[0], (short)Val[2], (short)Val[4]);
                            EndPoints[1] = new ASTCPixel((short)(Val[7] + Val[6]), (short)(Val[0] + Val[1]), (short)(Val[2] + Val[3]), (short)(Val[4] + Val[5]));
                        }
                        else
                        {
                            EndPoints[0] = ASTCPixel.BlueContract(Val[6] + Val[7], Val[0] + Val[1], Val[2] + Val[3], Val[4] + Val[5]);
                            EndPoints[1] = ASTCPixel.BlueContract(Val[6], Val[0], Val[2], Val[4]);
                        }

                        EndPoints[0].ClampByte();
                        EndPoints[1].ClampByte();

                        break;
                    }

                default:
                    throw new ASTCDecoderException("Unsupported color endpoint mode (is it HDR?)");
            }
        }

        static void DecodeColorValues(
            int[] OutputValues,
            byte[] InputData,
            uint[] Modes,
            int NumberPartitions,
            int NumberBitsForColorData)
        {
            // First figure out how many color values we have
            int NumberValues = 0;

            for (int i = 0; i < NumberPartitions; i++)
            {
                NumberValues += (int)((Modes[i] >> 2) + 1) << 1;
            }

            // Then based on the number of values and the remaining number of bits,
            // figure out the max value for each of them...
            int Range = 256;

            while (--Range > 0)
            {
                IntegerEncoded IntEncoded = IntegerEncoded.CreateEncoding(Range);
                int BitLength = IntEncoded.GetBitLength(NumberValues);

                if (BitLength <= NumberBitsForColorData)
                {
                    // Find the smallest possible range that matches the given encoding
                    while (--Range > 0)
                    {
                        IntegerEncoded NewIntEncoded = IntegerEncoded.CreateEncoding(Range);
                        if (!NewIntEncoded.MatchesEncoding(IntEncoded))
                        {
                            break;
                        }
                    }

                    // Return to last matching range.
                    Range++;
                    break;
                }
            }

            // We now have enough to decode our integer sequence.
            List<IntegerEncoded> IntegerEncodedSequence = new List<IntegerEncoded>();
            BitArrayStream ColorBitStream = new BitArrayStream(new BitArray(InputData));

            IntegerEncoded.DecodeIntegerSequence(IntegerEncodedSequence, ColorBitStream, Range, NumberValues);

            // Once we have the decoded values, we need to dequantize them to the 0-255 range
            // This procedure is outlined in ASTC spec C.2.13
            int OutputIndices = 0;

            foreach (IntegerEncoded IntEncoded in IntegerEncodedSequence)
            {
                int BitLength = IntEncoded.NumberBits;
                int BitValue = IntEncoded.BitValue;
                int A = 0, B = 0, C = 0, D = 0;
                // A is just the lsb replicated 9 times.
                A = BitArrayStream.Replicate(BitValue & 1, 1, 9);

                switch (IntEncoded.GetEncoding())
                {
                    case IntegerEncoded.EIntegerEncoding.JustBits:
                        {
                            OutputValues[OutputIndices++] = BitArrayStream.Replicate(BitValue, BitLength, 8);

                            break;
                        }

                    case IntegerEncoded.EIntegerEncoding.Trit:
                        {
                            D = IntEncoded.TritValue;

                            switch (BitLength)
                            {
                                case 1:
                                    {
                                        C = 204;

                                        break;
                                    }

                                case 2:
                                    {
                                        C = 93;
                                        // B = b000b0bb0
                                        int b = (BitValue >> 1) & 1;
                                        B = (b << 8) | (b << 4) | (b << 2) | (b << 1);

                                        break;
                                    }

                                case 3:
                                    {
                                        C = 44;
                                        // B = cb000cbcb
                                        int cb = (BitValue >> 1) & 3;
                                        B = (cb << 7) | (cb << 2) | cb;

                                        break;
                                    }


                                case 4:
                                    {
                                        C = 22;
                                        // B = dcb000dcb
                                        int dcb = (BitValue >> 1) & 7;
                                        B = (dcb << 6) | dcb;

                                        break;
                                    }

                                case 5:
                                    {
                                        C = 11;
                                        // B = edcb000ed
                                        int edcb = (BitValue >> 1) & 0xF;
                                        B = (edcb << 5) | (edcb >> 2);

                                        break;
                                    }

                                case 6:
                                    {
                                        C = 5;
                                        // B = fedcb000f
                                        int fedcb = (BitValue >> 1) & 0x1F;
                                        B = (fedcb << 4) | (fedcb >> 4);

                                        break;
                                    }

                                default:
                                    throw new ASTCDecoderException("Unsupported trit encoding for color values!");
                            }

                            break;
                        }

                    case IntegerEncoded.EIntegerEncoding.Quint:
                        {
                            D = IntEncoded.QuintValue;

                            switch (BitLength)
                            {
                                case 1:
                                    {
                                        C = 113;

                                        break;
                                    }

                                case 2:
                                    {
                                        C = 54;
                                        // B = b0000bb00
                                        int b = (BitValue >> 1) & 1;
                                        B = (b << 8) | (b << 3) | (b << 2);

                                        break;
                                    }

                                case 3:
                                    {
                                        C = 26;
                                        // B = cb0000cbc
                                        int cb = (BitValue >> 1) & 3;
                                        B = (cb << 7) | (cb << 1) | (cb >> 1);

                                        break;
                                    }

                                case 4:
                                    {
                                        C = 13;
                                        // B = dcb0000dc
                                        int dcb = (BitValue >> 1) & 7;
                                        B = (dcb << 6) | (dcb >> 1);

                                        break;
                                    }

                                case 5:
                                    {
                                        C = 6;
                                        // B = edcb0000e
                                        int edcb = (BitValue >> 1) & 0xF;
                                        B = (edcb << 5) | (edcb >> 3);

                                        break;
                                    }

                                default:
                                    throw new ASTCDecoderException("Unsupported quint encoding for color values!");
                            }
                            break;
                        }
                }

                if (IntEncoded.GetEncoding() != IntegerEncoded.EIntegerEncoding.JustBits)
                {
                    int T = D * C + B;
                    T ^= A;
                    T = (A & 0x80) | (T >> 2);

                    OutputValues[OutputIndices++] = T;
                }
            }
        }

        static void FillVoidExtentLDR(BitArrayStream BitStream, int[] OutputBuffer, int BlockWidth, int BlockHeight)
        {
            // Don't actually care about the void extent, just read the bits...
            for (int i = 0; i < 4; ++i)
            {
                BitStream.ReadBits(13);
            }

            // Decode the RGBA components and renormalize them to the range [0, 255]
            ushort R = (ushort)BitStream.ReadBits(16);
            ushort G = (ushort)BitStream.ReadBits(16);
            ushort B = (ushort)BitStream.ReadBits(16);
            ushort A = (ushort)BitStream.ReadBits(16);

            int RGBA = (R >> 8) | (G & 0xFF00) | ((B) & 0xFF00) << 8 | ((A) & 0xFF00) << 16;

            for (int j = 0; j < BlockHeight; j++)
            {
                for (int i = 0; i < BlockWidth; i++)
                {
                    OutputBuffer[j * BlockWidth + i] = RGBA;
                }
            }
        }

        static TexelWeightParams DecodeBlockInfo(BitArrayStream BitStream)
        {
            TexelWeightParams TexelParams = new TexelWeightParams();

            // Read the entire block mode all at once
            ushort ModeBits = (ushort)BitStream.ReadBits(11);

            // Does this match the void extent block mode?
            if ((ModeBits & 0x01FF) == 0x1FC)
            {
                if ((ModeBits & 0x200) != 0)
                {
                    TexelParams.VoidExtentHDR = true;
                }
                else
                {
                    TexelParams.VoidExtentLDR = true;
                }

                // Next two bits must be one.
                if ((ModeBits & 0x400) == 0 || BitStream.ReadBits(1) == 0)
                {
                    TexelParams.Error = true;
                }

                return TexelParams;
            }

            // First check if the last four bits are zero
            if ((ModeBits & 0xF) == 0)
            {
                TexelParams.Error = true;
                return TexelParams;
            }

            // If the last two bits are zero, then if bits
            // [6-8] are all ones, this is also reserved.
            if ((ModeBits & 0x3) == 0 && (ModeBits & 0x1C0) == 0x1C0)
            {
                TexelParams.Error = true;

                return TexelParams;
            }

            // Otherwise, there is no error... Figure out the layout
            // of the block mode. Layout is determined by a number
            // between 0 and 9 corresponding to table C.2.8 of the
            // ASTC spec.
            int Layout;
            if ((ModeBits & 0x1) != 0 || (ModeBits & 0x2) != 0)
            {
                // layout is in [0-4]
                if ((ModeBits & 0x8) != 0)
                {
                    // layout is in [2-4]
                    if ((ModeBits & 0x4) != 0)
                    {
                        // layout is in [3-4]
                        if ((ModeBits & 0x100) != 0)
                        {
                            Layout = 4;
                        }
                        else
                        {
                            Layout = 3;
                        }
                    }
                    else
                    {
                        Layout = 2;
                    }
                }
                else
                {
                    // layout is in [0-1]
                    if ((ModeBits & 0x4) != 0)
                    {
                        Layout = 1;
                    }
                    else
                    {
                        Layout = 0;
                    }
                }
            }
            else
            {
                // layout is in [5-9]
                if ((ModeBits & 0x100) != 0)
                {
                    // layout is in [7-9]
                    if ((ModeBits & 0x80) != 0)
                    {
                        if ((ModeBits & 0x20) != 0)
                        {
                            Layout = 8;
                        }
                        else
                        {
                            Layout = 7;
                        }
                    }
                    else
                    {
                        Layout = 9;
                    }
                }
                else
                {
                    // layout is in [5-6]
                    if ((ModeBits & 0x80) != 0)
                    {
                        Layout = 6;
                    }
                    else
                    {
                        Layout = 5;
                    }
                }
            }

            // Determine R
            int R = (ModeBits >> 4) & 1;
            if (Layout < 5)
            {
                R |= (ModeBits & 0x3) << 1;
            }
            else
            {
                R |= (ModeBits & 0xC) >> 1;
            }

            // Determine width & height
            switch (Layout)
            {
                case 0:
                    {
                        int A = (ModeBits >> 5) & 0x3;
                        int B = (ModeBits >> 7) & 0x3;

                        TexelParams.Width = B + 4;
                        TexelParams.Height = A + 2;

                        break;
                    }

                case 1:
                    {
                        int A = (ModeBits >> 5) & 0x3;
                        int B = (ModeBits >> 7) & 0x3;

                        TexelParams.Width = B + 8;
                        TexelParams.Height = A + 2;

                        break;
                    }

                case 2:
                    {
                        int A = (ModeBits >> 5) & 0x3;
                        int B = (ModeBits >> 7) & 0x3;

                        TexelParams.Width = A + 2;
                        TexelParams.Height = B + 8;

                        break;
                    }

                case 3:
                    {
                        int A = (ModeBits >> 5) & 0x3;
                        int B = (ModeBits >> 7) & 0x1;

                        TexelParams.Width = A + 2;
                        TexelParams.Height = B + 6;

                        break;
                    }

                case 4:
                    {
                        int A = (ModeBits >> 5) & 0x3;
                        int B = (ModeBits >> 7) & 0x1;

                        TexelParams.Width = B + 2;
                        TexelParams.Height = A + 2;

                        break;
                    }

                case 5:
                    {
                        int A = (ModeBits >> 5) & 0x3;

                        TexelParams.Width = 12;
                        TexelParams.Height = A + 2;

                        break;
                    }

                case 6:
                    {
                        int A = (ModeBits >> 5) & 0x3;

                        TexelParams.Width = A + 2;
                        TexelParams.Height = 12;

                        break;
                    }

                case 7:
                    {
                        TexelParams.Width = 6;
                        TexelParams.Height = 10;

                        break;
                    }

                case 8:
                    {
                        TexelParams.Width = 10;
                        TexelParams.Height = 6;
                        break;
                    }

                case 9:
                    {
                        int A = (ModeBits >> 5) & 0x3;
                        int B = (ModeBits >> 9) & 0x3;

                        TexelParams.Width = A + 6;
                        TexelParams.Height = B + 6;

                        break;
                    }

                default:
                    //Don't know this layout...
                    TexelParams.Error = true;
                    break;
            }

            // Determine whether or not we're using dual planes
            // and/or high precision layouts.
            bool D = ((Layout != 9) && ((ModeBits & 0x400) != 0));
            bool H = (Layout != 9) && ((ModeBits & 0x200) != 0);

            if (H)
            {
                int[] MaxWeights = { 9, 11, 15, 19, 23, 31 };
                TexelParams.MaxWeight = MaxWeights[R - 2];
            }
            else
            {
                int[] MaxWeights = { 1, 2, 3, 4, 5, 7 };
                TexelParams.MaxWeight = MaxWeights[R - 2];
            }

            TexelParams.DualPlane = D;

            return TexelParams;
        }
    }
}
