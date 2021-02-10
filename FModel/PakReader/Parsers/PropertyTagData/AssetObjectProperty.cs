namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class AssetObjectProperty : BaseProperty<string>
    {
        internal AssetObjectProperty()
        {
            Value = string.Empty;
        }
        internal AssetObjectProperty(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadFString();
        }

        public string GetValue() => Value;
    }
}
