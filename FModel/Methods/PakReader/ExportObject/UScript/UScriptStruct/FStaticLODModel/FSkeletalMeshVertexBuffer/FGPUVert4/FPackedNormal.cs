using System.IO;

namespace PakReader
{
    public struct FPackedNormal
    {
        public uint Data;

        public FPackedNormal(BinaryReader reader)
        {
            Data = reader.ReadUInt32();
            Data ^= 0x80808080; // 4.20+: https://github.com/gildor2/UModel/blob/dcdb92c987c15f0a5d3366247667a8fb9fd8008b/Unreal/UnCore.h#L1216
        }

        public static implicit operator FPackedNormal(FVector V) => new FPackedNormal
        {
            Data = (uint)((int)((V.X + 1) * 127.5f)
                     + ((int)((V.Y + 1) * 127.5f) << 8)
                     + ((int)((V.Z + 1) * 127.5f) << 16))
        };

        public static implicit operator FPackedNormal(FVector4 V) => new FPackedNormal
        {
            Data = (uint)((int)((V.X + 1) * 127.5f)
                     + ((int)((V.Y + 1) * 127.5f) << 8)
                     + ((int)((V.Z + 1) * 127.5f) << 16)
                     + ((int)((V.W + 1) * 127.5f) << 24))
        };

        public static implicit operator FPackedNormal(CPackedNormal me) => new FPackedNormal
        {
            Data = me.Data ^ 0x80808080
        };
    }
}
