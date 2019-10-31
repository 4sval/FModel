using System.Collections.Generic;
using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct FStructFallback
    {
        public FPropertyTag[] properties;

        internal FStructFallback(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            var properties_ = new List<FPropertyTag>();
            int i = 0;
            while (true)
            {
                var tag = read_property_tag(reader, name_map, import_map, true);
                if (tag.Equals(default))
                {
                    break;
                }

                properties_.Add(tag);
                i++;
            }
            properties = properties_.ToArray();
        }
    }
}
