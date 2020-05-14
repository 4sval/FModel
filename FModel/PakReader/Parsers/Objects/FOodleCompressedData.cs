using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FOodleCompressedData : IUStruct
    {
        /** The offset of the compressed data, within the archive */
        public readonly uint Offset;
        /** The compressed length of the data */
        public readonly uint CompressedLength;
        /** The decompressed length of the data */
        public readonly uint DecompressedLength;

        internal FOodleCompressedData(BinaryReader reader)
        {
            Offset = reader.ReadUInt32();
            CompressedLength = reader.ReadUInt32();
            DecompressedLength = reader.ReadUInt32();
        }
    }
}
