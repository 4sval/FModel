using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FChunkHeader : IUStruct
    {
        public readonly uint ChunkId;
        public readonly uint ChunkDataSize;

        internal FChunkHeader(BinaryReader reader)
        {
            ChunkId = reader.ReadUInt32();
            ChunkDataSize = reader.ReadUInt32();
        }
    }
}
