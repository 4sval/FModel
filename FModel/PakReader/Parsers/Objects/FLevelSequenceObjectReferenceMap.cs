using System.Collections.Generic;

namespace PakReader.Parsers.Objects
{
    public readonly struct FLevelSequenceObjectReferenceMap : IUStruct
    {
        public readonly Dictionary<FGuid, FLevelSequenceLegacyObjectReference> Map;

        internal FLevelSequenceObjectReferenceMap(PackageReader reader)
        {
            Map = new Dictionary<FGuid, FLevelSequenceLegacyObjectReference>(reader.ReadInt32());
            for (int i = 0; i < Map.Count; i++)
            {
                Map[new FGuid(reader)] = new FLevelSequenceLegacyObjectReference(reader);
            }
        }
    }
}
