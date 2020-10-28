using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class TextProperty : BaseProperty<FText>
    {
        internal TextProperty()
        {
            Value = new FText(ETextFlag.Immutable, new FTextHistory.None());
        }
        internal TextProperty(PackageReader reader)
        {
            Position = reader.Position;
            Value = new FText(reader);
        }

        public object GetValue() => Value.GetValue();
    }
}
