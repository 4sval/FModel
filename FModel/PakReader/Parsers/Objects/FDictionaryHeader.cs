using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FDictionaryHeader : IUStruct
    {
        /** Unique value indicating this file type */
        public readonly uint Magic;
        /** Dictionary file format version */
        public readonly uint DictionaryVersion;
        /** Oodle header version - noting changes in Oodle data format (only the major-version reflects file format changes) */
        public readonly uint OodleMajorHeaderVersion;
        /** Size of the hash table used for the dictionary */
        public readonly int HashTableSize;
        /** Compressed dictionary data, within the archive */
        public readonly FOodleCompressedData DictionaryData;
        /** Compressed Oodle compressor state data, within the archive */
        public readonly FOodleCompressedData CompressorData;

        internal FDictionaryHeader(BinaryReader reader)
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
