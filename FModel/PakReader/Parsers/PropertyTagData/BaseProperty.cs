using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public class BaseProperty
    {
        internal static BaseProperty ReadAsObject(PackageReader reader, FPropertyTag tag, FName type, ReadType readType)
        {
            BaseProperty prop = type.String switch
            {
                "ByteProperty" => new ByteProperty(reader, readType),
                "BoolProperty" => new BoolProperty(reader, tag, readType),
                "IntProperty" => new IntProperty(reader),
                "FloatProperty" => new FloatProperty(reader),
                "ObjectProperty" => new ObjectProperty(reader),
                "NameProperty" => new NameProperty(reader),
                "DelegateProperty" => new DelegateProperty(reader),
                "DoubleProperty" => new DoubleProperty(reader),
                "ArrayProperty" => new ArrayProperty(reader, tag),
                "StructProperty" => new StructProperty(reader, tag),
                "StrProperty" => new StrProperty(reader),
                "TextProperty" => new TextProperty(reader),
                "InterfaceProperty" => new InterfaceProperty(reader),
                //"MulticastDelegateProperty" => new MulticastDelegateProperty(reader, tag),
                //"LazyObjectProperty" => new LazyObjectProperty(reader, tag),
                "SoftObjectProperty" => new SoftObjectProperty(reader, readType),
                "AssetObjectProperty" => new SoftObjectProperty(reader, readType),
                "UInt64Property" => new UInt64Property(reader),
                "UInt32Property" => new UInt32Property(reader),
                "UInt16Property" => new UInt16Property(reader),
                "Int64Property" => new Int64Property(reader),
                "Int16Property" => new Int16Property(reader),
                "Int8Property" => new Int8Property(reader),
                "MapProperty" => new MapProperty(reader, tag),
                "SetProperty" => new SetProperty(reader, tag),
                "EnumProperty" => new EnumProperty(reader),
                _ => null, //throw new NotImplementedException($"Parsing of {type.String} types aren't supported yet."),
            };
            return prop;
        }

        internal static object ReadAsValue(PackageReader reader, FPropertyTag tag, FName type, ReadType readType)
        {
            object prop = type.String switch
            {
                "ByteProperty" => new ByteProperty(reader, readType).Value,
                "BoolProperty" => new BoolProperty(reader, tag, readType).Value,
                "IntProperty" => new IntProperty(reader).Value,
                "FloatProperty" => new FloatProperty(reader).Value,
                "ObjectProperty" => new ObjectProperty(reader).Value,
                "NameProperty" => new NameProperty(reader).Value,
                "DelegateProperty" => new DelegateProperty(reader),
                "DoubleProperty" => new DoubleProperty(reader).Value,
                "ArrayProperty" => new ArrayProperty(reader, tag).Value,
                "StructProperty" => new StructProperty(reader, tag).Value,
                "StrProperty" => new StrProperty(reader).Value,
                "TextProperty" => new TextProperty(reader).Value,
                "InterfaceProperty" => new InterfaceProperty(reader).Value,
                //"MulticastDelegateProperty" => new MulticastDelegateProperty(reader, tag).Value,
                //"LazyObjectProperty" => new LazyObjectProperty(reader, tag).Value,
                "SoftObjectProperty" => new SoftObjectProperty(reader, readType).Value,
                "AssetObjectProperty" => new SoftObjectProperty(reader, readType).Value,
                "UInt64Property" => new UInt64Property(reader).Value,
                "UInt32Property" => new UInt32Property(reader).Value,
                "UInt16Property" => new UInt16Property(reader).Value,
                "Int64Property" => new Int64Property(reader).Value,
                "Int16Property" => new Int16Property(reader).Value,
                "Int8Property" => new Int8Property(reader).Value,
                "MapProperty" => new MapProperty(reader, tag).Value,
                "SetProperty" => new SetProperty(reader, tag).Value,
                "EnumProperty" => new EnumProperty(reader).Value,
                _ => null, //throw new NotImplementedException($"Parsing of {type.String} types aren't supported yet."),
            };
            return prop;
        }
    }

    public class BaseProperty<T> : BaseProperty
    {
        public long Position { get; protected set; }
        public T Value { get; protected set; }
    }

    public enum ReadType
    {
        NORMAL,
        MAP,
        ARRAY
    }
}
