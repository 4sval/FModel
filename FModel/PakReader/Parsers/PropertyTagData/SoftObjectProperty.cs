using System.Collections.Generic;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class SoftObjectProperty : BaseProperty<FSoftObjectPath>
    {
        internal SoftObjectProperty()
        {
            Value = new FSoftObjectPath();
        }
        internal SoftObjectProperty(PackageReader reader, ReadType readType)
        {
            Position = reader.Position;
            Value = new FSoftObjectPath(reader);
            if (!(reader is IoPackageReader) && readType == ReadType.MAP)
                reader.Position += 16 - (reader.Position - Position); // skip ahead, putting the total bytes read to 16
        }

        public Dictionary<string, string> GetValue() => Value.GetValue();
    }
}
