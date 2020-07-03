using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class TextProperty : BaseProperty<FText>
    {
        internal TextProperty(PackageReader reader)
        {
            Position = reader.Position;
            Value = new FText(reader);
        }

        public object GetValue() => Value.GetValue();
    }
}
