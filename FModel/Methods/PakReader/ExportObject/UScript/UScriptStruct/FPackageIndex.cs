using Newtonsoft.Json;
using System.IO;

namespace PakReader
{
    public class FPackageIndex
    {
        [JsonIgnore]
        public int index;
        public string import;
        public string outer_import;

        internal FPackageIndex(BinaryReader reader, FObjectImport[] import_map)
        {
            index = reader.ReadInt32();
            if (index < 0) index *= -1;
            index -= 1;
            if (index < 0 || index >= import_map.Length)
            {
                import = index.ToString();
                outer_import = default;
            }
            else
            {
                var imp = import_map[index];
                import = imp.object_name;
                outer_import = imp.outer_index?.import;
            }
        }
    }
}
