using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FByteBulkData : IUStruct
    {
        // Memory saving, we don't need this
        //uint BulkDataFlags;
        //long ElementCount;
        //long BulkDataOffsetInFile;
        //long BulkDataSizeOnDisk;

        public readonly byte[] Data;

        internal FByteBulkData(BinaryReader reader, Stream ubulk, long ubulkOffset)
        {
            var BulkDataFlags = reader.ReadUInt32();

            var ElementCount = reader.ReadInt32();
            _ = reader.ReadInt32(); //BulkDataSizeOnDisk
            var BulkDataOffsetInFile = reader.ReadInt64();

            Data = null;
            if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_ForceInlinePayload) != 0)
            {
                Data = reader.ReadBytes((int)ElementCount);
            }
            
            if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_PayloadInSeperateFile) != 0)
            {
                ubulk.Position = BulkDataOffsetInFile + ubulkOffset;
                Data = new byte[ElementCount];
                ubulk.Read(Data, 0, (int)ElementCount);
            }
        }
    }
}
