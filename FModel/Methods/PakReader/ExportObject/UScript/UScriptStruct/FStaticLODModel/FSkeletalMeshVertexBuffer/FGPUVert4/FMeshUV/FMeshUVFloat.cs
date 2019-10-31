using System.IO;

namespace PakReader
{
    public struct FMeshUVFloat
    {
        public float U;
        public float V;

        public FMeshUVFloat(BinaryReader reader)
        {
            U = reader.ReadSingle();
            V = reader.ReadSingle();
        }

        public static implicit operator CMeshUVFloat(FMeshUVFloat me)
        {
            return new CMeshUVFloat
            {
                U = me.U,
                V = me.V
            };
        }
    }
}
