namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class IntProperty : BaseProperty<int>
    {
        internal IntProperty()
        {
            Value = 0;
        }
        internal IntProperty(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadInt32();
        }

        public int GetValue() => Value;
    }
}
