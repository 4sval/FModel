using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FSoftObjectPathMap
    {
        public string asset_path_name;
        public string sub_path_string;

        internal FSoftObjectPathMap(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            asset_path_name = read_fname(reader, name_map);
            sub_path_string = read_string(reader);
        }
    }
}
