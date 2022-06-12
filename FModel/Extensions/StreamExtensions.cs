using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace FModel.Extensions;

public enum Endianness
{
    LittleEndian,
    BigEndian
}

public static class StreamExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32(this Stream s, Endianness endian = Endianness.LittleEndian)
    {
        var b1 = s.ReadByte();
        var b2 = s.ReadByte();
        var b3 = s.ReadByte();
        var b4 = s.ReadByte();

        return endian switch
        {
            Endianness.LittleEndian => (uint) (b4 << 24 | b3 << 16 | b2 << 8 | b1),
            Endianness.BigEndian => (uint) (b1 << 24 | b2 << 16 | b3 << 8 | b4),
            _ => throw new Exception("unknown endianness")
        };
    }
}