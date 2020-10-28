using System;

namespace FModel.PakReader.Textures.ASTC
{
    class ASTCPixel
    {
        public short R { get; set; }
        public short G { get; set; }
        public short B { get; set; }
        public short A { get; set; }

        readonly byte[] BitDepth = new byte[4];

        public ASTCPixel(short _A, short _R, short _G, short _B)
        {
            A = _A;
            R = _R;
            G = _G;
            B = _B;

            for (int i = 0; i < 4; i++)
                BitDepth[i] = 8;
        }

        public void ClampByte()
        {
            R = Math.Min(Math.Max(R, (short)0), (short)255);
            G = Math.Min(Math.Max(G, (short)0), (short)255);
            B = Math.Min(Math.Max(B, (short)0), (short)255);
            A = Math.Min(Math.Max(A, (short)0), (short)255);
        }

        public short GetComponent(int Index)
        {
            return Index switch
            {
                0 => A,
                1 => R,
                2 => G,
                3 => B,
                _ => 0,
            };
        }

        public void SetComponent(int Index, int Value)
        {
            switch (Index)
            {
                case 0:
                    A = (short)Value;
                    break;
                case 1:
                    R = (short)Value;
                    break;
                case 2:
                    G = (short)Value;
                    break;
                case 3:
                    B = (short)Value;
                    break;
            }
        }

        public void ChangeBitDepth(byte[] Depth)
        {
            for (int i = 0; i < 4; i++)
            {
                int Value = ChangeBitDepth(GetComponent(i), BitDepth[i], Depth[i]);

                SetComponent(i, Value);
                BitDepth[i] = Depth[i];
            }
        }

        short ChangeBitDepth(short Value, byte OldDepth, byte NewDepth)
        {
            if (OldDepth == NewDepth)
            {
                // Do nothing
                return Value;
            }
            else if (OldDepth == 0 && NewDepth != 0)
            {
                return (short)((1 << NewDepth) - 1);
            }
            else if (NewDepth > OldDepth)
            {
                return (short)BitArrayStream.Replicate(Value, OldDepth, NewDepth);
            }
            else
            {
                // oldDepth > newDepth
                if (NewDepth == 0)
                {
                    return 0xFF;
                }
                else
                {
                    byte BitsWasted = (byte)(OldDepth - NewDepth);
                    short TempValue = Value;

                    TempValue = (short)((TempValue + (1 << (BitsWasted - 1))) >> BitsWasted);
                    TempValue = Math.Min(Math.Max((short)0, TempValue), (short)((1 << NewDepth) - 1));

                    return (byte)(TempValue);
                }
            }
        }

        public int Pack()
        {
            ASTCPixel NewPixel = new ASTCPixel(A, R, G, B);
            byte[] eightBitDepth = { 8, 8, 8, 8 };

            NewPixel.ChangeBitDepth(eightBitDepth);

            return (byte)NewPixel.A << 24 |
                   (byte)NewPixel.B << 16 |
                   (byte)NewPixel.G << 8 |
                   (byte)NewPixel.R << 0;
        }

        // Adds more precision to the blue channel as described
        // in C.2.14
        public static ASTCPixel BlueContract(int a, int r, int g, int b)
        {
            return new ASTCPixel((short)(a),
                                 (short)((r + b) >> 1),
                                 (short)((g + b) >> 1),
                                 (short)(b));
        }
    }
}
