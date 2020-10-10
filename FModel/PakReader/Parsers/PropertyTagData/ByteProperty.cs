using PakReader.Parsers.Objects;
using System;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class ByteProperty : BaseProperty<object>
    {
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
