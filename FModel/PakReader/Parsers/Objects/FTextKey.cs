using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FTextKey
    {
        public readonly uint StrHash;
        public readonly string String;

        internal FTextKey(BinaryReader reader)
        {
            StrHash = reader.ReadUInt32();
            String = reader.ReadFString();
        }

        internal FTextKey(string str)
        {
            StrHash = 0;
            String = str;
        }
    }
}
