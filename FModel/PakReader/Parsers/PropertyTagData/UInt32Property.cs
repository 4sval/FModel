using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class UInt32Property : BaseProperty<uint>
    {
        internal UInt32Property(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            Value = reader.ReadUInt32();
        }
    }
}
