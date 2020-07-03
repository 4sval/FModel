namespace PakReader.Parsers.PropertyTagData
{
    public sealed class Int8Property : BaseProperty<byte>
    {
        internal Int8Property(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadByte();
        }

        public byte GetValue() => Value;
    }
}
