using System.Collections.Generic;

namespace FModel
{
    static class SearchResource
    {
        /// <summary>
        /// because idk, i made my serializer in a weird way
        /// if a namespace is empty the key become the namespace and the new key is "LocResText"
        /// i could change the serializer but it's gonna break a lot of things and i don't wanna take time to fix them
        /// </summary>
        /// <param name="theNamespace"></param>
        /// <param name="theKey"></param>
        /// <returns></returns>
        public static string getTranslatedText(string theNamespace, string theKey)
        {
            try
            {
                if (!string.Equals(theNamespace, "LocResText"))
                {
                    return LocResSerializer.LocResDict[theNamespace][theKey];
                }
                else
                {
                    return LocResSerializer.LocResDict[theKey][theNamespace];
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
                text = getTranslatedText(namespac == null ? "LocResText" : namespac, key);
                if (string.IsNullOrEmpty(text))
                    text = defaultText;
            }

            return text;
        }
    }
}
