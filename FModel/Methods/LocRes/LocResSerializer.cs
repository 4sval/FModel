using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FModel
{
    static class LocResSerializer
    {
        //TODO: refactor
        private static long LocalizedStringArrayOffset { get; set; }
        private static string[] LocalizedStringArray { get; set; }
        private static int stringIndex { get; set; }
        private static string NamespacesString { get; set; }
        private static string myKey = "LocResText";
        private static Dictionary<string, Dictionary<string, string>> LocResDict { get; set; }

        public static string StringFinder(string filepath)
        {
            LocResDict = new Dictionary<string, Dictionary<string, string>>();

            using (BinaryReader reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.GetEncoding(1252)))
            {
                byte[] MagicNumber = reader.ReadBytes(16);

                byte VersionNumber = reader.ReadByte();

                LocalizedStringArrayOffset = -1;
                LocalizedStringArrayOffset = reader.ReadInt64();
                if (LocalizedStringArrayOffset != -1)
                {
                    long CurrentFileOffset = reader.BaseStream.Position;

                    reader.BaseStream.Seek(LocalizedStringArrayOffset, SeekOrigin.Begin);
                    int arrayLength = reader.ReadInt32();

                    reader.BaseStream.Seek(LocalizedStringArrayOffset, SeekOrigin.Begin);

                    LocalizedStringArray = new string[arrayLength];
                    for (int i = 0; i < LocalizedStringArray.Length; i++)
                    {
                        LocalizedStringArray[i] = readCleanString(reader);
                    }

                    reader.BaseStream.Seek(CurrentFileOffset, SeekOrigin.Begin);

                    uint NamespaceCount = reader.ReadUInt32();
                    reader.ReadBytes(17);

                    for (uint i = 0; i < NamespaceCount; i++)
                    {
                        reader.ReadInt32();
                        readNamespaces(reader);
                    }
                }
            }

            return JsonConvert.SerializeObject(LocResDict, Formatting.Indented);
        }

        private static string readCleanString(BinaryReader reader)
        {
            reader.ReadInt32();
            int stringLength = 0;
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                stringLength = reader.ReadInt32();
            }

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

        private static void readNamespaces(BinaryReader br)
        {
            if (br.BaseStream.Position > LocalizedStringArrayOffset) { return; }

            int stringLength = br.ReadInt32();
            if (stringLength > 0)
            {
                NamespacesString = Encoding.GetEncoding(1252).GetString(br.ReadBytes(stringLength)).TrimEnd('\0');
            }
            else if (stringLength == 0)
            {
                NamespacesString = "";
            }
            else
            {
                byte[] data = br.ReadBytes((-1 - stringLength) * 2);
                br.ReadBytes(2);
                NamespacesString = Encoding.Unicode.GetString(data);
            }

            br.ReadInt32();
            stringIndex = br.ReadInt32();
            if (stringIndex > LocalizedStringArray.Length || stringIndex < 0)
            {
                if (!LocResDict.ContainsKey(NamespacesString))
                {
                    LocResDict[NamespacesString] = new Dictionary<string, string>();
                }

                long newOffset = br.BaseStream.Position - 8;
                br.BaseStream.Seek(newOffset, SeekOrigin.Begin);

                int KeyCount = br.ReadInt32();
                for (int i = 0; i < KeyCount; i++)
                {
                    readNpKeys(br);
                }
            }
            else
            {
                if (!LocResDict.ContainsKey(NamespacesString))
                {
                    LocResDict[NamespacesString] = new Dictionary<string, string>();
                    LocResDict[NamespacesString][myKey] = LocalizedStringArray[stringIndex];
                }
            }
        }

        private static void readNpKeys(BinaryReader reader)
        {
            reader.ReadInt32();
            int stringLength = reader.ReadInt32();

            if (stringLength < 0)
            {
                byte[] data = reader.ReadBytes((-1 - stringLength) * 2);
                reader.ReadBytes(2);
                myKey = Encoding.Unicode.GetString(data);
            }
            else if (stringLength == 0)
            {
                myKey = "";
            }
            else
            {
                myKey = Encoding.GetEncoding(1252).GetString(reader.ReadBytes(stringLength)).TrimEnd('\0');
            }

            reader.ReadInt32();
            stringIndex = reader.ReadInt32();

            LocResDict[NamespacesString][myKey] = LocalizedStringArray[stringIndex];
        }
    }
}
