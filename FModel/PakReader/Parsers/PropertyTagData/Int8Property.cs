using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class Int8Property : BaseProperty<byte>
    {
        internal Int8Property(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            Value = reader.ReadByte();
        }
    }
}
