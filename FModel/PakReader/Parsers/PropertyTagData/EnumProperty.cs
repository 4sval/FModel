using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class EnumProperty : BaseProperty<FName>
    {
        internal EnumProperty(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadFName();
        }

        public string GetValue() => Value.String;
    }
}
