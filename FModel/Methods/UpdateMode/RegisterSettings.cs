using FModel.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FModel
{
    static class RegisterSettings
    {
        public static Dictionary<string, string> updateModeDictionary { get; set; }
        public static List<string> updateModeListParameters { get; set; }

        public static void UpdateModeAddToDict(string[] myPaksFilesList)
        {
            for (int i = 0; i < myPaksFilesList.Length; i++)
            {
                bool b = updateModeListParameters.Any(s => myPaksFilesList[i].Contains(s));
                if (b)
                {
                    string filename = myPaksFilesList[i].Substring(myPaksFilesList[i].LastIndexOf("/", StringComparison.Ordinal) + 1);
                    if (filename.Contains(".uasset") || filename.Contains(".uexp") || filename.Contains(".ubulk"))
                    {
                        if (!updateModeDictionary.ContainsKey(filename.Substring(0, filename.LastIndexOf(".", StringComparison.Ordinal))))
                            updateModeDictionary.Add(filename.Substring(0, filename.LastIndexOf(".", StringComparison.Ordinal)), myPaksFilesList[i]);
                    }
                }
            }
        }
    }
}
