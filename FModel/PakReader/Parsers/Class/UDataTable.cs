using System;
using System.Collections;
using System.Collections.Generic;

namespace PakReader.Parsers.Class
{
    public sealed class UDataTable : IUExport
    {
        /** Map of name of row to row data structure. */
        readonly Dictionary<string, object> RowMap;

        internal UDataTable(PackageReader reader)
        {
            _ = new UObject(reader);
            
            int NumRows = reader.ReadInt32();
            RowMap = new Dictionary<string, object>();
            for (int i = 0; i < NumRows; i++)
            {
                int num = 1;
                string RowName = reader.ReadFName().String;
                string baseName = RowName;
                while (RowMap.ContainsKey(RowName))
                {
                    RowName = $"{baseName}_NK{num++:00}";
                }

                RowMap[RowName] = new UObject(reader, true);
            }
        }

        public object this[string key] => RowMap[key];
        public IEnumerable<string> Keys => RowMap.Keys;
        public IEnumerable<object> Values => RowMap.Values;
        public int Count => RowMap.Count;
        public bool ContainsKey(string key) => RowMap.ContainsKey(key);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => RowMap.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => RowMap.GetEnumerator();

        public bool TryGetValue(string key, out object value) => RowMap.TryGetValue(key, out value);
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
