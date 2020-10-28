using System;
using System.IO;

namespace FModel.PakReader.IO
{
    public readonly struct FNameEntryId : IEquatable<FNameEntryId>
    {
        public readonly uint Value;

        public FNameEntryId(uint value)
        {
            Value = value;
        }

        public FNameEntryId(BinaryReader reader)
        {
            Value = reader.ReadUInt32();
        }

        public bool Equals(FNameEntryId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is FNameEntryId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Value;
        }

        public static bool operator ==(FNameEntryId left, FNameEntryId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FNameEntryId left, FNameEntryId right)
        {
            return !left.Equals(right);
        }
    }
}