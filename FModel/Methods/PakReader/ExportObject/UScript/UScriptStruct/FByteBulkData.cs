using System.IO;

namespace PakReader
{
    public struct FByteBulkData
    {
        public FByteBulkDataHeader header;
        public byte[] data;

        internal FByteBulkData(BinaryReader reader, BinaryReader ubulk, long bulk_offset)
        {
            header = new FByteBulkDataHeader(reader);

            data = null;
            if ((header.bulk_data_flags & 0x0040) != 0)
            {
                data = reader.ReadBytes(header.element_count);
            }
            if ((header.bulk_data_flags & 0x0100) != 0)
            {
                if (ubulk == null)
                {
                    throw new IOException("No ubulk specified");
                }
                // Archive seems "kind of" appended.
                ubulk.BaseStream.Seek(header.offset_in_file + bulk_offset, SeekOrigin.Begin);
                data = ubulk.ReadBytes(header.element_count);
            }

            if (data == null)
            {
                throw new IOException("Could not read data");
            }
        }
    }
}
