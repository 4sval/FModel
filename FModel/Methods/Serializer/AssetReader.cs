using System;
using System.IO;
using System.Text;

namespace FModel
{
    class AssetReader
    {
        public static string readCleanString(BinaryReader reader)
        {
            reader.ReadInt32();
            int stringLength = reader.ReadInt32();

            if (stringLength < 0)
            {
                byte[] data = reader.ReadBytes((-1 - stringLength) * 2);
                reader.ReadBytes(2);
                return Encoding.Unicode.GetString(data);
            }
            else if (stringLength == 0)
            {
                return "";
            }
            else
            {
                return Encoding.GetEncoding(1252).GetString(reader.ReadBytes(stringLength)).TrimEnd('\0');
            }
        }
    }
}
