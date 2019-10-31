using System.IO;

namespace PakReader
{
    public struct FIndexLengthPair
    {
        public uint word1;
        public uint word2;

        public FIndexLengthPair(BinaryReader reader)
        {
            word1 = reader.ReadUInt32();
            word2 = reader.ReadUInt32();
        }
    }
}
