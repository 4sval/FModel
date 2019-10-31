using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public class FSoundFormatData
    {
        public string name;
        public FByteBulkData data;

        internal FSoundFormatData(BinaryReader reader, FNameEntrySerialized[] name_map, int asset_file_size, long export_size, BinaryReader ubulk)
        {
            name = read_fname(reader, name_map);
            data = new FByteBulkData(reader, ubulk, export_size + asset_file_size);
        }
    }
}
