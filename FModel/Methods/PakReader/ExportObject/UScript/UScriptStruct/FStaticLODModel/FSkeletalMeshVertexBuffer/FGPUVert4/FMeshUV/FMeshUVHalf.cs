using System.IO;

namespace PakReader
{
    public struct FMeshUVHalf
    {
        public ushort U;
        public ushort V;

        public FMeshUVHalf(BinaryReader reader)
        {
            U = reader.ReadUInt16();
            V = reader.ReadUInt16();
        }

        public static explicit operator FMeshUVFloat(FMeshUVHalf me)
        {
            return new FMeshUVFloat
            {
                U = Extensions.HalfToFloat(me.U),
                V = Extensions.HalfToFloat(me.V)
            };
        }
    }
}
