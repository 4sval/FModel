using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FModel
{
    static class AssetNameMap
    {
        public static List<string> myNamespacesList { get; set; }
        public static void searchStringsInUexp(string filepath)
        {
            myNamespacesList = new List<string>();

            if (File.Exists(filepath))
            {
                List<string> weirdStrings = new List<string>();
                List<string> listBeforeData = new List<string>();
                bool previousByteWasValid = false;

                byte[] bytes = File.ReadAllBytes(filepath);

                string pattern = @"^[a-zA-Z0-9()'\-.&!+, ]";
                Regex regex = new Regex(pattern);

                foreach (byte b in bytes) // Iterate throught all the bytes of the file
                {
                    string byteString = Encoding.UTF8.GetString(new[] { b }); // Generate string for single byte

                    bool valid = regex.IsMatch(byteString);

                    if (valid)
                    {
                        if (previousByteWasValid) weirdStrings[weirdStrings.Count - 1] += byteString;
                        else weirdStrings.Add(byteString);
                    }
                    previousByteWasValid = valid;
                }

                bool found = false;
                foreach (string str in weirdStrings)
                {
                    if (str.Length >= 2)
                    {
                        if (found) { myNamespacesList.Add(str); break; }

                        listBeforeData.Add(str);

                        if (str == "Outfit"
                        || str == "Back Bling"
                        || str == "Pet"
                        || str == "Harvesting Tool"
                        || str == "Glider"
                        || str == "Contrail"
                        || str == "Emote"
                        || str == "Emoticon"
                        || str == "Toy"
                        || str == "Music")
                        {
                            int index = listBeforeData.IndexOf(str);

                            //Console.WriteLine("DName: " + listBeforeData[index - 3]);

                            myNamespacesList.Add(listBeforeData[index - 3]);
                            myNamespacesList.Add(listBeforeData[index - 1]);

                            found = true;
                        }
                    }
                }
            }
        }

        public static void getNameMap(string filepath)
        {
            Console.Clear();

            using (BinaryReader reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.GetEncoding(1252)))
            {
                reader.ReadBytes(24);

                int fileLength = reader.ReadInt32();
                Console.WriteLine(fileLength);

                reader.ReadBytes(13);
                int NamespaceCount = reader.ReadInt32();

                long LocalizedStringArrayOffset = -1;
                LocalizedStringArrayOffset = reader.ReadInt64();
                if (LocalizedStringArrayOffset != -1)
                {
                    long newOffset = LocalizedStringArrayOffset - 4;
                    reader.BaseStream.Seek(newOffset, SeekOrigin.Begin);

                    string[] LocalizedStringArray = new string[NamespaceCount];
                    for (int i = 0; i < LocalizedStringArray.Length; i++)
                    {
                        string tag = AssetReader.readCleanString(reader);
                        if (tag != "None") { LocalizedStringArray[i] = tag; }

                        Console.WriteLine(LocalizedStringArray[i]);
                    }
                }
            }
        }
    }
}
