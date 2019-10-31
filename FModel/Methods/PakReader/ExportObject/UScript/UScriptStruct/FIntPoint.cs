using System.IO;

namespace PakReader
{
    public struct FIntPoint
    {
        public uint x;
        public uint y;

        internal FIntPoint(BinaryReader reader)
        {
            x = reader.ReadUInt32();
            y = reader.ReadUInt32();
        }
    }
}
