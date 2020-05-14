using System;
using System.Linq;

namespace PakReader
{
    static class BinaryHelper
    {
        public static uint Flip(uint value) => (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
         (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;

        static readonly uint[] _Lookup32 = Enumerable.Range(0, 256).Select(i => {
            string s = i.ToString("X2");
            return s[0] + ((uint)s[1] << 16);
        }).ToArray();
        public static string ToHex(params byte[] bytes)
        {
            if (bytes == null) return null;
            var length = bytes.Length;
            var result = new char[length * 2];
            for (int i = 0; i < length; i++)
            {
                var val = _Lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }
        public static string ToStringKey(this byte[] byteKey)
        {
            return "0x" + BitConverter.ToString(byteKey).Replace("-", "");
        }
        public static byte[] ToBytesKey(this string stringKey)
        {
            byte[] arr = new byte[stringKey.Length >> 1];
            for (int i = 0; i < stringKey.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(stringKey[i << 1]) << 4) + (GetHexVal(stringKey[(i << 1) + 1])));
            }
            return arr;
        }
        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : 55);
        }
    }
}
