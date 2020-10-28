using System;

using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class ByteProperty : BaseProperty<object>
    {
        internal ByteProperty()
        {
            Value = 0;
        }
        internal ByteProperty(PackageReader reader, FPropertyTag tag, ReadType readType)
        {
            Position = reader.Position;
            Value = readType switch
            {
                ReadType.NORMAL => tag.EnumName.IsNone ? reader.ReadByte().ToString() : reader.ReadFName().String,
                ReadType.MAP => (byte)reader.ReadUInt32(),
                ReadType.ARRAY => reader.ReadByte(),
                _ => throw new ArgumentOutOfRangeException(nameof(readType)),
            };
        }

        public object GetValue() => Value;
    }
}
