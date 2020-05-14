using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class DelegateProperty : BaseProperty
    {
        public int Object;
        public FName Name;

        internal DelegateProperty(PackageReader reader, FPropertyTag tag)
        {
            Object = reader.ReadInt32();
            Name = reader.ReadFName();
        }
    }
}
