using System.IO;

namespace PakReader
{
    internal struct FDateTime
    {
        public long date;

        public FDateTime(BinaryReader reader)
        {
            date = reader.ReadInt64();
        }
    }
}
