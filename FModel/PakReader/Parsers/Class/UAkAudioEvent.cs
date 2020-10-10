using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PakReader.Parsers.Class
{
    public sealed class UAkAudioEvent : IUExport
    {
        readonly Dictionary<string, object> Map;

        internal UAkAudioEvent(PackageReader reader)
        {
            _ = new UObject(reader, true);
            Map = new Dictionary<string, object>(1)
            {
                { "MaxAttenuationRadius", reader.ReadFloat() }
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
