using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;

namespace PakReader.Parsers.Class
{
    public class UObject : IUExport, IUStruct
    {
        readonly Dictionary<string, object> Dict;

        // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L930
        public UObject(PackageReader reader) : this(reader, reader.ExportMap.Sum(e => e.SerialSize), false) { }
        public UObject(PackageReader reader, bool structFallback) : this(reader, reader.ExportMap.Sum(e => e.SerialSize), structFallback) { }
        public UObject(PackageReader reader, long maxSize) : this(reader, maxSize, false) { }

        // Structs that don't use binary serialization
        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L2197
        internal UObject(PackageReader reader, long maxSize, bool structFallback)
        {
            var properties = new Dictionary<string, object>();
            int num = 1;

            while (true)
            {
                var Tag = new FPropertyTag(reader);
                if (Tag.Name.IsNone || Tag.Name.String == null)
                    break;

                var pos = reader.Position;
                var obj = BaseProperty.ReadAsObject(reader, Tag, Tag.Type, ReadType.NORMAL) ?? null;

                var key = properties.ContainsKey(Tag.Name.String) ? $"{Tag.Name.String}_NK{num++:00}" : Tag.Name.String;
                properties[key] = obj;
                if (obj == null) break;

                if (Tag.Size + pos != reader.Position)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Didn't read {key} correctly (at {reader.Position}, should be {Tag.Size + pos}, {Tag.Size + pos - reader.Position} behind)");
#endif
                    reader.Position = Tag.Size + pos;
                }
            }
            Dict = properties;

            if (!structFallback && reader.ReadInt32() != 0 && reader.Position + 16 <= maxSize)
            {
                new FGuid(reader);
            }
        }

        public object this[string key] => Dict[key];
        public IEnumerable<string> Keys => Dict.Keys;
        public IEnumerable<object> Values => Dict.Values;
        public int Count => Dict.Count;
        public bool ContainsKey(string key) => Dict.ContainsKey(key);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Dict.GetEnumerator();

        public bool TryGetValue(out object value, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (Dict.TryGetValue(key, out var v))
                {
                    value = v;
                    return true;
                }
            }
            value = null;
            return false;
        }
        public bool TryGetValue(string key, out object value) => Dict.TryGetValue(key, out value);

        public T Deserialize<T>()
        {
            var ret = ReflectionHelper.NewInstance<T>();
            var map = ReflectionHelper.GetActionMap<T>();
            foreach (var kv in Dict)
            {
                (var baseType, var typeGetter) = ReflectionHelper.GetPropertyInfo(kv.Value.GetType());
                if (map.TryGetValue((kv.Key.ToLowerInvariant(), baseType), out Action<object, object> setter))
                {
                    setter(ret, typeGetter(kv.Value));
                }
            }
            return ret;
        }
    }
}
