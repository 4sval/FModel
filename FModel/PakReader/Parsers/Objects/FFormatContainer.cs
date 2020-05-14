using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FFormatContainer : IUStruct
    {
        public readonly FName FormatName;
        public readonly FByteBulkData Data;

        internal FFormatContainer(PackageReader reader, Stream ubulk, long ubulkOffset)
        {
            FormatName = reader.ReadFName();
            Data = new FByteBulkData(reader, ubulk, ubulkOffset);
        }
    }
}
