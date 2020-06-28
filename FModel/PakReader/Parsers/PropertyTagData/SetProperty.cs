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
                Value[i] = BaseProperty.ReadAsObject(reader, tag, tag.InnerType, ReadType.ARRAY);
            }
        }

        public object[] GetValue()
        {
            var ret = new object[Value.Length];
            for (int i = 0; i < ret.Length; i++)
            {
                if (Value[i] == null)
                    ret[i] = null;
                else
                    ret[i] = ((BaseProperty)Value[i]).GetType().Name switch
                    {
                        "ByteProperty" => ((ByteProperty)Value[i]).GetValue(),
                        "BoolProperty" => ((BoolProperty)Value[i]).GetValue(),
                        "IntProperty" => ((IntProperty)Value[i]).GetValue(),
                        "FloatProperty" => ((FloatProperty)Value[i]).GetValue(),
                        "ObjectProperty" => ((ObjectProperty)Value[i]).GetValue(),
                        "NameProperty" => ((NameProperty)Value[i]).GetValue(),
                        "DoubleProperty" => ((DoubleProperty)Value[i]).GetValue(),
                        "ArrayProperty" => ((ArrayProperty)Value[i]).GetValue(),
                        "StructProperty" => ((StructProperty)Value[i]).GetValue(),
                        "StrProperty" => ((StrProperty)Value[i]).GetValue(),
                        "TextProperty" => ((TextProperty)Value[i]).GetValue(),
                        "InterfaceProperty" => ((InterfaceProperty)Value[i]).GetValue(),
                        "SoftObjectProperty" => ((SoftObjectProperty)Value[i]).GetValue(),
                        "UInt64Property" => ((UInt64Property)Value[i]).GetValue(),
                        "UInt32Property" => ((UInt32Property)Value[i]).GetValue(),
                        "UInt16Property" => ((UInt16Property)Value[i]).GetValue(),
                        "Int64Property" => ((Int64Property)Value[i]).GetValue(),
                        "Int16Property" => ((Int16Property)Value[i]).GetValue(),
                        "Int8Property" => ((Int8Property)Value[i]).GetValue(),
                        "MapProperty" => ((MapProperty)Value[i]).GetValue(),
                        "SetProperty" => ((SetProperty)Value[i]).GetValue(),
                        "EnumProperty" => ((EnumProperty)Value[i]).GetValue(),
                        _ => Value[i],
                    };
            }
            return ret;
        }
    }
}
