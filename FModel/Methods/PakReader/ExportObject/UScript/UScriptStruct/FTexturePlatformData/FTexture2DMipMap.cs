using Newtonsoft.Json;
using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FTexture2DMipMap
    {
        [JsonIgnore]
        public FByteBulkData data;
        public int size_x;
        public int size_y;
        public int size_z;

        internal FTexture2DMipMap(BinaryReader reader, BinaryReader ubulk, long bulk_offset)
        {
            int cooked = reader.ReadInt32();
            data = new FByteBulkData(reader, ubulk, bulk_offset);
            size_x = reader.ReadInt32();
            size_y = reader.ReadInt32();
            size_z = reader.ReadInt32();
            if (cooked != 1)
            {
                read_string(reader);
            }
        }
    }
}
