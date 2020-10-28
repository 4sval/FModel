namespace FModel.PakReader.IO
{
    public class FIoStoreEntry : ReaderEntry
    {
        public readonly FFileIoStoreReader ioStore;
        public override string ContainerName => ioStore.FileName;
        public override string Name { get; }
        public readonly uint UserData;
        
        public FIoChunkId ChunkId => ioStore.TocResource.ChunkIds[UserData];
        public FIoOffsetAndLength OffsetLength => ioStore.Toc[ChunkId];
        public long Offset => (long) OffsetLength.Offset;
        public long Length => (long) OffsetLength.Length;

        public FIoStoreEntry(FFileIoStoreReader ioStore, uint userData, string name, bool caseSensitive)
        {
            this.ioStore = ioStore;
            UserData = userData;
            if (!caseSensitive)
                name = name.ToLowerInvariant();
            if (name.StartsWith('/'))
                name = name.Substring(1);
            Name = name;
        }

        public byte[] GetData() => ioStore.Read(ChunkId);
    }
}