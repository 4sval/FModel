using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using FModel.PakReader.IO;
using FModel.PakReader.Parsers.PropertyTagData;
using FModel.Utils;

namespace FModel.PakReader.Parsers.Class
{
    public sealed class UDataTable : IUExport
    {
        /** Map of name of row to row data structure. */
        private readonly Dictionary<string, object> RowMap;

        internal UDataTable(PackageReader reader)
        {
            _ = new UObject(reader);
            
            int NumRows = reader.ReadInt32();
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

        internal UDataTable(IoPackageReader reader, IReadOnlyDictionary<int, PropertyInfo> properties, string type)
        {
            var baseObj = new UObject(reader, properties, type: type);

            if (!baseObj.TryGetValue("RowStruct", out var rowStructProp) || !(rowStructProp is ObjectProperty rowStruct) || !rowStruct.Value.IsImport)
            {
                return;
            }

            var rowStructimportIndex = rowStruct.Value.AsImport;

            if (rowStructimportIndex >= reader.ImportMap.Length)
            {
                return;
            }

            var rowStructimport = reader.ImportMap[rowStructimportIndex];

            if (rowStructimport.Type != EType.ScriptImport ||
                !Globals.GlobalData.ScriptObjectByGlobalId.TryGetValue(rowStructimport, out var rowStrucDesc) ||
                rowStrucDesc.Name.IsNone)
            {
                return;
            }

            if (!Globals.TypeMappings.TryGetValue(rowStrucDesc.Name.String, out var rowProperties))
            {
                FConsole.AppendText($"{reader.Summary.Name.String} can't be parsed yet (RowType: {rowStrucDesc.Name.String})", FColors.Red, true);
                return;
            }

            var NumRows = reader.ReadInt32();
            RowMap = new Dictionary<string, object>();

            for (var i = 0; i < NumRows; i++)
            {
                var num = 1;
                var RowName = reader.ReadFName().String ?? "";
                var baseName = RowName;

                while (RowMap.ContainsKey(RowName))
                {
                    RowName = $"{baseName}_NK{num++:00}";
                }

                RowMap[RowName] = new UObject(reader, rowProperties, true, rowStrucDesc.Name.String);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetCaseInsensitiveValue(string key, out object value)
        {
            foreach (var r in RowMap)
            {
                if (r.Key.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                {
                    value = r.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }
    }
}
