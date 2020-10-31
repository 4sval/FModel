using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FModel.Logger;
using FModel.PakReader.IO;
using FModel.PakReader.Parsers.Objects;
using FModel.PakReader.Parsers.PropertyTagData;
using FModel.Utils;

namespace FModel.PakReader.Parsers.Class
{
    public class UObject : IUExport, IUStruct
    {
        private readonly Dictionary<string, object> Dict;

        public UObject(IoPackageReader reader, IReadOnlyDictionary<int, PropertyInfo> properties, bool structFallback = false, string type = null)
        {
            Dict = new Dictionary<string, object>();
            var header = new FUnversionedHeader(reader);
            using var it = new FIterator(header);

#if DEBUG

            var headerWritten = false;

            do
            {
                if (properties.ContainsKey(it.Current.Val))
                {
                    continue;
                }

                if (!headerWritten)
                {
                    headerWritten = true;
                    FConsole.AppendText(string.Concat("\n", type ?? "Unknown", ": ", reader.Summary.Name.String), "#CA6C6C", true);
                }

                FConsole.AppendText($"Val: {it.Current.Val} (IsNonZero: {it.Current.IsNonZero})", FColors.Yellow, true);
            }
            while (it.MoveNext());
            it.Reset();
#endif

            var num = 1;

            do
            {
                var (val, isNonZero) = it.Current;
                if (properties.TryGetValue(val, out var propertyInfo))
                {
                    if (isNonZero)
                    {
                        var obj = BaseProperty.ReadAsObject(reader, new FPropertyTag(propertyInfo), new FName(propertyInfo.Type), ReadType.NORMAL);
                        var key = Dict.ContainsKey(propertyInfo.Name) ? $"{propertyInfo.Name}_NK{num++:00}" : propertyInfo.Name;
                        Dict[key] = obj;
                    }
                    else
                    {
                        var obj = BaseProperty.ReadAsZeroObject(reader, new FPropertyTag(propertyInfo),
                            new FName(propertyInfo.Type));
                        var key = Dict.ContainsKey(propertyInfo.Name) ? $"{propertyInfo.Name}_NK{num++:00}" : propertyInfo.Name;
                        Dict[key] = obj;
                    }
                }
                else
                {
                    Dict[val.ToString()] = null;
                    if (!isNonZero)
                    {
                        // We are lucky: We don't know this property but it also has no content
                        DebugHelper.WriteLine($"Unknown property for {GetType().Name} with value {val} but it's zero so we are good");
                    }
                    else
                    {
                        DebugHelper.WriteLine($"Unknown property for {GetType().Name} with value {val}. Can't proceed serialization (Serialized {Dict.Count} properties till now)");
                        return;
                        //throw new FileLoadException($"Unknown property for {GetType().Name} with value {val}. Can't proceed serialization");
                    }
                }
            } while (it.MoveNext());
            if (!structFallback && reader.ReadInt32() != 0/* && reader.Position + 16 <= maxSize*/)
            {
                new FGuid(reader);
            }
        }

        // Empty UObject used for new package format when a property is zero
        public UObject()
        {
            Dict = new Dictionary<string, object>();
        }

        // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L930
        public UObject(PackageReader reader) : this(reader, false) { }

        // Structs that don't use binary serialization
        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L2197
        internal UObject(PackageReader reader, bool structFallback)
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

            if (!structFallback && reader.ReadInt32() != 0/* && reader.Position + 16 <= maxSize*/)
            {
                new FGuid(reader);
            }
        }

        public object this[string key] => Dict[key];
        public IEnumerable<string> Keys => Dict.Keys;
        public IEnumerable<object> Values => Dict.Values;
        public int Count => Dict.Count;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(string key) => Dict.ContainsKey(key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Dict.GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => Dict.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(string key, out object value) => Dict.TryGetValue(key, out value);
    }
}
