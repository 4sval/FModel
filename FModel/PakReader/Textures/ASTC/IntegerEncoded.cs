using System.Collections;
using System.Collections.Generic;

namespace FModel.PakReader.Textures.ASTC
{
    public struct IntegerEncoded
    {
        public enum EIntegerEncoding
        {
            JustBits,
            Quint,
            Trit
        }

        readonly EIntegerEncoding Encoding;
        public int NumberBits { get; private set; }
        public int BitValue { get; private set; }
        public int TritValue { get; private set; }
        public int QuintValue { get; private set; }

        public IntegerEncoded(EIntegerEncoding _Encoding, int NumBits)
        {
            Encoding = _Encoding;
            NumberBits = NumBits;
            BitValue = 0;
            TritValue = 0;
            QuintValue = 0;
        }

        public bool MatchesEncoding(IntegerEncoded Other)
        {
            return Encoding == Other.Encoding && NumberBits == Other.NumberBits;
        }

        public EIntegerEncoding GetEncoding()
        {
            return Encoding;
        }

        public int GetBitLength(int NumberVals)
        {
            int TotalBits = NumberBits * NumberVals;
            if (Encoding == EIntegerEncoding.Trit)
            {
                TotalBits += (NumberVals * 8 + 4) / 5;
            }
            else if (Encoding == EIntegerEncoding.Quint)
            {
                TotalBits += (NumberVals * 7 + 2) / 3;
            }
            return TotalBits;
        }

        public static IntegerEncoded CreateEncoding(int MaxVal)
        {
            while (MaxVal > 0)
            {
                int Check = MaxVal + 1;

                // Is maxVal a power of two?
                if ((Check & (Check - 1)) == 0)
                {
                    return new IntegerEncoded(EIntegerEncoding.JustBits, BitArrayStream.PopCnt(MaxVal));
                }

                // Is maxVal of the type 3*2^n - 1?
                if ((Check % 3 == 0) && ((Check / 3) & ((Check / 3) - 1)) == 0)
                {
                    return new IntegerEncoded(EIntegerEncoding.Trit, BitArrayStream.PopCnt(Check / 3 - 1));
                }

                // Is maxVal of the type 5*2^n - 1?
                if ((Check % 5 == 0) && ((Check / 5) & ((Check / 5) - 1)) == 0)
                {
                    return new IntegerEncoded(EIntegerEncoding.Quint, BitArrayStream.PopCnt(Check / 5 - 1));
                }

                // Apparently it can't be represented with a bounded integer sequence...
                // just iterate.
                MaxVal--;
            }

            return new IntegerEncoded(EIntegerEncoding.JustBits, 0);
        }

        public static void DecodeTritBlock(
            BitArrayStream BitStream,
            List<IntegerEncoded> ListIntegerEncoded,
            int NumberBitsPerValue)
        {
            // Implement the algorithm in section C.2.12
            int[] m = new int[5];
            int[] t = new int[5];
            int T;

            // Read the trit encoded block according to
            // table C.2.14
            m[0] = BitStream.ReadBits(NumberBitsPerValue);
            T = BitStream.ReadBits(2);
            m[1] = BitStream.ReadBits(NumberBitsPerValue);
            T |= BitStream.ReadBits(2) << 2;
            m[2] = BitStream.ReadBits(NumberBitsPerValue);
            T |= BitStream.ReadBits(1) << 4;
            m[3] = BitStream.ReadBits(NumberBitsPerValue);
            T |= BitStream.ReadBits(2) << 5;
            m[4] = BitStream.ReadBits(NumberBitsPerValue);
            T |= BitStream.ReadBits(1) << 7;

            int C;
            BitArrayStream Tb = new BitArrayStream(new BitArray(new int[] { T }));
            if (Tb.ReadBits(2, 4) == 7)
            {
                C = (Tb.ReadBits(5, 7) << 2) | Tb.ReadBits(0, 1);
                t[4] = t[3] = 2;
            }
            else
            {
                C = Tb.ReadBits(0, 4);
                if (Tb.ReadBits(5, 6) == 3)
                {
                    t[4] = 2;
                    t[3] = Tb.ReadBit(7);
                }
                else
                {
                    t[4] = Tb.ReadBit(7);
                    t[3] = Tb.ReadBits(5, 6);
                }
            }

            BitArrayStream Cb = new BitArrayStream(new BitArray(new int[] { C }));
            if (Cb.ReadBits(0, 1) == 3)
            {
                t[2] = 2;
                t[1] = Cb.ReadBit(4);
                t[0] = (Cb.ReadBit(3) << 1) | (Cb.ReadBit(2) & ~Cb.ReadBit(3));
            }
            else if (Cb.ReadBits(2, 3) == 3)
            {
                t[2] = 2;
                t[1] = 2;
                t[0] = Cb.ReadBits(0, 1);
            }
            else
            {
                t[2] = Cb.ReadBit(4);
                t[1] = Cb.ReadBits(2, 3);
                t[0] = (Cb.ReadBit(1) << 1) | (Cb.ReadBit(0) & ~Cb.ReadBit(1));
            }

            for (int i = 0; i < 5; i++)
            {
                IntegerEncoded IntEncoded = new IntegerEncoded(EIntegerEncoding.Trit, NumberBitsPerValue)
                {
                    BitValue = m[i],
                    TritValue = t[i]
                };
                ListIntegerEncoded.Add(IntEncoded);
            }
        }

