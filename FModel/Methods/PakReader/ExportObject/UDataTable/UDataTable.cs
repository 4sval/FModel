using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public sealed class UDataTable : ExportObject
    {
        public UObject super_object;
        public (string Name, UObject Object)[] rows;

        internal UDataTable(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            super_object = new UObject(reader, name_map, import_map, "RowStruct", true);

            rows = new (string Name, UObject Object)[reader.ReadInt32()];

            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = (read_fname(reader, name_map), new UObject(reader, name_map, import_map, "RowStruct", false));
            }
        }
    }
}
