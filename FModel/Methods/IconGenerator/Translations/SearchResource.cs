using FModel.Methods.Serializer.LocRes;
using FModel.Properties;
using System.Collections.Generic;

namespace FModel
{
    static class SearchResource
    {
        public static string getTranslatedText(string theNamespace, string theKey)
        {
            try
            {
                if (HotfixedStrings.HotfixedStringsDict != null && HotfixedStrings.HotfixedStringsDict.ContainsKey(theKey))
                {
                    if (HotfixedStrings.HotfixedStringsDict[theKey].ContainsKey(GetLanguageCode()))
                        return HotfixedStrings.HotfixedStringsDict[theKey][GetLanguageCode()];
                    else
                        return string.IsNullOrEmpty(theNamespace) ? LocResSerializer.LocResDict[theKey] : LocResSerializer.LocResDict[theNamespace][theKey];
                }
                else
                    return string.IsNullOrEmpty(theNamespace) ? LocResSerializer.LocResDict[theKey] : LocResSerializer.LocResDict[theNamespace][theKey];
            }
            catch (KeyNotFoundException)
            {
                return string.Empty;
            }
        }

        public static string getTextByKey(string key, string defaultText, string namespac = null)
        {
            if (Properties.Settings.Default.IconLanguage.Equals("English"))
            {
                if (HotfixedStrings.HotfixedStringsDict != null && HotfixedStrings.HotfixedStringsDict.ContainsKey(key))
                {
                    return HotfixedStrings.HotfixedStringsDict[key].ContainsKey(GetLanguageCode())
                        ? HotfixedStrings.HotfixedStringsDict[key][GetLanguageCode()]
                        : defaultText;
                }
                else
                    return defaultText;
            }

            string text = defaultText;
            if (LocResSerializer.LocResDict != null)
            {
                text = getTranslatedText(namespac, key);
                if (string.IsNullOrEmpty(text))
                    text = defaultText;
            }

            return text;
        }

        private static string GetLanguageCode()
        {
            switch (Settings.Default.IconLanguage)
            {
                case "French":
                    return "fr";
                case "German":
                    return  "de";
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
