using System;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class SetProperty : BaseProperty<object[]>
    {
        // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/CoreUObject/Private/UObject/PropertySet.cpp#L216
        internal SetProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            var NumKeysToRemove = reader.ReadInt32();
            if (NumKeysToRemove != 0)
            {
                // Let me know if you find a package that has a non-zero NumKeysToRemove value
                throw new NotImplementedException("Parsing of non-zero NumKeysToRemove sets aren't supported yet.");
            }

            var NumEntries = reader.ReadInt32();
            Value = new object[NumEntries];
            for (int i = 0; i < NumEntries; i++)
            {
                Value[i] = ReadAsObject(reader, tag, tag.InnerType, ReadType.ARRAY);
            }
        }

        public object[] GetValue()
        {
            var ret = new object[Value.Length];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = Value[i] switch
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
                    _ => Value[i],
                };
            }
            return ret;
        }
    }
}
