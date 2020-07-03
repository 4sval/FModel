namespace PakReader.Parsers.PropertyTagData
{
    public sealed class UInt64Property : BaseProperty<ulong>
    {
        internal UInt64Property(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadUInt64();
        }

        public ulong GetValue() => Value;
    }
}
