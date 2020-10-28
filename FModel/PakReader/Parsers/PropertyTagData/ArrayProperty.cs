using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class ArrayProperty : BaseProperty<BaseProperty[]>
    {
        internal ArrayProperty()
        {
            Value = new BaseProperty[0];
        }
        internal ArrayProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;

            int length = reader.ReadInt32();
            Value = new BaseProperty[length];

            FPropertyTag InnerTag = default;
            // Execute if UE4 version is at least VER_UE4_INNER_ARRAY_TAG_INFO
            if (tag.InnerType.String == "StructProperty")
            {
                // Serialize a PropertyTag for the inner property of this array, allows us to validate the inner struct to see if it has changed
                InnerTag = reader is IoPackageReader ? tag : new FPropertyTag(reader);
            }
            for (int i = 0; i < length; i++)
            {
                Value[i] = ReadAsObject(reader, InnerTag, tag.InnerType, ReadType.ARRAY);
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
