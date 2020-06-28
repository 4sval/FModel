using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class InterfaceProperty : BaseProperty<uint>
    {
        // Value is ObjectRef
        internal InterfaceProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            Value = reader.ReadUInt32();
        }

        public uint GetValue() => Value;
    }
}
