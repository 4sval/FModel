namespace PakReader.Parsers.PropertyTagData
{
    public sealed class Int16Property : BaseProperty<short>
    {
        internal Int16Property(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadInt16();
        }

        public short GetValue() => Value;
    }
}
