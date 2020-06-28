using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class SoftObjectProperty : BaseProperty<FSoftObjectPath>
    {
        internal SoftObjectProperty(PackageReader reader, FPropertyTag tag, ReadType readType)
        {
            Position = reader.Position;
            Value = new FSoftObjectPath(reader);
            if (readType == ReadType.MAP)
                reader.Position += 16 - (reader.Position - Position); // skip ahead, putting the total bytes read to 16
        }

        public FSoftObjectPath GetValue() => Value;
    }
}
