using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;

namespace FModel
{
    static class AESManager
    {
        public static List<AESEntry> AESEntries { get; set; }
        private static XmlSerializer serializer = new XmlSerializer(typeof(List<AESEntry>));

        public static void serialize(string data)
        {
            ConfigurationUserLevel level = ConfigurationUserLevel.PerUserRoamingAndLocal;
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(level);
            string configurationFilePath = configuration.FilePath.Substring(0, configuration.FilePath.LastIndexOf("\\"));

            AESEntries.Add(new AESEntry()
            {
                theKey = data
            });

            string path = configurationFilePath + "\\AESManager.xml";
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                serializer.Serialize(fileStream, AESEntries);
            }
        }

        public static string[] deserialize()
        {
            ConfigurationUserLevel level = ConfigurationUserLevel.PerUserRoamingAndLocal;
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(level);
            string configurationFilePath = configuration.FilePath.Substring(0, configuration.FilePath.LastIndexOf("\\"));

            string path = configurationFilePath + "\\AESManager.xml";

            List<AESEntry> outputList;
            using (var fileStream = new FileStream(path, FileMode.Open))
            {
                outputList = (List<AESEntry>)serializer.Deserialize(fileStream);
            }

            string[] toReturn = new string[outputList.Count];
            for (int i = 0; i < toReturn.Length; i++)
            {
                toReturn[i] = outputList[i].theKey;
            }
            return toReturn;
        }
    }
}
