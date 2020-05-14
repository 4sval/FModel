using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class UInt64Property : BaseProperty<ulong>
    {
        internal UInt64Property(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            Value = reader.ReadUInt64();
        }
    }
}
