using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FPakCompressedBlock
    {
        public readonly long CompressedStart;
        public readonly long CompressedEnd;
        public readonly long Size;

        internal FPakCompressedBlock(BinaryReader reader)
        {
            CompressedStart = reader.ReadInt64();
            CompressedEnd = reader.ReadInt64();
            Size = CompressedEnd - CompressedStart;
        }

        internal FPakCompressedBlock(long start, long end)
        {
            CompressedStart = start;
            CompressedEnd = end;
            Size = end - start;
        }
    }
}
