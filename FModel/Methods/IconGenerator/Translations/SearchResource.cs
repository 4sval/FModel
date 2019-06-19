using Newtonsoft.Json.Linq;
using FModel.Parser.LocResParser;

namespace FModel
{
    static class SearchResource
    {
        private static string parsedJsonToCheck { get; set; }
        private static JObject jo { get; set; }
        private static string oldLanguage = Properties.Settings.Default.IconLanguage;

        /// <summary>
        /// for most (if not all) of our translations there's no namespace so we just have to find the key in the string
        /// once found, we only take the part between this key and we parse to get out LocResText
        /// To improve speed, we check if the language has change or if JObject has never been loaded
        /// </summary>
        /// <param name="theKey"></param>
        /// <returns></returns>
        public static string getTranslatedText(string theKey)
        {
            string toReturn = string.Empty;
            string newLanguage = Properties.Settings.Default.IconLanguage;

            if (parsedJsonToCheck == null || newLanguage != oldLanguage)
            {
                parsedJsonToCheck = JToken.Parse(LoadLocRes.myLocRes).ToString().TrimStart('[').TrimEnd(']');
                jo = JObject.Parse(parsedJsonToCheck);
            }

            foreach (JToken token in jo.FindTokens(theKey))
            {
                LocResParser LocResParse = LocResParser.FromJson(token.ToString());
                if (LocResParse.LocResText != null)
                {
                    toReturn = LocResParse.LocResText;
                }
            }

            oldLanguage = newLanguage;
            return toReturn;
        }

        public static string getTextByKey(string key, string defaultText)
        {
            string text = defaultText;
            if (LoadLocRes.myLocRes != null && Properties.Settings.Default.IconLanguage != "English")
            {
                text = getTranslatedText(key);
                if (string.IsNullOrEmpty(text))
                    text = defaultText;
            }

            return text;
        }
    }
}
