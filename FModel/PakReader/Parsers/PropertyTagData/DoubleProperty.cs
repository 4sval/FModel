namespace PakReader.Parsers.PropertyTagData
{
    public sealed class DoubleProperty : BaseProperty<double>
    {
        internal DoubleProperty(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadDouble();
        }

        public double GetValue() => Value;
    }
}
