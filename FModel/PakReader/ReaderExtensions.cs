using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace PakReader
{
    static class ReaderExtensions
    {
        public static unsafe string ReadFString(this BinaryReader reader)
        {
            var SaveNum = reader.ReadInt32();

            if (SaveNum == 0) return null;

            // > 0 for ANSICHAR, < 0 for UCS2CHAR serialization
            if (SaveNum < 0)
            {
                // If SaveNum cannot be negated due to integer overflow, Ar is corrupted.
                if (SaveNum == int.MinValue)
                {
                    throw new FileLoadException("Archive is corrupted");
                }

                SaveNum = -SaveNum;

                var dataBytes = reader.ReadBytes(SaveNum * sizeof(char));

                fixed (byte* pData = dataBytes)
                {
                    return new string((char*)pData, 0, SaveNum - 1); // 1 byte is removed because of null terminator (\0)
                }
            }
            else
            {
                var dataBytes = reader.ReadBytes(SaveNum);

                fixed (byte* pData = dataBytes)
                {
                    return new string((sbyte*)pData, 0, SaveNum - 1); // 1 byte is removed because of null terminator (\0)
                }
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
