using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FSmartName
    {
        public string display_name;

        internal FSmartName(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            display_name = read_fname(reader, name_map);
        }
    }
}
