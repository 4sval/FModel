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
            int i = 1;

            while (true)
            {
                var Tag = new FPropertyTag(reader);
                if (Tag.Name.IsNone)
                    break;

                var pos = reader.Position;
                var obj = BaseProperty.ReadAsObject(reader, Tag, Tag.Type, ReadType.NORMAL) ?? null;

                var key = properties.ContainsKey(Tag.Name.String) ? $"{Tag.Name.String}_NK{i++}" : Tag.Name.String;
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

        public Dictionary<string, object> GetValue()
        {
            var ret = new Dictionary<string, object>(Dict.Count);
            foreach (KeyValuePair<string, object> KvP in Dict)
            {
                if (KvP.Value == null)
                    ret[KvP.Key] = null;
                else
                    ret[KvP.Key] = KvP.Value.GetType().Name switch
                    {
                        "ByteProperty" => ((ByteProperty)KvP.Value).GetValue(),
                        "BoolProperty" => ((BoolProperty)KvP.Value).GetValue(),
                        "IntProperty" => ((IntProperty)KvP.Value).GetValue(),
                        "FloatProperty" => ((FloatProperty)KvP.Value).GetValue(),
                        "ObjectProperty" => ((ObjectProperty)KvP.Value).GetValue(),
                        "NameProperty" => ((NameProperty)KvP.Value).GetValue(),
                        "DoubleProperty" => ((DoubleProperty)KvP.Value).GetValue(),
                        "ArrayProperty" => ((ArrayProperty)KvP.Value).GetValue(),
                        "StructProperty" => ((StructProperty)KvP.Value).GetValue(),
                        "StrProperty" => ((StrProperty)KvP.Value).GetValue(),
                        "TextProperty" => ((TextProperty)KvP.Value).GetValue(),
                        "InterfaceProperty" => ((InterfaceProperty)KvP.Value).GetValue(),
                        "SoftObjectProperty" => ((SoftObjectProperty)KvP.Value).GetValue(),
                        "UInt64Property" => ((UInt64Property)KvP.Value).GetValue(),
                        "UInt32Property" => ((UInt32Property)KvP.Value).GetValue(),
                        "UInt16Property" => ((UInt16Property)KvP.Value).GetValue(),
                        "Int64Property" => ((Int64Property)KvP.Value).GetValue(),
                        "Int16Property" => ((Int16Property)KvP.Value).GetValue(),
                        "Int8Property" => ((Int8Property)KvP.Value).GetValue(),
                        "MapProperty" => ((MapProperty)KvP.Value).GetValue(),
                        "SetProperty" => ((SetProperty)KvP.Value).GetValue(),
                        "EnumProperty" => ((EnumProperty)KvP.Value).GetValue(),
                        _ => KvP.Value,
                    };
            }
            return ret;
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
