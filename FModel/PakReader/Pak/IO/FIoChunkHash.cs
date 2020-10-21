using System.IO;

namespace PakReader.Pak.IO
{
    public struct FIoChunkHash
    {
        public byte[] Hash;

        public FIoChunkHash(BinaryReader reader)
        {
            Hash = reader.ReadBytes(32);
        }
    }
}