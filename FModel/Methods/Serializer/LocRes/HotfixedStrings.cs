using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FModel.Methods.Serializer.LocRes
{
    class HotfixedStrings
    {
        public static Dictionary<string, dynamic> HotfixedStringsDict { get; set; }

        public static void setHotfixedStrings()
        {
            string pdd = Path.GetFullPath(Path.Combine(Properties.Settings.Default.PAKsPath, @"..\..\PersistentDownloadDir\EMS\"));
            if (File.Exists(pdd + "a22d837b6a2b46349421259c0a5411bf"))
            {
                HotfixedStringsDict = new Dictionary<string, dynamic>();
                using (StreamReader sr = new StreamReader(File.Open(pdd + "a22d837b6a2b46349421259c0a5411bf", FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.StartsWith("+TextReplacements=(Category=Game,"))
                        {
                            string txtNamespace = GetValueFromParam(line, "Namespace=\"", "\",");
                            string txtKey = GetValueFromParam(line, "Key=\"", "\",");

                            int trIndex = line.IndexOf("LocalizedStrings=(") + "LocalizedStrings=(".Length;
                            string translations = GetValueFromParam(line, "LocalizedStrings=(", "))");
                            if (!translations.EndsWith(")")) { translations = translations + ")"; }

                            HotfixedStringsDict[txtKey] = new Dictionary<string, string>();
                            HotfixedStringsDict[txtKey]["namespace"] = txtNamespace;
                            Regex regex = new Regex(@"(?<=\().+?(?=\))");
                            foreach (Match match in regex.Matches(translations))
                            {
                                try
                                {
                                    string[] langParts = match.Value.Substring(1, match.Value.Length - 2).Split(new string[] { "\",\"" }, StringSplitOptions.None);

                                    HotfixedStringsDict[txtKey][langParts[0]] = langParts[1];
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    string[] langParts = match.Value.Substring(1, match.Value.Length - 2).Split(new string[] { "\", \"" }, StringSplitOptions.None);

                                    HotfixedStringsDict[txtKey][langParts[0]] = langParts[1];
                                }
                            }
                        }
                    }
                }
            }
        }

        private static string GetValueFromParam(string fullLine, string startWith, string endWith)
        {
            int startIndex = fullLine.IndexOf(startWith) + startWith.Length;
            int endIndex = fullLine.Substring(startIndex).IndexOf(endWith);
            return fullLine.Substring(startIndex, endIndex);
        }
    }
}
