using System.IO;

namespace PakReader
{
    public struct FMeshUVChannelInfo
    {
        public bool initialized;
        public bool override_densities;
        public float[] local_uv_densities;

        internal FMeshUVChannelInfo(BinaryReader reader)
        {
            initialized = reader.ReadUInt32() != 0;
            override_densities = reader.ReadUInt32() != 0;
            local_uv_densities = new float[4];
            for (int i = 0; i < 4; i++)
            {
                local_uv_densities[i] = reader.ReadSingle();
            }
        }
    }
}
