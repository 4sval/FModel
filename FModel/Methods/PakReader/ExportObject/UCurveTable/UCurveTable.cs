using System;
using System.IO;
using static PakReader.AssetReader;

namespace PakReader
{
    public sealed class UCurveTable : ExportObject
    {
        public UObject super_object;
        public ECurveTableMode curve_table_mode;
        public (string Name, UObject Object)[] row_map;

        internal UCurveTable(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            super_object = new UObject(reader, name_map, import_map, "CurveTable", true);

            row_map = new (string Name, UObject Object)[reader.ReadInt32()];

            curve_table_mode = (ECurveTableMode)reader.ReadByte();

            string row_type;
            switch (curve_table_mode)
            {
                case ECurveTableMode.Empty:
                    row_type = "Empty";
                    break;
                case ECurveTableMode.SimpleCurves:
                    row_type = "SimpleCurveKey";
                    break;
                case ECurveTableMode.RichCurves:
                    row_type = "RichCurveKey";
                    break;
                default:
                    throw new InvalidOperationException("Unsupported curve mode " + (byte)curve_table_mode);
            }
            for (int i = 0; i < row_map.Length; i++)
            {
                row_map[i] = (read_fname(reader, name_map), new UObject(reader, name_map, import_map, row_type, false));
            }
        }
    }
}
