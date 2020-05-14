using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FPakCompressedBlock
    {
        public readonly long CompressedStart;
        public readonly long CompressedEnd;

        internal FPakCompressedBlock(BinaryReader reader)
        {
            CompressedStart = reader.ReadInt64();
            CompressedEnd = reader.ReadInt64();
        }

        internal FPakCompressedBlock(long start, long end)
        {
            CompressedStart = start;
            CompressedEnd = end;
        }
    }
}
