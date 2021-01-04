using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FModel.PakReader.IO;
using FModel.PakReader.Parsers.Objects;
using FModel.PakReader.Parsers.PropertyTagData;
using FModel.Utils;

namespace FModel.PakReader.Parsers.Class
{
    public class UObject : IUExport, IUStruct
    {
        private readonly Dictionary<string, object> Dict;

        public UObject(IoPackageReader reader, string type, bool structFallback = false)
        {
            Dict = new Dictionary<string, object>();
            var header = new FUnversionedHeader(reader);
            if (header.HasValues)
            {
                using var it = new FIterator(header);
                if (header.HasNonZeroValues)
                {
                    FUnversionedType unversionedType = reader.GetOrCreateSchema(type);
                    var num = 1;
                    do
                    {
                        var (val, isNonZero) = it.Current;
                        if (unversionedType.Properties.TryGetValue(val, out var props))
                        {
                            var propertyTag = new FPropertyTag(props);
                            if (isNonZero)
                            {
                                var key = Dict.ContainsKey(props.Name) ? $"{props.Name}_NK{num++:00}" : props.Name;
                                var obj = BaseProperty.ReadAsObject(reader, propertyTag, propertyTag.Type, ReadType.NORMAL);
                                Dict[key] = obj;
                            }
                            else
                            {
                                var key = Dict.ContainsKey(props.Name) ? $"{props.Name}_NK{num++:00}" : props.Name;
                                var obj = BaseProperty.ReadAsZeroObject(reader, propertyTag, propertyTag.Type);
                                Dict[key] = obj;
                            }
                        }
                        else Dict[val.ToString()] = null;
                    } while (it.MoveNext());
                }
                else
                {
#if DEBUG
                    FConsole.AppendText(string.Concat("\n", type ?? "Unknown", ": ", reader.Summary.Name.String), "#CA6C6C", true);
                    do
                    {
                        FConsole.AppendText($"Val: {it.Current.Val} (IsNonZero: {it.Current.IsNonZero})", FColors.Yellow, true);
                    }
                    while (it.MoveNext());
#endif
                }
            }

            if (!structFallback && reader.ReadInt32() != 0 /* && reader.Position + 16 <= maxSize*/)
                reader.Position += FGuid.SIZE;
        }

        // Empty UObject used for new package format when a property is zero
        public UObject()
        {
            Dict = new Dictionary<string, object>(0);
        }

        // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L930
        public UObject(PackageReader reader) : this(reader, false) { }

        // Structs that don't use binary serialization
        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/CoreUObject/Private/UObject/Class.cpp#L2197
        internal UObject(PackageReader reader, bool structFallback)
        {
            var properties = new Dictionary<string, object>();
            var num = 1;

            while (true)
            {
                var Tag = new FPropertyTag(reader);
                if (Tag.Name.IsNone || Tag.Name.String == null)
                    break;

                var pos = reader.Position;
                var obj = BaseProperty.ReadAsObject(reader, Tag, Tag.Type, ReadType.NORMAL);

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

            if (!structFallback && reader.ReadInt32() != 0 /* && reader.Position + 16 <= maxSize*/)
                reader.Position += FGuid.SIZE;
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
