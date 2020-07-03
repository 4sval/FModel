using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class ObjectProperty : BaseProperty<FPackageIndex>
    {
        internal ObjectProperty(PackageReader reader)
        {
            Position = reader.Position;
            Value = new FPackageIndex(reader);
        }

        public object GetValue() => Value.GetValue();
    }
}
