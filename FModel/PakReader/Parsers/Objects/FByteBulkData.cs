using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FByteBulkData : IUStruct
    {
        public readonly FByteBulkDataHeader Header;
        public readonly byte[] Data;

        internal FByteBulkData(BinaryReader reader, Stream ubulk, long ubulkOffset)
        {
            Header = new FByteBulkDataHeader(reader, ubulkOffset);
            var bulkDataFlags = Header.BulkDataFlags;

            Data = new byte[Header.ElementCount];
            if (Header.ElementCount == 0)
            {
                return;
            }
            else if ((bulkDataFlags & (uint)EBulkDataFlags.BULKDATA_Unused) != 0)
            {
                return;
            }
            else if ((bulkDataFlags & (uint)EBulkDataFlags.BULKDATA_OptionalPayload) != 0) //.uptnl
            {
                return;
            }
            else if ((bulkDataFlags & (uint)EBulkDataFlags.BULKDATA_ForceInlinePayload) != 0) //.uexp
            {
                reader.Read(Data, 0, Header.ElementCount);
            }
            else if((bulkDataFlags & (uint)EBulkDataFlags.BULKDATA_PayloadInSeperateFile) != 0) //.ubulk
            {
                ubulk.Position = Header.OffsetInFile;
                ubulk.Read(Data, 0, Header.ElementCount);
            }
            else if((bulkDataFlags & (uint)EBulkDataFlags.BULKDATA_PayloadAtEndOfFile) != 0) //.uexp
            {
                var savePos = reader.BaseStream.Position;
                if (Header.OffsetInFile + Header.ElementCount <= reader.BaseStream.Length)
                {
                    reader.BaseStream.Position = Header.OffsetInFile;
                    reader.Read(Data, 0, Header.ElementCount);
                }
                reader.BaseStream.Position = savePos;
            }
        }
    }
}
