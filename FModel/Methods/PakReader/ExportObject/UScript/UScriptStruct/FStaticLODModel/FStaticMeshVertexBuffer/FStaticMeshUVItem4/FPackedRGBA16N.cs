using System.IO;

namespace PakReader
{
    public struct FPackedRGBA16N
    {
        public ushort X;
        public ushort Y;
        public ushort Z;
        public ushort W;

        public FPackedRGBA16N(BinaryReader reader)
        {
            X = reader.ReadUInt16();
            Y = reader.ReadUInt16();
            Z = reader.ReadUInt16();
            W = reader.ReadUInt16();

            X ^= 0x8000; // 4.20+: https://github.com/gildor2/UModel/blob/dcdb92c987c15f0a5d3366247667a8fb9fd8008b/Unreal/UnCore.h#L1290
            Y ^= 0x8000;
            Z ^= 0x8000;
            W ^= 0x8000;
        }

        public FPackedNormal ToPackedNormal() => new FVector
        {
            X = (X - 32767.5f) / 32767.5f,
            Y = (Y - 32767.5f) / 32767.5f,
            Z = (Z - 32767.5f) / 32767.5f
        };
    }
}
