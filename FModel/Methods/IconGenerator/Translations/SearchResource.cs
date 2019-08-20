using System.Collections.Generic;

namespace FModel
{
    static class SearchResource
    {
        public static string getTranslatedText(string theNamespace, string theKey)
        {
            try
            {
                if (string.IsNullOrEmpty(theNamespace))
                {
                    return LocResSerializer.LocResDict[theKey];
                }
                else
                {
                    return LocResSerializer.LocResDict[theNamespace][theKey];
                }
            }
            catch (KeyNotFoundException)
            {
                return string.Empty;
            }
        }

        public static string getTextByKey(string key, string defaultText, string namespac = null)
        {
            if (Properties.Settings.Default.IconLanguage.Equals("English"))
                return defaultText;

            string text = defaultText;
            if (LocResSerializer.LocResDict != null)
            {
                text = getTranslatedText(namespac, key);
                if (string.IsNullOrEmpty(text))
                    text = defaultText;
            }

            return text;
        }
    }
}
