using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class ArrayProperty : BaseProperty<BaseProperty[]>
    {
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
                InnerTag = new FPropertyTag(reader);
            }
            for (int i = 0; i < length; i++)
            {
                Value[i] = ReadProperty(reader, InnerTag, tag.InnerType, ReadType.ARRAY);
            }
        }
    }
}
