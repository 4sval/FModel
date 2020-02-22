using FModel.Methods.Auth;
using FModel.Methods.Utilities;
using PakReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Assets
{
    static class AssetTranslations
    {
        public static Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> HotfixLocResDict { get; set; } //namespace -> key -> language -> string
        public static Dictionary<string, Dictionary<string, string>> BRLocResDict { get; set; } //namespace -> key -> string
        public static Dictionary<string, Dictionary<string, string>> STWLocResDict { get; set; } //namespace -> key -> string

        public static void SetAssetTranslation(string language)
        {
            string locFolder = "/FortniteGame/Content/Localization/";
            string locFileNameBase = "Game_BR";
            switch (language)
            {
                case "French":
                    PopulateDict(locFolder, locFileNameBase, "fr");
                    break;
                case "German":
                    PopulateDict(locFolder, locFileNameBase, "de");
                    break;
                case "Italian":
                    PopulateDict(locFolder, locFileNameBase, "it");
                    break;
                case "Spanish":
                    PopulateDict(locFolder, locFileNameBase, "es");
                    break;
                case "Spanish (LA)":
                    PopulateDict(locFolder, locFileNameBase, "es-419");
                    break;
                case "Arabic":
                    PopulateDict(locFolder, locFileNameBase, "ar");
                    break;
                case "Japanese":
                    PopulateDict(locFolder, locFileNameBase, "ja");
                    break;
                case "Korean":
                    PopulateDict(locFolder, locFileNameBase, "ko");
                    break;
                case "Polish":
                    PopulateDict(locFolder, locFileNameBase, "pl");
                    break;
                case "Portuguese (Brazil)":
                    PopulateDict(locFolder, locFileNameBase, "pt-BR");
                    break;
                case "Russian":
                    PopulateDict(locFolder, locFileNameBase, "ru");
                    break;
                case "Turkish":
                    PopulateDict(locFolder, locFileNameBase, "tr");
                    break;
                case "Chinese (S)":
                    PopulateDict(locFolder, locFileNameBase, "zh-CN");
                    break;
                case "Traditional Chinese":
                    PopulateDict(locFolder, locFileNameBase, "zh-Hant");
                    break;
                default:
                    if (HotfixLocResDict == null) { SetHotfixedLocResDict(); } //once, no need to do more
                    break;
            }
        }

        public static string SearchTranslation(string tNamespace, string tKey, string ifNotFound)
        {
            if (HotfixLocResDict != null
                && HotfixLocResDict.ContainsKey(tNamespace)
                && HotfixLocResDict[tNamespace].ContainsKey(tKey)
                && HotfixLocResDict[tNamespace][tKey].ContainsKey(ifNotFound))
            {
                string ifNotFoundTemp = ifNotFound;

                // If there is a default text in hotfix, it's changed.
                bool isHotfixDefault = false;
                if (HotfixLocResDict[tNamespace][tKey].ContainsKey("en"))
                {
                    ifNotFound = HotfixLocResDict[tNamespace][tKey][ifNotFound]["en"];
                    isHotfixDefault = true;
                }

                if (HotfixLocResDict[tNamespace][tKey][ifNotFoundTemp].ContainsKey(GetLanguageCode()))
                {
                    string hotfixString = HotfixLocResDict[tNamespace][tKey][ifNotFoundTemp][GetLanguageCode()];

                    // ONLY if there is english in the hotfix.
                    // If the translation is empty, it will be the default text.
                    if (isHotfixDefault && !string.IsNullOrEmpty(ifNotFound) && string.IsNullOrEmpty(hotfixString))
                        hotfixString = ifNotFound;

                    return hotfixString;
                }
            }

            if (FProp.Default.FLanguage == "English")
            {
                return ifNotFound;
            }
            else if (BRLocResDict != null && BRLocResDict.ContainsKey(tNamespace) && BRLocResDict[tNamespace].ContainsKey(tKey))
            {
                return BRLocResDict[tNamespace][tKey];
            }
            else if (STWLocResDict != null && STWLocResDict.ContainsKey(tNamespace) && STWLocResDict[tNamespace].ContainsKey(tKey))
            {
                return STWLocResDict[tNamespace][tKey];
            }
            else
            {
                return ifNotFound;
            }
        }

        private static void PopulateDict(string folder, string fileName, string lang)
        {
            string path = $"{folder}{fileName}/{lang}/{fileName}.locres";
            DebugHelper.WriteLine(".PAKs: Loading " + path + " as the translation file");

            if (HotfixLocResDict == null) { SetHotfixedLocResDict(); } //once, no need to do more

            Dictionary<string, Dictionary<string, string>> finalDict = new Dictionary<string, Dictionary<string, string>>();
            foreach (FPakEntry[] PAKsFileInfos in PAKEntries.PAKToDisplay.Values)
            {
                IEnumerable<string> locresFilesPath = PAKsFileInfos.Where(x => x.Name.StartsWith(folder + fileName) && x.Name.Contains($"/{lang}/") && x.Name.EndsWith(".locres")).Select(x => x.Name);
                if (locresFilesPath.Any())
                    foreach (string file in locresFilesPath)
                    {
                        var dict = GetLocResDict(file);
                        foreach (var namespac in dict)
                        {
                            if (!finalDict.ContainsKey(namespac.Key))
                                finalDict.Add(namespac.Key, new Dictionary<string, string>());

                            foreach (var key in namespac.Value)
                                finalDict[namespac.Key].Add(key.Key, key.Value);
                        }
                    }
            }
            BRLocResDict = finalDict;
            STWLocResDict = GetLocResDict(path.Replace("Game_BR", "Game_StW"));
        }

        private static Dictionary<string, Dictionary<string, string>> GetLocResDict(string LocResPath)
        {
            PakReader.PakReader reader = AssetsUtility.GetPakReader(LocResPath);
            if (reader != null)
            {
                List<FPakEntry> entriesList = AssetsUtility.GetPakEntries(LocResPath);
                foreach (FPakEntry entry in entriesList)
                {
                    if (string.Equals(Path.GetExtension(entry.Name.ToLowerInvariant()), ".locres"))
                    {
                        using (var s = reader.GetPackageStream(entry))
                            return new LocResFile(s).Entries;
                    }
                }
            }
            return new Dictionary<string, Dictionary<string, string>>();
        }

        public static void SetHotfixedLocResDict()
        {
            if (!FProp.Default.ELauncherToken.StartsWith("eg1~") || AuthFlow.IsLauncherTokenExpired())
                AuthFlow.SetOAuthLauncherToken();

            if (!AuthFlow.IsLauncherTokenExpired() && FProp.Default.ELauncherToken.StartsWith("eg1~"))
            {
                string response = Requests.GetLauncherEndpoint("https://fortnite-public-service-prod11.ol.epicgames.com/fortnite/api/cloudstorage/system/a22d837b6a2b46349421259c0a5411bf");
                if (!string.IsNullOrEmpty(response))
                {
                    string[] lines = response.Split('\n');
                    DebugHelper.WriteLine(".PAKs: Populating hotfixed string dictionary");
                    HotfixLocResDict = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>();

                    foreach (string line in lines)
                    {
                        HFedStringsProcess(line);
                    }
                    DebugHelper.WriteLine(".PAKs: Populated hotfixed string dictionary");
                }
                else
                {
                    DebugHelper.WriteLine(".PAKs: Hotfixed strings endpoint returned nothing!!! this isn't normal");
                    string pdd = Path.GetFullPath(Path.Combine(FProp.Default.FPak_Path, @"..\..\PersistentDownloadDir\EMS\"));
                    if (File.Exists(pdd + "a22d837b6a2b46349421259c0a5411bf"))
                    {
                        DebugHelper.WriteLine(".PAKs: Populating hotfixed string dictionary at " + pdd + "a22d837b6a2b46349421259c0a5411bf");
                        HotfixLocResDict = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>();
                        using (StreamReader sr = new StreamReader(File.Open(pdd + "a22d837b6a2b46349421259c0a5411bf", FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                        {
                            while (!sr.EndOfStream)
                            {
                                string line = sr.ReadLine();
                                HFedStringsProcess(line);
                            }
                        }
                        DebugHelper.WriteLine(".PAKs: Populated hotfixed string dictionary");
                    }
                    else
                        DebugHelper.WriteLine(".PAKs: No such file or directory " + pdd + "a22d837b6a2b46349421259c0a5411bf");
                }
            }
        }

        private static string GetValueFromParam(string fullLine, string startWith, string endWith)
        {
            int startIndex = fullLine.IndexOf(startWith, StringComparison.InvariantCultureIgnoreCase) + startWith.Length;
            int endIndex = fullLine.Substring(startIndex).ToString().IndexOf(endWith, StringComparison.InvariantCultureIgnoreCase);
            return fullLine.Substring(startIndex, endIndex);
        }

        private static void HFedStringsProcess(string line)
        {
            if (line.StartsWith("+TextReplacements=(Category=Game,"))
            {
                string txtNamespace = GetValueFromParam(line, "Namespace=\"", "\",");
                string txtKey = GetValueFromParam(line, "Key=\"", "\",");
                string txtNativeString = GetValueFromParam(line, "NativeString=\"", "\",");

                string translations = GetValueFromParam(line, "LocalizedStrings=(", "))");
                if (!translations.EndsWith(")")) { translations += ")"; }

                if (!HotfixLocResDict.ContainsKey(txtNamespace))
                    HotfixLocResDict[txtNamespace] = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

                if (!HotfixLocResDict[txtNamespace].ContainsKey(txtKey))
                    HotfixLocResDict[txtNamespace][txtKey] = new Dictionary<string, Dictionary<string, string>>();

                if (!HotfixLocResDict[txtNamespace][txtKey].ContainsKey(txtNativeString))
                    HotfixLocResDict[txtNamespace][txtKey][txtNativeString] = new Dictionary<string, string>();

                Regex regex = new Regex(@"(?<=\().+?(?=\))");
                foreach (Match match in regex.Matches(translations))
                {
                    try
                    {
                        string[] langParts = match.Value.Substring(1, match.Value.Length - 2).Split(new string[] { "\",\"" }, StringSplitOptions.None);
                        HotfixLocResDict[txtNamespace][txtKey][txtNativeString][langParts[0]] = langParts[1];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        string[] langParts = match.Value.Substring(1, match.Value.Length - 2).Split(new string[] { "\", \"" }, StringSplitOptions.None);
                        if (langParts.Length > 1)
                            // 02/22/2020 legendary trad in spanish is miss-typed and cause crash ("es",Legendario""),
                            HotfixLocResDict[txtNamespace][txtKey][txtNativeString][langParts[0]] = langParts[1];
                    }
                }
            }
        }

        private static string GetLanguageCode()
        {
            switch (FProp.Default.FLanguage)
            {
                case "French":
                    return "fr";
                case "German":
                    return "de";
                case "Italian":
                    return "it";
                case "Spanish":
                    return "es";
                case "Spanish (LA)":
                    return "es-419";
                case "Arabic":
                    return "ar";
                case "Japanese":
                    return "ja";
                case "Korean":
                    return "ko";
                case "Polish":
                    return "pl";
                case "Portuguese (Brazil)":
                    return "pt-BR";
                case "Russian":
                    return "ru";
                case "Turkish":
                    return "tr";
                case "Chinese (S)":
                    return "zh-CN";
                case "Traditional Chinese":
                    return "zh-Hant";
                default:
                    return "en";
            }
        }
    }
}
