using System;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class ByteProperty : BaseProperty<byte>
    {
        internal ByteProperty(PackageReader reader, ReadType readType)
        {
            Position = reader.Position;
            Value = readType switch
            {
                ReadType.NORMAL => (byte)reader.ReadFName().Index,
                ReadType.MAP => (byte)reader.ReadUInt32(),
                ReadType.ARRAY => reader.ReadByte(),
                _ => throw new ArgumentOutOfRangeException(nameof(readType)),
            };
        }

        public byte GetValue() => Value;
    }
}
