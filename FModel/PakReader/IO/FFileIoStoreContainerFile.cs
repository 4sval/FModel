using System.IO;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.IO
{
    public struct FFileIoStoreContainerFile
    {
        public Stream FileHandle;
        public long CompressionBlockSize;
        public string[] CompressionMethods;
        public FIoStoreTocCompressedBlockEntry[] CompressionBlocks;
        public FGuid EncryptionKeyGuid;
        public byte[] EncryptionKey;
        public EIoContainerFlags ContainerFlags;
        public FSHAHash[] BlockSignatureHashes;
        
        public long FileSize => FileHandle.Length;
    }
}