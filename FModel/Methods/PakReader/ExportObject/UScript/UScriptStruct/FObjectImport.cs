using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FObjectImport
    {
        public string class_package;
        public string class_name;
        public FPackageIndex outer_index;
        public string object_name;

        internal FObjectImport(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            class_package = read_fname(reader, name_map);
            class_name = read_fname(reader, name_map);
            outer_index = new FPackageIndex(reader, import_map);
            object_name = read_fname(reader, name_map);
        }
    }
}
