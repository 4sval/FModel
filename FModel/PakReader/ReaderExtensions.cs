using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace PakReader
{
    static class ReaderExtensions
    {
        public static string ReadFString(this BinaryReader reader)
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

            if (SaveNum == 0) return null;

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
                return Encoding.UTF8.GetString(reader.ReadBytes(SaveNum).AsSpan(..^1));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ReadTArray<T>(this BinaryReader reader, Func<T> Getter)
        {
            int SerializeNum = reader.ReadInt32();
            T[] A = new T[SerializeNum];
            for(int i = 0; i < SerializeNum; i++)
            {
                A[i] = Getter();
            }
            return A;
        }
    }
}
