using FModel.Methods.Auth;
using FModel.Methods.Utilities;
using PakReader;
using System;
using System.Collections.Generic;
using System.IO;
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
            string partialPath = "/FortniteGame/Content/Localization/Game_BR/{0}/Game_BR.locres";
            switch (language)
            {
                case "French":
                    PopulateDict(string.Format(partialPath, "fr"));
                    break;
                case "German":
                    PopulateDict(string.Format(partialPath, "de"));
                    break;
                case "Italian":
                    PopulateDict(string.Format(partialPath, "it"));
                    break;
                case "Spanish":
                    PopulateDict(string.Format(partialPath, "es"));
                    break;
                case "Spanish (LA)":
                    PopulateDict(string.Format(partialPath, "es-419"));
                    break;
                case "Arabic":
                    PopulateDict(string.Format(partialPath, "ar"));
                    break;
                case "Japanese":
                    PopulateDict(string.Format(partialPath, "ja"));
                    break;
                case "Korean":
                    PopulateDict(string.Format(partialPath, "ko"));
                    break;
                case "Polish":
                    PopulateDict(string.Format(partialPath, "pl"));
                    break;
                case "Portuguese (Brazil)":
                    PopulateDict(string.Format(partialPath, "pt-BR"));
                    break;
                case "Russian":
                    PopulateDict(string.Format(partialPath, "ru"));
                    break;
                case "Turkish":
                    PopulateDict(string.Format(partialPath, "tr"));
                    break;
                case "Chinese (S)":
                    PopulateDict(string.Format(partialPath, "zh-CN"));
                    break;
                case "Traditional Chinese":
                    PopulateDict(string.Format(partialPath, "zh-Hant"));
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

        private static void PopulateDict(string LocResPath)
        {
            DebugHelper.WriteLine(".PAKs: Loading " + LocResPath + " as the translation file");

            if (HotfixLocResDict == null) { SetHotfixedLocResDict(); } //once, no need to do more
            BRLocResDict = GetLocResDict(LocResPath);
            STWLocResDict = GetLocResDict(LocResPath.Replace("Game_BR", "Game_StW"));
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
                        if (line.StartsWith("+TextReplacements=(Category=Game,"))
                        {
                            string txtNamespace = GetValueFromParam(line, "Namespace=\"", "\",");
                            string txtKey = GetValueFromParam(line, "Key=\"", "\",");
                            string txtNativeString = GetValueFromParam(line, "NativeString=\"", "\",");

                            string translations = GetValueFromParam(line, "LocalizedStrings=(", "))");
                            if (!translations.EndsWith(")")) { translations = translations + ")"; }

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
                                    HotfixLocResDict[txtNamespace][txtKey][txtNativeString][langParts[0]] = langParts[1];
                                }
                            }
                        }
                    }
                    DebugHelper.WriteLine(".PAKs: Populated hotfixed string dictionary");
                }
            }
        }

        private static string GetValueFromParam(string fullLine, string startWith, string endWith)
        {
            int startIndex = fullLine.IndexOf(startWith, StringComparison.InvariantCultureIgnoreCase) + startWith.Length;
            int endIndex = fullLine.Substring(startIndex).ToString().IndexOf(endWith, StringComparison.InvariantCultureIgnoreCase);
            return fullLine.Substring(startIndex, endIndex);
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
