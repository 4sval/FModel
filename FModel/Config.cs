using System;
using System.IO;
using Newtonsoft.Json;

namespace FModel
{
    class Config
    {
        private static string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString() + "\\FModel";
        private const string configFile = "config.json";
        public static ConfigFile conf;

        static Config()
        {
            if (!Directory.Exists(docPath))
                Directory.CreateDirectory(docPath);
            if (!File.Exists(docPath + "/" + configFile))
            {
                string json = JsonConvert.SerializeObject(conf, Formatting.Indented);
                File.WriteAllText(docPath + "/" + configFile, json);
            }
            else
            {
                string json = File.ReadAllText(docPath + "/" + configFile);
                conf = JsonConvert.DeserializeObject<ConfigFile>(json);
            }
        }
    }
    public struct ConfigFile
    {
        public string pathToFortnitePAKs;
    }
}