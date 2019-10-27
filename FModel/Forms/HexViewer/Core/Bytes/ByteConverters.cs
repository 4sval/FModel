//////////////////////////////////////////////
// Apache 2.0  - 2016-2019
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WpfHexaEditor.Core.Bytes
{
    /// <summary>
    /// ByteCharConverter for convert data
    /// </summary>
    public static class ByteConverters
    {
        /// <summary>
        /// Convert long to hex value
        /// </summary>
        public static string LongToHex(long val, OffSetPanelFixedWidth offsetwight = OffSetPanelFixedWidth.Dynamic) =>
            val.ToString(offsetwight == OffSetPanelFixedWidth.Dynamic
                ? ConstantReadOnly.HexStringFormat
                : ConstantReadOnly.HexLineInfoStringFormat
                    , CultureInfo.InvariantCulture);

        public static string LongToString(long val, int saveBits = -1)
        {
            if (saveBits == -1) return val.ToString();

            //Char[] with fixed size is always
            var chs = new char[saveBits];
            for (int i = 1; i <= saveBits; i++)
            {
                chs[saveBits - i] = (char)(val % 10 + 48);
                val /= 10;
            }
            return new string(chs);
        }

        /// <summary>
        /// Convert Byte to Char (can be used as visible text)
        /// </summary>
        /// <remarks>
        /// Code from : https://github.com/pleonex/tinke/blob/master/Be.Windows.Forms.HexBox/ByteCharConverters.cs
        /// </remarks>
        public static char ByteToChar(byte val) => val > 0x1F && !(val > 0x7E && val < 0xA0) ? (char)val : '.';

        /// <summary>
        /// Convert Char to Byte
        /// </summary>
        public static byte CharToByte(char val) => (byte)val;

        /// <summary>
        /// Converts a byte array to a hex string. For example: {10,11} = "0A 0B"
        /// </summary>
        public static string ByteToHex(byte[] data)
        {
            if (data == null) return string.Empty;

            var sb = new StringBuilder();

            foreach (var b in data)
            {
                var hex = ByteToHex(b);
                sb.Append(hex);
                sb.Append(" ");
            }

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        /// <summary>
        /// Convert a byte to char[2].
        /// </summary>
        public static char[] ByteToHexCharArray(byte val)
        {
            var hexbyteArray = new char[2];
            ByteToHexCharArray(val, hexbyteArray);
            return hexbyteArray;
        }

        /// <summary>
        /// Fill the <paramref name="charArr"/> with hex char;
        /// </summary>
        /// <param name="charArr">The length of this value should be 2.</param>
        public static void ByteToHexCharArray(byte val, char[] charArr)
        {
            if (charArr == null)
                throw new ArgumentNullException(nameof(charArr));

            if (charArr.Length != 2)
                throw new ArgumentException($"The length of {charArr} should be 2.");

            charArr[0] = ByteToHexChar(val >> 4);
            charArr[1] = ByteToHexChar(val - ((val >> 4) << 4));
        }

        /// <summary>
        /// Convert a byte to Hex char,i.e,10 = 'A'
        /// </summary>
        public static char ByteToHexChar(int val)
        {
            if (val < 10)
            {
                return (char)(48 + val);
            }
            else
            {
                switch (val)
                {
                    case 10:
                        return 'A';
                    case 11:
                        return 'B';
                    case 12:
                        return 'C';
                    case 13:
                        return 'D';
                    case 14:
                        return 'E';
                    case 15:
                        return 'F';
                    default:
                        return 's';
                }
            }
        }

        /// <summary>
        /// Converts the byte to a hex string. For example: "10" = "0A";
        /// </summary>
        public static string ByteToHex(byte val) => new string(ByteToHexCharArray(val));

        /// <summary>
        /// Convert byte to ASCII string
        /// </summary>
        public static string BytesToString(byte[] buffer, ByteToString converter = ByteToString.ByteToCharProcess)
        {
            if (buffer == null) return string.Empty;

            switch (converter)
            {
                case ByteToString.AsciiEncoding:
                    return Encoding.ASCII.GetString(buffer, 0, buffer.Length);

                case ByteToString.ByteToCharProcess:
                    var builder = new StringBuilder();

                    foreach (var @byte in buffer)
                        builder.Append(ByteToChar(@byte));

                    return builder.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts the hex string to an byte array. The hex string must be separated by a space char ' '. If there is any invalid hex information in the string the result will be null.
        /// </summary>
        public static byte[] HexToByte(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return null;

            hex = hex.Trim();
            var hexArray = hex.Split(' ');
            var byteArray = new byte[hexArray.Length];

            for (var i = 0; i < hexArray.Length; i++)
            {
                var hexValue = hexArray[i];
                var (isByte, val) = HexToUniqueByte(hexValue);

                if (!isByte) return null;

                byteArray[i] = val;
            }

            return byteArray;
        }

        /// <summary>
        /// Return Tuple (bool, byte) that bool represent if is a byte
        /// </summary>
        public static (bool success, byte val) HexToUniqueByte(string hex) =>
            (byte.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var val), val);

        /// <summary>
        /// Convert a hex string to long. 
        /// </summary>
        /// <return>
        /// Return (true, [position])
        /// Return (false, -1) on error
        /// </return>
        public static (bool success, long position) HexLiteralToLong(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return (false, -1);

            var i = hex.Length > 1 && hex[0] == '0' && (hex[1] == 'x' || hex[1] == 'X') 
                ? 2 
                : 0;
            
            long value = 0;

            while (i < hex.Length)
            {
                #region convert

                int x = hex[i++];

                if
                    (x >= '0' && x <= '9') x -= '0';
                else if
                    (x >= 'A' && x <= 'F') x -= 'A' + 10;
                else if
                    (x >= 'a' && x <= 'f') x -= 'a' + 10;
                else
                    return (false, -1);

                value = 16 * value + x;

                #endregion
            }

            return (true, value);
        }

        /// <summary>
        /// Check if is an hexa string
        /// </summary>
        public static (bool success, long value) IsHexValue(string hexastring) => HexLiteralToLong(hexastring);

        /// <summary>
        /// Check if is an hexa byte string
        /// </summary>
        public static (bool success, byte[] value) IsHexaByteStringValue(string hexastring) => 
            HexToByte(hexastring) == null 
                ? (false, null) 
                : (true, byteArray: HexToByte(hexastring));

        /// <summary>
        /// Convert string to byte array
        /// </summary>
        public static byte[] StringToByte(string str) => str.Select(CharToByte).ToArray();

        /// <summary>
        /// Convert String to hex string For example: "barn" = "62 61 72 6e"
        /// </summary>
        public static string StringToHex(string str) => ByteToHex(StringToByte(str));        
    }
}