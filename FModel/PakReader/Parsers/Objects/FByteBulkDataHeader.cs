using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FByteBulkDataHeader : IUStruct
    {
        public readonly int BulkDataFlags;
        public readonly int ElementCount;
        public readonly long SizeOnDisk;
        public readonly long OffsetInFile;

        internal FByteBulkDataHeader(BinaryReader reader, long ubulkOffset)
        {
            BulkDataFlags = reader.ReadInt32();
            if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_Size64Bit) != 0)
            {
                ElementCount = (int)reader.ReadInt64();
                SizeOnDisk = reader.ReadInt64();
            }
            else
            {
                ElementCount = reader.ReadInt32();
                SizeOnDisk = reader.ReadInt32();
            }

            OffsetInFile = reader.ReadInt64();
            if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_NoOffsetFixUp) == 0) // UE4.26 flag
            {
                OffsetInFile += ubulkOffset;
            }

            if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_BadDataVersion) != 0)
            {
                reader.ReadBytes(2);
            }
        }
    }
}
