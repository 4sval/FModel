using System.IO;

namespace PakReader.Pak.IO
{
    public readonly struct FIoOffsetAndLength
    {
        // We use 5 bytes for offset and size, this is enough to represent
        // an offset and size of 1PB
        public readonly byte[] OffsetAndLength;
        public long Offset => OffsetAndLength[4]
                              | ((long) OffsetAndLength[3] << 8)
                              | ((long) OffsetAndLength[2] << 16)
                              | ((long) OffsetAndLength[1] << 24)
                              | ((long) OffsetAndLength[0] << 32);
        public long Length => OffsetAndLength[9]
                              | ((long) OffsetAndLength[8] << 8)
                              | ((long) OffsetAndLength[7] << 16)
                              | ((long) OffsetAndLength[6] << 24)
                              | ((long) OffsetAndLength[5] << 32);

        public FIoOffsetAndLength(BinaryReader reader)
        {
            OffsetAndLength = reader.ReadBytes(5 + 5);
        }
    }
}