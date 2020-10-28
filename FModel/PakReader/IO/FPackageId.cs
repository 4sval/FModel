using System.IO;

namespace FModel.PakReader.IO
{
    public readonly struct FPackageId
    {
        public readonly ulong Id;

        public FPackageId(ulong id)
        {
            Id = id;
        }
        
        public FPackageId(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
        }
        
        public FIoChunkId CreateIoChunkId(ushort chunkIndex, EIoChunkType type = EIoChunkType.ExportBundleData) => new FIoChunkId(Id, chunkIndex, type);
    }
}