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
        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }
    
    public class FIoStoreTocHeader
    {
        public const int SIZE = 144;
        public static byte[] TOC_MAGIC = new byte[]
            {0x2D, 0x3D, 0x3D, 0x2D, 0x2D, 0x3D, 0x3D, 0x2D, 0x2D, 0x3D, 0x3D, 0x2D, 0x2D, 0x3D, 0x3D, 0x2D};

        public byte[] TocMagic;
        public EIoStoreTocVersion Version;
        public uint TocHeaderSize;
        public uint TocEntryCount;
        public uint TocCompressedBlockEntryCount;
        public uint TocCompressedBlockEntrySize;	// For sanity checking
        public uint CompressionMethodNameCount;
        public uint CompressionMethodNameLength;
        public uint CompressionBlockSize;
        public long DirectoryIndexSize;
        public FIoContainerId ContainerId;
        public FGuid EncryptionKeyGuid;
        public EIoContainerFlags ContainerFlags;

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
            DirectoryIndexSize = reader.ReadInt64();
            ContainerId = new FIoContainerId(reader);
            EncryptionKeyGuid = new FGuid(reader);
            ContainerFlags = (EIoContainerFlags) reader.ReadInt32();
            reader.BaseStream.Position += 60; // Padding
        }
    }
}