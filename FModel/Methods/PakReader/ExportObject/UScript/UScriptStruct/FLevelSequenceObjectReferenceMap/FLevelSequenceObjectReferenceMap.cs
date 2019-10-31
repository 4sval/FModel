using System.IO;

namespace PakReader
{
    internal struct FLevelSequenceObjectReferenceMap
    {
        public FLevelSequenceLegacyObjectReference[] map_data;

        internal FLevelSequenceObjectReferenceMap(BinaryReader reader)
        {
            int element_count = reader.ReadInt32();
            map_data = new FLevelSequenceLegacyObjectReference[element_count];
            for (int i = 0; i < element_count; i++)
            {
                map_data[i] = new FLevelSequenceLegacyObjectReference(reader);
            }
        }
    }
}
