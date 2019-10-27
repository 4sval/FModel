using System;
using System.IO;
using System.Text;

namespace PakReader
{
    static class Extensions
    {
        public static string ReadString(this BinaryReader reader, int maxLength = -1)
        {
            int length = reader.ReadInt32();
            if (length > 65536 || length < -65536)
            {
                throw new IOException($"String length too large ({length}), likely a read error.");
            }
            if (maxLength != -1 && Math.Abs(length) > maxLength)
            {
                throw new ArgumentOutOfRangeException("String exceeded max length");
            }
            if (length < 0)
            {
                length *= -1;
                ushort[] data = new ushort[length];
                for (int i = 0; i < length; i++)
                {
                    data[i] = reader.ReadUInt16();
                }
                unsafe
                {
                    fixed (ushort* dataPtr = &data[0])
                        return new string((char*)dataPtr, 0, data.Length);
                }
            }
            else
            {
                byte[] bytes = reader.ReadBytes(length);
                if (bytes.Length == 0) return string.Empty;
                return Encoding.UTF8.GetString(bytes).Substring(0, length - 1);
            }
        }

        public static T[] ReadTArray<T>(this BinaryReader reader, Func<T> getter)
        {
            int length = reader.ReadInt32();
            T[] container = new T[length];
            for (int i = 0; i < length; i++)
            {
                container[i] = getter();
            }
            return container;
        }

        public static byte[] SubArray(this byte[] inp, int offset, int length)
        {
            var ret = new byte[length];
            Buffer.BlockCopy(inp, offset, ret, 0, length);
            return ret;
        }

        public static float HalfToFloat(ushort h)
        {
            int sign = (h >> 15) & 0x00000001;
            int exp = (h >> 10) & 0x0000001F;
            int mant = h & 0x000003FF;

            exp = exp + (127 - 15);
            uint df = (uint)(sign << 31) | (uint)(exp << 23) | (uint)(mant << 13);
            return BitConverter.ToSingle(BitConverter.GetBytes(df), 0);
        }

        public static void StrCpy(byte[] dst, string name, int offset = 0)
        {
            byte[] src = Encoding.ASCII.GetBytes(name);
            Buffer.BlockCopy(src, 0, dst, offset, src.Length);
        }
    }
}
