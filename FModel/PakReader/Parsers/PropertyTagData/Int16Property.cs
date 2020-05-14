using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class Int16Property : BaseProperty<short>
    {
        internal Int16Property(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            Value = reader.ReadInt16();
        }
    }
}
