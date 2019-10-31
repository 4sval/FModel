using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    internal struct FLevelSequenceLegacyObjectReference
    {
        public FGuid key_guid;
        public FGuid object_id;
        public string object_path;

        internal FLevelSequenceLegacyObjectReference(BinaryReader reader)
        {
            key_guid = new FGuid(reader);
            object_id = new FGuid(reader);
            object_path = read_string(reader);
        }
    }
}
