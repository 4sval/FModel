namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class Int64Property : BaseProperty<long>
    {
        internal Int64Property()
        {
            Value = 0;
        }
        internal Int64Property(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadInt64();
        }

        public long GetValue() => Value;
    }
}
