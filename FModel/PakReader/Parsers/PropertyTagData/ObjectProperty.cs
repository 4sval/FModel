using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class ObjectProperty : BaseProperty<FPackageIndex>
    {
        internal ObjectProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            Value = new FPackageIndex(reader);
        }
    }
}
