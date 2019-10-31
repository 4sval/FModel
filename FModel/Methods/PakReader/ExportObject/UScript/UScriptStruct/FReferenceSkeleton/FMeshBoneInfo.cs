using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FMeshBoneInfo
    {
        public string name;
        public int parent_index;

        internal FMeshBoneInfo(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            name = read_fname(reader, name_map);
            parent_index = reader.ReadInt32();
        }
    }
}
