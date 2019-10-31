using System.IO;

namespace PakReader
{
    public struct FByteBulkDataHeader
    {
        public int bulk_data_flags;
        public int element_count;
        public int size_on_disk;
        public long offset_in_file;

        internal FByteBulkDataHeader(BinaryReader reader)
        {
            bulk_data_flags = reader.ReadInt32();
            element_count = reader.ReadInt32();
            size_on_disk = reader.ReadInt32();
            offset_in_file = reader.ReadInt64();
        }
    }
}
