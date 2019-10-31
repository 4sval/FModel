using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    internal struct FNameEntrySerialized
    {
        public string data;
        public ushort non_case_preserving_hash;
        public ushort case_preserving_hash;

        internal FNameEntrySerialized(BinaryReader reader)
        {
            data = read_string(reader);
            non_case_preserving_hash = reader.ReadUInt16();
            case_preserving_hash = reader.ReadUInt16();
        }
    }
}
