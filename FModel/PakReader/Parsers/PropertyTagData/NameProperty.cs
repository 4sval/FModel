using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class NameProperty : BaseProperty<FName>
    {
        internal NameProperty(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadFName();
        }

        public string GetValue() => Value.String;
    }
}
