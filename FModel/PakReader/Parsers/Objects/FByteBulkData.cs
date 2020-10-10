using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FByteBulkData : IUStruct
    {
        public readonly byte[] Data;

        internal FByteBulkData(BinaryReader reader, Stream ubulk, long ubulkOffset)
        {
            var BulkDataFlags = reader.ReadUInt32();

            var ElementCount = reader.ReadInt32();
            _ = reader.ReadInt32(); //BulkDataSizeOnDisk
            var BulkDataOffsetInFile = reader.ReadInt64();

            Data = null;
            if ((BulkDataFlags & 0x20) != 0 || ElementCount == 0)
                return;
            if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_OptionalPayload) != 0) //.uptnl
                return;

            if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_PayloadAtEndOfFile) != 0 &&
                BulkDataOffsetInFile > 0 && ElementCount > 0 &&
                BulkDataOffsetInFile + ElementCount <= reader.BaseStream.Length) //.uasset
            {
                long rememberMe = reader.BaseStream.Position;
                reader.BaseStream.Seek(BulkDataOffsetInFile, SeekOrigin.Begin);
                Data = reader.ReadBytes(ElementCount);
                reader.BaseStream.Seek(rememberMe, SeekOrigin.Begin);
            }
            else if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_ForceInlinePayload) != 0) //.uexp
            {
                Data = reader.ReadBytes(ElementCount);
            }
            else if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_PayloadInSeperateFile) != 0 &&
                ubulk != null && BulkDataOffsetInFile + ubulkOffset >= 0) //.ubulk
            {
                if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_NoOffsetFixUp) == 0) // UE4.26 flag
                {
                    ubulk.Position = BulkDataOffsetInFile + ubulkOffset;
                }
                Data = new byte[ElementCount];
                ubulk.Read(Data, 0, (int)ElementCount);
            }
        }
    }
}
