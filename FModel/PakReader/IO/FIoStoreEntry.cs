using FModel.Utils;

namespace FModel.PakReader.IO
{
    public class FIoStoreEntry : ReaderEntry
    {
        public readonly FFileIoStoreReader ioStore;
        public readonly uint UserData;

        public override string ContainerName => ioStore.FileName;
        public override string Name { get; }
        public override long Size { get; }
        public override long UncompressedSize { get; }
        public override int StructSize { get; }
        public override uint CompressionMethodIndex { get; }
        public override bool Encrypted { get; }
        
        public FIoChunkId ChunkId => ioStore.TocResource.ChunkIds[UserData];
        public FIoOffsetAndLength OffsetLength => ioStore.Toc[ChunkId];
        public override long Offset => (long) OffsetLength.Offset;
        public long Length => (long) OffsetLength.Length;
        public string CompressionMethodString => ioStore.TocResource.CompressionMethods[CompressionMethodIndex > 0 ? CompressionMethodIndex - 1 : CompressionMethodIndex];

        public FIoStoreEntry(FFileIoStoreReader ioStore, uint userData, string name, bool caseSensitive)
        {
            this.ioStore = ioStore;
            UserData = userData;
            if (!caseSensitive)
                name = name.ToLowerInvariant();
            if (name.StartsWith('/'))
                name = name[1..];
            Name = name;

            StructSize = 0;
            Size = 0;
            UncompressedSize = 0;

            var compressionBlockSize = ioStore.TocResource.Header.CompressionBlockSize;
            var firstBlockIndex = (int)(Offset / compressionBlockSize);
            var lastBlockIndex = (int)((BinaryHelper.Align((long)Offset + Length, compressionBlockSize) - 1) / compressionBlockSize);
            for (int blockIndex = firstBlockIndex; blockIndex <= lastBlockIndex; blockIndex++)
            {
                var compressionBlock = ioStore.TocResource.CompressionBlocks[blockIndex];
                UncompressedSize += compressionBlock.UncompressedSize;
                CompressionMethodIndex = compressionBlock.CompressionMethodIndex;

                var rawSize = BinaryHelper.Align(compressionBlock.CompressedSize, AESDecryptor.ALIGN);
                Size += rawSize;

                if (ioStore.TocResource.Header.ContainerFlags.HasAnyFlags(EIoContainerFlags.Encrypted))
                {
                    Encrypted = true;
                }
            }
        }

        public byte[] GetData() => ioStore.Read(ChunkId);
    }
}