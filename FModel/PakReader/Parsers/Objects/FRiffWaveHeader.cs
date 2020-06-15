using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FRiffWaveHeader : IUStruct
    {
        public readonly uint ChunkId;
        public readonly uint ChunkDataSize;
        public readonly uint TypeId;

        internal FRiffWaveHeader(BinaryReader reader)
        {
            ChunkId = reader.ReadUInt32();
            ChunkDataSize = reader.ReadUInt32();
            TypeId = reader.ReadUInt32();
        }
    }
}
