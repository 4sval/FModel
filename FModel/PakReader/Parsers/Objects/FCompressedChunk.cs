using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FCompressedChunk : IUStruct
    {
        public readonly int UncompressedOffset;
        public readonly int UncompressedSize;
        public readonly int CompressedOffset;
        public readonly int CompressedSize;

        internal FCompressedChunk(BinaryReader reader)
        {
            UncompressedOffset = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
            CompressedOffset = reader.ReadInt32();
            CompressedSize = reader.ReadInt32();
        }
    }
}
