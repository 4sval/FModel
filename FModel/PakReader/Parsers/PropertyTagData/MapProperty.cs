using System;
using System.Collections.Generic;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class MapProperty : BaseProperty<IReadOnlyDictionary<object, object>>
    {
        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/CoreUObject/Private/UObject/PropertyMap.cpp#L243
        internal MapProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            var NumKeysToRemove = reader.ReadInt32();
            if (NumKeysToRemove != 0)
            {
                // Let me know if you find a package that has a non-zero NumKeysToRemove value
                throw new NotImplementedException("Parsing of non-zero NumKeysToRemove maps aren't supported yet.");
            }

            var NumEntries = reader.ReadInt32();
            var dict = new Dictionary<object, object>(NumEntries);
            for (int i = 0; i < NumEntries; i++)
            {
                dict[ReadAsValue(reader, tag, tag.InnerType, ReadType.MAP)] = BaseProperty.ReadAsObject(reader, tag, tag.ValueType, ReadType.MAP);
            }
            Value = dict;
        }

        public Dictionary<object, object> GetValue()
        {
            var ret = new Dictionary<object, object>(Value.Count);
            foreach (KeyValuePair<object, object> KvP in Value)
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
    }
}
