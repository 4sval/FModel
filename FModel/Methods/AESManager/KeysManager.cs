using FModel.Methods.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.AESManager
{
    static class KeysManager
    {
        public static readonly XmlSerializer serializer = new XmlSerializer(typeof(List<AESInfosEntry>));
        private static readonly string AESManager_PATH = FProp.Default.FOutput_Path + "\\FAESManager.xml";

        public static void Serialize(string PAKName, string PAKKey)
        {
            DebugHelper.WriteLine("FAESManager: Serializing " + PAKName + " with " + PAKKey);
            AESEntries.AESEntriesList.Add(new AESInfosEntry
            {
                ThePAKName = PAKName,
                ThePAKKey = PAKKey
            });
        }

        public static void Deserialize()
        {
            if (File.Exists(AESManager_PATH))
            {
                DebugHelper.WriteLine("FAESManager: Deserializing");
                List<AESInfosEntry> outputList;
                using (var fileStream = new FileStream(AESManager_PATH, FileMode.Open))
                {
                    outputList = (List<AESInfosEntry>)serializer.Deserialize(fileStream);
                }
                AESEntries.AESEntriesList = outputList;
            }
            else
                DebugHelper.WriteLine("FAESManager: Deserializing but " + AESManager_PATH + " not found");
        }
    }
}
