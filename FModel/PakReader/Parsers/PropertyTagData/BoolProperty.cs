using System;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class BoolProperty : BaseProperty<bool>
    {
        internal BoolProperty(FPropertyTag tag)
        {
            Value = tag.BoolVal != 0;
        }
        internal BoolProperty(PackageReader reader, FPropertyTag tag, ReadType readType)
        {
            switch (readType)
            {
                case ReadType.NORMAL when !(reader is IoPackageReader):
                    Position = tag.Position;
                    Value = tag.BoolVal != 0;
                    break;
                case ReadType.NORMAL:
                case ReadType.MAP:
                case ReadType.ARRAY:
                    Position = reader.Position;
                    Value = reader.ReadByte() != 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(readType));
            }
        }

        public bool GetValue() => Value;
    }
}
