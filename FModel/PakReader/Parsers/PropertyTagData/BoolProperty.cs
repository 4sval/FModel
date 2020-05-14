using System;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class BoolProperty : BaseProperty<bool>
    {
        internal BoolProperty(PackageReader reader, FPropertyTag tag, ReadType readType)
        {
            switch (readType)
            {
                case ReadType.NORMAL:
                    Position = tag.Position;
                    Value = tag.BoolVal != 0;
                    break;
                case ReadType.MAP:
                case ReadType.ARRAY:
                    Position = reader.Position;
                    Value = reader.ReadByte() != 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(readType));
            }
        }
    }
}
