using System.IO;

namespace PakReader
{
    public struct FRuntimeSkinWeightProfileData
    {
        public FSkinWeightOverrideInfo[] overrides_info;
        public ushort[] weights;
        public (uint, uint)[] vertex_index_override_index;

        public FRuntimeSkinWeightProfileData(BinaryReader reader)
        {
            overrides_info = reader.ReadTArray(() => new FSkinWeightOverrideInfo(reader));
            weights = reader.ReadTArray(() => reader.ReadUInt16());
            vertex_index_override_index = new (uint, uint)[reader.ReadInt32()];
            for (int i = 0; i < vertex_index_override_index.Length; i++)
            {
                vertex_index_override_index[i] = (reader.ReadUInt32(), reader.ReadUInt32());
            }
        }
    }
}
