using System.IO;

namespace PakReader
{
    public struct FGuid
    {
        public uint A, B, C, D;

        public FGuid(BinaryReader reader)
        {
            A = reader.ReadUInt32();
            B = reader.ReadUInt32();
            C = reader.ReadUInt32();
            D = reader.ReadUInt32();
        }

        public override bool Equals(object obj) => obj is FGuid ? this == (FGuid)obj : false;

        public override int GetHashCode()
        {
            return (int)(((long)A + B + C + D) % uint.MaxValue - int.MaxValue);
        }

        public override string ToString()
        {
            return $"{A}-{B}-{C}-{D}";
        }

        public static bool operator ==(FGuid a, FGuid b) =>
            a.A == b.A &&
            a.B == b.B &&
            a.C == b.C &&
            a.D == b.D;

        public static bool operator !=(FGuid a, FGuid b) =>
            a.A != b.A ||
            a.B != b.B ||
            a.C != b.C ||
            a.D != b.D;
    }
}
