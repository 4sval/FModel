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
            StringBuilder sb = new StringBuilder();
            List<string> LocalizedStringArray = new List<string>();

            using (BinaryReader reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.GetEncoding(1252)))
            {
                byte[] MagicNumber = reader.ReadBytes(16);

                byte VersionNumber = reader.ReadByte();
                //Console.WriteLine("VersionNumber: " + VersionNumber);

                long LocalizedStringArrayOffset = -1;
                LocalizedStringArrayOffset = reader.ReadInt64();
                //Console.WriteLine("LocalizedStringArrayOffset: " + LocalizedStringArrayOffset);

                uint EntriesCount = reader.ReadUInt32();
                //Console.WriteLine("EntriesCount: " + EntriesCount);

                uint NamespaceCount = reader.ReadUInt32();
                //Console.WriteLine("NamespaceCount: " + NamespaceCount);

                reader.BaseStream.Position = LocalizedStringArrayOffset;

                readCleanString(reader, LocalizedStringArray);

                foreach (string s in LocalizedStringArray)
                {
                    sb.Append(s.Replace("\0", string.Empty) + "\n");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="allMyStrings"></param>
        private static void readCleanString(BinaryReader reader, List<string> allMyStrings)
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
                allMyStrings.Add(Encoding.Unicode.GetString(data));
            }
            else if (stringLength == 0)
            {
                return;
            }
            else
            {
                allMyStrings.Add(Encoding.GetEncoding(1252).GetString(reader.ReadBytes(stringLength)));
            }

            readCleanString(reader, allMyStrings);
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
