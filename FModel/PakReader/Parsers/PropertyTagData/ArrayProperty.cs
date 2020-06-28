using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class ArrayProperty : BaseProperty<object[]>
    {
        internal ArrayProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;

            int length = reader.ReadInt32();
            Value = new object[length];

            FPropertyTag InnerTag = default;
            // Execute if UE4 version is at least VER_UE4_INNER_ARRAY_TAG_INFO
            if (tag.InnerType.String == "StructProperty")
            {
                // Serialize a PropertyTag for the inner property of this array, allows us to validate the inner struct to see if it has changed
                InnerTag = new FPropertyTag(reader);
            }
            for (int i = 0; i < length; i++)
            {
                Value[i] = BaseProperty.ReadAsObject(reader, InnerTag, tag.InnerType, ReadType.ARRAY);
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
