using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using FModel.PakReader.IO;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.Class
{
    public sealed class UCurveTable : IUExport
    {
        public ECurveTableMode CurveTableMode { get; }
        readonly Dictionary<string, object> RowMap;

        internal UCurveTable(PackageReader reader)
        {
            new UObject(reader); //will break

            int NumRows = reader.ReadInt32();
            CurveTableMode = (ECurveTableMode)reader.ReadByte();

            RowMap = new Dictionary<string, object>();
            for (int i = 0; i < NumRows; i++)
            {
                int num = 1;
                string RowName = reader.ReadFName().String ?? "";
                string baseName = RowName;
                while (RowMap.ContainsKey(RowName))
                {
                    RowName = $"{baseName}_NK{num++:00}";
                }

                RowMap[RowName] = new UObject(reader, true);
            }
        }

        internal UCurveTable(IoPackageReader reader)
        {
            reader.ReadUInt16(); // don't ask me
            reader.ReadUInt32(); // what this is

            int NumRows = reader.ReadInt32();
            CurveTableMode = (ECurveTableMode)reader.ReadByte();
            Dictionary<int, PropertyInfo> properties = CurveTableMode switch
            {
                ECurveTableMode.RichCurves => Globals.TypeMappings["RichCurve"],
                ECurveTableMode.SimpleCurves => Globals.TypeMappings["SimpleCurve"],
                _ => throw new FileLoadException($"This table has an unknown mode ({CurveTableMode})")
            };

            RowMap = new Dictionary<string, object>();
            for (int i = 0; i < NumRows; i++)
            {
                int num = 1;
                string RowName = reader.ReadFName().String ?? "";
                string baseName = RowName;
                while (RowMap.ContainsKey(RowName))
                {
                    RowName = $"{baseName}_NK{num++:00}";
                }

                RowMap[RowName] = new UObject(reader, properties, true);
            }
        }

        public object this[string key] => RowMap[key];
        public IEnumerable<string> Keys => RowMap.Keys;
        public IEnumerable<object> Values => RowMap.Values;
        public int Count => RowMap.Count;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key) => RowMap.ContainsKey(key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => RowMap.GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => RowMap.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out object value) => RowMap.TryGetValue(key, out value);
    }
}
