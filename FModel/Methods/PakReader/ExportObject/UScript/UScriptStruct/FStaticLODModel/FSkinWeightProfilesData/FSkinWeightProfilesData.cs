using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FSkinWeightProfilesData
    {
        public (string, FRuntimeSkinWeightProfileData)[] override_data;

        internal FSkinWeightProfilesData(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            override_data = new (string, FRuntimeSkinWeightProfileData)[reader.ReadInt32()];
            for (int i = 0; i < override_data.Length; i++)
            {
                override_data[i] = (read_fname(reader, name_map), new FRuntimeSkinWeightProfileData(reader));
            }
        }
    }
}
