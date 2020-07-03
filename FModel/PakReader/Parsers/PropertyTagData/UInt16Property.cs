namespace PakReader.Parsers.PropertyTagData
{
    public sealed class UInt16Property : BaseProperty<ushort>
    {
        internal UInt16Property(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadUInt16();
        }

        public ushort GetValue() => Value;
    }
}
