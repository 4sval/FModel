namespace PakReader.Parsers.PropertyTagData
{
    public sealed class StrProperty : BaseProperty<string>
    {
        internal StrProperty(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadFString();
        }

        public string GetValue() => Value;
    }
}
