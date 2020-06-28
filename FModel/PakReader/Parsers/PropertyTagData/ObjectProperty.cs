using PakReader.Parsers.Objects;
using System.Collections.Generic;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class ObjectProperty : BaseProperty<FPackageIndex>
    {
        internal ObjectProperty(PackageReader reader, FPropertyTag tag)
        {
            Position = reader.Position;
            Value = new FPackageIndex(reader);
        }

        public object GetValue() => Value.GetValue();
    }
}