        public static void DecodeQuintBlock(
            BitArrayStream BitStream,
            List<IntegerEncoded> ListIntegerEncoded,
            int NumberBitsPerValue)
        {
            // Implement the algorithm in section C.2.12
            int[] m = new int[3];
            int[] q = new int[3];
            int Q;

            // Read the trit encoded block according to
            // table C.2.15
            m[0] = BitStream.ReadBits(NumberBitsPerValue);
            Q = BitStream.ReadBits(3);
            m[1] = BitStream.ReadBits(NumberBitsPerValue);
            Q |= BitStream.ReadBits(2) << 3;
            m[2] = BitStream.ReadBits(NumberBitsPerValue);
            Q |= BitStream.ReadBits(2) << 5;

            BitArrayStream Qb = new BitArrayStream(new BitArray(new int[] { Q }));
            if (Qb.ReadBits(1, 2) == 3 && Qb.ReadBits(5, 6) == 0)
            {
                q[0] = q[1] = 4;
                q[2] = (Qb.ReadBit(0) << 2) | ((Qb.ReadBit(4) & ~Qb.ReadBit(0)) << 1) | (Qb.ReadBit(3) & ~Qb.ReadBit(0));
            }
            else
            {
                int C;
                if (Qb.ReadBits(1, 2) == 3)
                {
                    q[2] = 4;
                    C = (Qb.ReadBits(3, 4) << 3) | ((~Qb.ReadBits(5, 6) & 3) << 1) | Qb.ReadBit(0);
                }
                else
                {
                    q[2] = Qb.ReadBits(5, 6);
                    C = Qb.ReadBits(0, 4);
                }

                BitArrayStream Cb = new BitArrayStream(new BitArray(new int[] { C }));
                if (Cb.ReadBits(0, 2) == 5)
                {
                    q[1] = 4;
                    q[0] = Cb.ReadBits(3, 4);
                }
                else
                {
                    q[1] = Cb.ReadBits(3, 4);
                    q[0] = Cb.ReadBits(0, 2);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                IntegerEncoded IntEncoded = new IntegerEncoded(EIntegerEncoding.Quint, NumberBitsPerValue)
                {
                    BitValue = m[i],
                    QuintValue = q[i]
                };
                ListIntegerEncoded.Add(IntEncoded);
            }
        }

        public static void DecodeIntegerSequence(
            List<IntegerEncoded> DecodeIntegerSequence,
            BitArrayStream BitStream,
            int MaxRange,
            int NumberValues)
        {
            // Determine encoding parameters
            IntegerEncoded IntEncoded = CreateEncoding(MaxRange);

            // Start decoding
            int NumberValuesDecoded = 0;
            while (NumberValuesDecoded < NumberValues)
            {
                switch (IntEncoded.GetEncoding())
                {
                    case EIntegerEncoding.Quint:
                        {
                            DecodeQuintBlock(BitStream, DecodeIntegerSequence, IntEncoded.NumberBits);
                            NumberValuesDecoded += 3;

                            break;
                        }

                    case EIntegerEncoding.Trit:
                        {
                            DecodeTritBlock(BitStream, DecodeIntegerSequence, IntEncoded.NumberBits);
                            NumberValuesDecoded += 5;

                            break;
                        }

                    case EIntegerEncoding.JustBits:
                        {
                            IntEncoded.BitValue = BitStream.ReadBits(IntEncoded.NumberBits);
                            DecodeIntegerSequence.Add(IntEncoded);
                            NumberValuesDecoded++;

                            break;
                        }
                }
            }
        }
    }
}
