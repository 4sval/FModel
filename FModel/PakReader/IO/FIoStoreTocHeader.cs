using System.IO;
using System.Linq;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.IO
{
    public enum EIoStoreTocVersion
    {
        Invalid = 0,
        Initial,
        DirectoryIndex,
        PartitionSize,
        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }

    public readonly struct FIoStoreTocHeader
    {
        public static byte[] TOC_MAGIC = { 0x2D, 0x3D, 0x3D, 0x2D, 0x2D, 0x3D, 0x3D, 0x2D, 0x2D, 0x3D, 0x3D, 0x2D, 0x2D, 0x3D, 0x3D, 0x2D };

        public readonly byte[] TocMagic;
        public readonly EIoStoreTocVersion Version;
        public readonly uint TocHeaderSize;
        public readonly uint TocEntryCount;
        public readonly uint TocCompressedBlockEntryCount;
        public readonly uint TocCompressedBlockEntrySize;	// For sanity checking
        public readonly uint CompressionMethodNameCount;
        public readonly uint CompressionMethodNameLength;
        public readonly uint CompressionBlockSize;
        public readonly uint DirectoryIndexSize;
        public readonly uint PartitionCount;
        public readonly FIoContainerId ContainerId;
        public readonly FGuid EncryptionKeyGuid;
        public readonly EIoContainerFlags ContainerFlags;

        public FIoStoreTocHeader(BinaryReader reader)
        {
            TocMagic = reader.ReadBytes(16);

            if (!TOC_MAGIC.SequenceEqual(TocMagic))
                throw new FileLoadException("Invalid utoc magic");

            Version = (EIoStoreTocVersion) reader.ReadInt32();
            TocHeaderSize = reader.ReadUInt32();
            TocEntryCount = reader.ReadUInt32();
            TocCompressedBlockEntryCount = reader.ReadUInt32();
            TocCompressedBlockEntrySize = reader.ReadUInt32();
            CompressionMethodNameCount = reader.ReadUInt32();
            CompressionMethodNameLength = reader.ReadUInt32();
            CompressionBlockSize = reader.ReadUInt32();
            DirectoryIndexSize = reader.ReadUInt32();
            PartitionCount = reader.ReadUInt32();
            ContainerId = new FIoContainerId(reader);
            EncryptionKeyGuid = new FGuid(reader);
            ContainerFlags = (EIoContainerFlags) reader.ReadInt32();
            //reader.BaseStream.Position += 60; // Padding
        }
    }
}