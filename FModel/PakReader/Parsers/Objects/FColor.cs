using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FColor : IUStruct
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;
        public readonly string Hex => A == 0 || A == 255 ?
            BinaryHelper.ToHex(R, G, B) :
            BinaryHelper.ToHex(A, R, G, B);

        internal FColor(BinaryReader reader)
        {
            R = reader.ReadByte();
            G = reader.ReadByte();
            B = reader.ReadByte();
            A = reader.ReadByte();
        }
    }
}
