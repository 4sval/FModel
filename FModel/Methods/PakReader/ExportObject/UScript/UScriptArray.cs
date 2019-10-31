using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public struct UScriptArray
    {
        public FPropertyTag tag;
        public object[] data;

        internal UScriptArray(BinaryReader reader, string inner_type, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            uint element_count = reader.ReadUInt32();
            tag = default;

            if (inner_type == "StructProperty" || inner_type == "ArrayProperty")
            {
                tag = read_property_tag(reader, name_map, import_map, false);
                if (tag.Equals(default))
                {
                    throw new IOException("Could not read file");
                }
            }
            object inner_tag_data = tag.Equals(default) ? null : tag.tag_data;

            data = new object[element_count];
            for (int i = 0; i < element_count; i++)
            {
                if (inner_type == "BoolProperty")
                {
                    data[i] = reader.ReadByte() != 0;
                }
                else if (inner_type == "ByteProperty")
                {
                    data[i] = reader.ReadByte();
                }
                else
                {
                    var tag = new_property_tag_type(reader, name_map, import_map, inner_type, inner_tag_data);
                    if ((int)tag.type != 100)
                    {
                        data[i] = tag.data;
                    }
                }
            }
        }
    }
}
