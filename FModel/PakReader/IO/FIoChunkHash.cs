using System.IO;

namespace FModel.PakReader.IO
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