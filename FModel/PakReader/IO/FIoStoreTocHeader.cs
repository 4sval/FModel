using System.IO;
using System.Linq;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.IO
{
    public enum EIoStoreTocVersion : byte
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
        public readonly byte Reserved0;
        public readonly ushort Reserved1;
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
        public readonly byte Reserved3;
        public readonly ushort Reserved4;
        public readonly uint Reserved5;
        public readonly ulong PartitionSize;
        //uint64 Reserved6[6] = { 0 };

        public FIoStoreTocHeader(BinaryReader reader)
        {
            TocMagic = reader.ReadBytes(16);

            if (!TOC_MAGIC.SequenceEqual(TocMagic))
                throw new FileLoadException("Invalid utoc magic");

            Version = (EIoStoreTocVersion) reader.ReadByte();
            Reserved0 = reader.ReadByte();
            Reserved1 = reader.ReadUInt16();
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
            ContainerFlags = (EIoContainerFlags) reader.ReadByte();
            Reserved3 = reader.ReadByte();
            Reserved4 = reader.ReadUInt16();
            Reserved5 = reader.ReadUInt32();
            PartitionSize = reader.ReadUInt64();
        }
    }
}