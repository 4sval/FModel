using System;
using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FLinearColor : IUStruct
    {
        public readonly float R;
        public readonly float G;
        public readonly float B;
        public readonly float A;
        public readonly string Hex => A == 1 || A == 0 ?
            BinaryHelper.ToHex((byte)Math.Round(R * 255), (byte)Math.Round(G * 255), (byte)Math.Round(B * 255)) :
            BinaryHelper.ToHex((byte)Math.Round(A * 255), (byte)Math.Round(R * 255), (byte)Math.Round(G * 255), (byte)Math.Round(B * 255));

        internal FLinearColor(BinaryReader reader)
        {
            R = reader.ReadSingle();
            G = reader.ReadSingle();
            B = reader.ReadSingle();
            A = reader.ReadSingle();
        }
    }
}
