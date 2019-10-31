using Newtonsoft.Json;
using System.IO;

namespace PakReader
{
    public sealed class FontFace : ExportObject
    {
        public UObject base_object;
        [JsonIgnore]
        public uint data;

        internal FontFace(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            base_object = new UObject(reader, name_map, import_map, "FontFace", true);

            new FStripDataFlags(reader); // no idea
            new FStripDataFlags(reader); // why are there two

            data = reader.ReadUInt32();
        }
    }
}
