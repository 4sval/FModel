using System;
using System.IO;
using System.Text;

namespace PakReader
{
    static class Extensions
    {
        public static string ReadFString(this BinaryReader reader, int maxLength = -1)
        {
            // > 0 for ANSICHAR, < 0 for UCS2CHAR serialization
            var SaveNum = reader.ReadInt32();
            bool LoadUCS2Char = SaveNum < 0;
            if (LoadUCS2Char)
            {
                // If SaveNum cannot be negated due to integer overflow, Ar is corrupted.
                if (SaveNum == int.MinValue)
                {
                    throw new FileLoadException("Archive is corrupted");
                }

                SaveNum = -SaveNum;
            }

            if (SaveNum == 0) return string.Empty;

            // 1 byte is removed because of null terminator (\0)
            if (LoadUCS2Char)
            {
                ushort[] data = new ushort[SaveNum];
                for (int i = 0; i < SaveNum; i++)
                {
                    data[i] = reader.ReadUInt16();
                }
                unsafe
                {
                    fixed (ushort* dataPtr = &data[0])
                        return new string((char*)dataPtr, 0, data.Length - 1);
                }
            }
            else
            {
                byte[] bytes = reader.ReadBytes(SaveNum);
                if (bytes.Length == 0) return string.Empty;
                return Encoding.UTF8.GetString(bytes).Substring(0, SaveNum - 1);
            }
        }

        public static T[] ReadTArray<T>(this BinaryReader reader, Func<T> Getter)
        {
            int SerializeNum = reader.ReadInt32();
            T[] A = new T[SerializeNum];
            for (int i = 0; i < SerializeNum; i++)
            {
                A[i] = Getter();
            }
            return A;
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
