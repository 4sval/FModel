using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.Class
{
    public sealed class UStringTable : IUExport
    {
        readonly Dictionary<string, object> Map;

        internal UStringTable(PackageReader reader)
        {
            var _ = new UObject(reader); // will break

            Map = new Dictionary<string, object>(2)
            {
                { "StringTable", new FStringTable(reader) },
                { "StringTableId", reader.ReadFName() }
            };
        }

        public object this[string key] => Map[key];
        public IEnumerable<string> Keys => Map.Keys;
        public IEnumerable<object> Values => Map.Values;
        public int Count => Map.Count;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key) => Map.ContainsKey(key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Map.GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => Map.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out object value) => Map.TryGetValue(key, out value);
    }
}
