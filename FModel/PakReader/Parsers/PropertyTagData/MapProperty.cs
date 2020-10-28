using System;
using System.Collections.Generic;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class MapProperty : BaseProperty<IReadOnlyDictionary<object, object>>
    {
        internal MapProperty()
        {
            Value = new Dictionary<object, object>();
        }
        // https://github.com/EpicGames/UnrealEngine/blob/7d9919ac7bfd80b7483012eab342cb427d60e8c9/Engine/Source/Runtime/CoreUObject/Private/UObject/PropertyMap.cpp#L243
        internal MapProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            var NumKeysToRemove = reader.ReadInt32();
            if (NumKeysToRemove != 0)
            {
                // Let me know if you find a package that has a non-zero NumKeysToRemove value
                //throw new NotImplementedException("Parsing of non-zero NumKeysToRemove maps aren't supported yet.");

                for (var i = 0; i < NumKeysToRemove; i++)
                {
                    ReadAsValue(reader, tag, tag.InnerType, ReadType.MAP);
                }
            }

            var NumEntries = reader.ReadInt32();
            var dict = new Dictionary<object, object>(NumEntries);
            for (int i = 0; i < NumEntries; i++)
            {
                dict[ReadAsValue(reader, tag, tag.InnerType, ReadType.MAP)] = ReadAsObject(reader, tag, tag.ValueType, ReadType.MAP);
            }
            Value = dict;
        }

        public Dictionary<object, object> GetValue()
        {
            var ret = new Dictionary<object, object>(Value.Count);
            foreach (var (key, value) in Value)
            {
                ret[key] = value switch
                {
                    ByteProperty byteProperty => byteProperty.GetValue(),
                    BoolProperty boolProperty => boolProperty.GetValue(),
                    IntProperty intProperty => intProperty.GetValue(),
                    FloatProperty floatProperty => floatProperty.GetValue(),
                    ObjectProperty objectProperty => objectProperty.GetValue(),
                    NameProperty nameProperty => nameProperty.GetValue(),
                    DelegateProperty delegateProperty => delegateProperty.GetValue(),
                    DoubleProperty doubleProperty => doubleProperty.GetValue(),
                    ArrayProperty arrayProperty => arrayProperty.GetValue(),
                    StructProperty structProperty => structProperty.GetValue(),
                    StrProperty strProperty => strProperty.GetValue(),
                    TextProperty textProperty => textProperty.GetValue(),
                    InterfaceProperty interfaceProperty => interfaceProperty.GetValue(),
                    SoftObjectProperty softObjectProperty => softObjectProperty.GetValue(),
                    UInt64Property uInt64Property => uInt64Property.GetValue(),
                    UInt32Property uInt32Property => uInt32Property.GetValue(),
                    UInt16Property uInt16Property => uInt16Property.GetValue(),
                    Int64Property int64Property => int64Property.GetValue(),
                    Int16Property int16Property => int16Property.GetValue(),
                    Int8Property int8Property => int8Property.GetValue(),
                    MapProperty mapProperty => mapProperty.GetValue(),
                    SetProperty setProperty => setProperty.GetValue(),
                    EnumProperty enumProperty => enumProperty.GetValue(),
                    _ => value,
                };
            }
            return ret;
        }
    }
}
