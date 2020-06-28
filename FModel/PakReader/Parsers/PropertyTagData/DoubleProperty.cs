using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class DoubleProperty : BaseProperty<double>
    {
        internal DoubleProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            Value = reader.ReadDouble();
        }

        public double GetValue() => Value;
    }
}
