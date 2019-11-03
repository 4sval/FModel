using System.IO;

namespace PakReader
{
    public class FOodleCompressedData
    {
        public uint Offset;
        public uint CompressedLength;
        public uint DecompressedLength;

        public FOodleCompressedData(BinaryReader reader)
        {
            Offset = reader.ReadUInt32();
            CompressedLength = reader.ReadUInt32();
            DecompressedLength = reader.ReadUInt32();
        }
    }
}
