using System.IO;

namespace PakReader.Pak.IO
{
    public readonly struct FIoStoreTocEntryMeta
    {
        public const int SIZE = 32 + 4;
        
        public readonly FIoChunkHash ChunkHash;
        public readonly FIoStoreTocEntryMetaFlags Flags;

        public FIoStoreTocEntryMeta(BinaryReader reader)
        {
            ChunkHash = new FIoChunkHash(reader);
            Flags = (FIoStoreTocEntryMetaFlags) reader.ReadByte();
        }
    }
}