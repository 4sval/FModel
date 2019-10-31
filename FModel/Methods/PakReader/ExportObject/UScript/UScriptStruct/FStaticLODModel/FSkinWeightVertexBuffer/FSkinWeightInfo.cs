using System.IO;

namespace PakReader
{
    public struct FSkinWeightInfo
    {
        public byte[] bone_index;
        public byte[] bone_weight;

        public FSkinWeightInfo(BinaryReader reader, int influences = 4) // NUM_INFLUENCES_UE4 = 4
        {
            bone_index = reader.ReadBytes(influences);
            bone_weight = reader.ReadBytes(influences);
        }
    }
}
