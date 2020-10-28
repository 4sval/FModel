using System.IO;

namespace FModel.PakReader.IO
{
    public readonly struct FIoOffsetAndLength
    {
        // We use 5 bytes for offset and size, this is enough to represent
        // an offset and size of 1PB
        public readonly byte[] OffsetAndLength;
        public ulong Offset => OffsetAndLength[4]
                              | ((ulong) OffsetAndLength[3] << 8)
                              | ((ulong) OffsetAndLength[2] << 16)
                              | ((ulong) OffsetAndLength[1] << 24)
                              | ((ulong) OffsetAndLength[0] << 32);
        public ulong Length => OffsetAndLength[9]
                               | ((ulong) OffsetAndLength[8] << 8)
                               | ((ulong) OffsetAndLength[7] << 16)
                               | ((ulong) OffsetAndLength[6] << 24)
                               | ((ulong) OffsetAndLength[5] << 32);

        public FIoOffsetAndLength(BinaryReader reader)
        {
            OffsetAndLength = reader.ReadBytes(5 + 5);
        }
    }
}