using System.IO;
using PakReader.Parsers.Objects;

namespace PakReader.Pak.IO
{
    public struct FFileIoStoreContainerFile
    {
        public Stream FileHandle;
        public string FileName;
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