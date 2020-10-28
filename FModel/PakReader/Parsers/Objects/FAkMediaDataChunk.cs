using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FAkMediaDataChunk : IUStruct
    {
		public readonly FByteBulkData Data;
		public readonly bool IsPrefetch;

		internal FAkMediaDataChunk(PackageReader reader, Stream ubulk, long bulkOffset)
		{
			IsPrefetch = reader.ReadInt32() != 0;
			Data = new FByteBulkData(reader, ubulk, bulkOffset);
		}
	}
}
