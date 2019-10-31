using System;
using System.Collections.Generic;
using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct UScriptMap
    {
        public Dictionary<object, object> map_data;

        internal UScriptMap(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string key_type, string value_type)
        {
            int num_keys_to_remove = reader.ReadInt32();
            if (num_keys_to_remove != 0)
            {
                throw new NotSupportedException($"Could not read MapProperty with types: {key_type} {value_type}");
            }

            int num = reader.ReadInt32();
            map_data = new Dictionary<object, object>(num);
            for (int i = 0; i < num; i++)
            {
                map_data[read_map_value(reader, key_type, "StructProperty", name_map, import_map)] = read_map_value(reader, value_type, "StructProperty", name_map, import_map);
            }
        }
    }
}
