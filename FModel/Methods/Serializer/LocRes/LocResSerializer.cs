using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FModel
{
    /*
     * Author: Asval
     * pretty sure it can be refactored
     * 
     * */
    static class LocResSerializer
    {
        private static byte[] LocResMagic = { 0x0E, 0x14, 0x74, 0x75, 0x67, 0x4A, 0x03, 0xFC, 0x4A, 0x15, 0x90, 0x9D, 0xC3, 0x37, 0x7F, 0x1B };
        private static long LocalizedStringArrayOffset { get; set; }
        private static string[] LocalizedStringArray { get; set; }
        private static string NamespacesString { get; set; }
        private static string myKey { get; set; }
        private static Dictionary<string, Dictionary<string, string>> LocResDict { get; set; }

        public static string StringFinder(string filepath)
        {
            LocResDict = new Dictionary<string, Dictionary<string, string>>();
            myKey = "LocResText";
            NamespacesString = "";
            LocalizedStringArrayOffset = -1;

            using (BinaryReader reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.GetEncoding(1252)))
            {
                byte[] MagicNumber = reader.ReadBytes(16);
                if (MagicNumber.SequenceEqual(LocResMagic))
                {
                    byte VersionNumber = reader.ReadByte();
                    if (VersionNumber == 2) //optimized
                    {
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
                                LocalizedStringArray[i] = AssetReader.readCleanString(reader);
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
                    else { throw new Exception("Unsupported LocRes version."); }
                }
                else { throw new Exception("Wrong LocResMagic number."); }
            }

            return JsonConvert.SerializeObject(LocResDict, Formatting.Indented);
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
            int stringIndex = br.ReadInt32();
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
                    myKey = AssetReader.readCleanString(br);

                    br.ReadInt32();
                    stringIndex = br.ReadInt32();

                    LocResDict[NamespacesString][myKey] = LocalizedStringArray[stringIndex];
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
    }
}
