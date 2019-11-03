using System.IO;

namespace PakReader
{
    public class FDictionaryHeader
    {
        public uint Magic;
        public uint DictionaryVersion;
        public uint OodleMajorHeaderVersion;
        public int HashTableSize;
        public FOodleCompressedData DictionaryData;
        public FOodleCompressedData CompressorData;

        public FDictionaryHeader(BinaryReader reader)
        {
            Magic = reader.ReadUInt32();
            DictionaryVersion = reader.ReadUInt32();
            OodleMajorHeaderVersion = reader.ReadUInt32();
            HashTableSize = reader.ReadInt32();
            DictionaryData = new FOodleCompressedData(reader);
            CompressorData = new FOodleCompressedData(reader);
        }
    }
}
