using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FModel
{
    static class TempStringFinder
    {
        private static List<string> weirdStrings;
        private static bool previousByteWasValid = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private static string StringFinder(string filepath)
        {
            StringBuilder sb = new StringBuilder();

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

                /*reader.BaseStream.Position = 50;
                ByteArrayToString(reader, (int)LocalizedStringArrayOffset);*/

                weirdStrings = new List<string>();
                reader.BaseStream.Position = LocalizedStringArrayOffset;
                byte[] LocalizedStringArray = reader.ReadBytes((int)reader.BaseStream.Length - (int)LocalizedStringArrayOffset);

                string pattern = @"^[\w.:',+-=!?\""{}()\[\]|>«»#&% ]";
                Regex regex = new Regex(pattern);

                foreach (byte b in LocalizedStringArray) // Iterate throught all the bytes of the array
                {
                    string byteString = string.Empty;

                    if (b == 0x00) { continue; } //skip if empty byte
                    if (b == 0xA0) { byteString = " "; } //weird spaces
                    else
                    {
                        byteString = Encoding.GetEncoding(1252).GetString(new[] { b }); //generate string for single byte
                    }

                    bool valid = regex.IsMatch(byteString);

                    if (valid)
                    {
                        if (previousByteWasValid) weirdStrings[weirdStrings.Count - 1] += byteString;
                        else weirdStrings.Add(byteString);
                    }
                    previousByteWasValid = valid;
                }

                foreach (string str in weirdStrings)
                {
                    if (str.Length > 2)
                    {
                        if (str.Contains("ÿÿ")) { sb.Append(str.Substring(str.LastIndexOf("ÿ") + 1) + "\n"); }
                        else { sb.Append(str + "\n"); }
                    }
                }
            }

            return sb.ToString();
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
