using Newtonsoft.Json.Linq;
using FModel.Parser.LocResParser;

namespace FModel
{
    static class SearchResource
    {
        private static string parsedJsonToCheck { get; set; }
        private static JObject jo { get; set; }
        private static string oldLanguage = Properties.Settings.Default.IconLanguage;

        public static string getTranslatedText(string theNamespace)
        {
            string toReturn = string.Empty;
            string newLanguage = Properties.Settings.Default.IconLanguage;

            if (parsedJsonToCheck == null || newLanguage != oldLanguage)
            {
                parsedJsonToCheck = JToken.Parse(LoadLocRes.myLocRes).ToString().TrimStart('[').TrimEnd(']');
                jo = JObject.Parse(parsedJsonToCheck);
            }
            foreach (JToken token in jo.FindTokens(theNamespace))
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
    }
}
