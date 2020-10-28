namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class FloatProperty : BaseProperty<float>
    {
        internal FloatProperty()
        {
            Value = 0f;
        }
        internal FloatProperty(PackageReader reader)
        {
            Position = reader.Position;
            Value = reader.ReadFloat();
        }

        public float GetValue() => Value;
    }
}
