using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FModel
{
    static class TempStringFinder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private static string StringFinder(string filepath)
        {
            var dict = new Dictionary<string, string>();

            using (BinaryReader reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.GetEncoding(1252)))
            {
                byte[] MagicNumber = reader.ReadBytes(16);

                byte VersionNumber = reader.ReadByte();

                long LocalizedStringArrayOffset = -1;
                LocalizedStringArrayOffset = reader.ReadInt64();
                if (LocalizedStringArrayOffset != -1)
                {
                    long CurrentFileOffset = reader.BaseStream.Position;

                    reader.BaseStream.Seek(LocalizedStringArrayOffset, SeekOrigin.Begin);
                    int arrayLength = reader.ReadInt32();
                    Console.WriteLine("arrayLength: " + arrayLength);

                    reader.BaseStream.Seek(LocalizedStringArrayOffset, SeekOrigin.Begin);

                    string[] LocalizedStringArray = new string[arrayLength];
                    for (int i = 0; i < LocalizedStringArray.Length; i++)
                    {
                        if (!dict.ContainsKey("key " + i))
                        {
                            dict.Add("key " + i, readCleanString(reader));
                        }
                    }

                    reader.BaseStream.Seek(CurrentFileOffset, SeekOrigin.Begin);
                }

                /*uint NamespaceCount = reader.ReadUInt32();
                Console.WriteLine("NamespaceCount: " + NamespaceCount);

                uint KeyCount = reader.ReadUInt32();
                Console.WriteLine("KeyCount: " + KeyCount);*/
            }

            return JsonConvert.SerializeObject(dict, Formatting.Indented);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="allMyStrings"></param>
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

        public static string locresOpenFile(string defaultPath)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Multiselect = false;
            theDialog.InitialDirectory = defaultPath;
            theDialog.Title = @"Choose your LocRes file";
            theDialog.Filter = @"All Files (*.*)|*.*";

            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                return StringFinder(theDialog.FileName);
            }
            else { return null; }
        }
    }
}
