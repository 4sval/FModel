using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;

namespace FModel
{
    static class DynamicKeysManager
    {
        public static List<AESEntry> AESEntries { get; set; }
        private static XmlSerializer serializer = new XmlSerializer(typeof(List<AESEntry>));
        private static string path = Properties.Settings.Default.ExtractOutput + "\\AESManager.xml";

        public static void serialize(string key, string pak)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            AESEntries.Add(new AESEntry()
            {
                theKey = key,
                thePak = pak
            });

            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                serializer.Serialize(fileStream, AESEntries);
            }
        }

        public static void deserialize()
        {
            if (File.Exists(path))
            {
                List<AESEntry> outputList;
                using (var fileStream = new FileStream(path, FileMode.Open))
                {
                    outputList = (List<AESEntry>)serializer.Deserialize(fileStream);
                }
                AESEntries = outputList;
            }
        }
    }
}
