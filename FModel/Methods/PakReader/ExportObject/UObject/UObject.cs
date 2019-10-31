using System.Collections.Generic;
using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public class UObject : ExportObject
    {
        public string export_type;
        public FPropertyTag[] properties;

        internal UObject(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string export_type, bool read_guid)
        {
            this.export_type = export_type;
            var properties_ = new List<FPropertyTag>();
            while (true)
            {
                var tag = read_property_tag(reader, name_map, import_map, export_type != "FontFace");
                if (tag.Equals(default))
                {
                    break;
                }
                properties_.Add(tag);
            }

            if (read_guid && reader.ReadUInt32() != 0)
            {
                if (reader.BaseStream.Position + 16 <= reader.BaseStream.Length)
                    new FGuid(reader);
            }

            properties = properties_.ToArray();
        }
    }
}
