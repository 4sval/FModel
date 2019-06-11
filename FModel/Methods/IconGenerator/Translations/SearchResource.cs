using Newtonsoft.Json.Linq;
using FModel.Parser.LocResParser;

namespace FModel
{
    static class SearchResource
    {
        private static string parsedJsonToCheck { get; set; }

        public static string getTranslatedText(string theNamespace)
        {
            string toReturn = string.Empty;
            string myParsedJson = string.Empty;

            if (myParsedJson != parsedJsonToCheck)
            {
                myParsedJson = JToken.Parse(LoadLocRes.myLocRes).ToString().TrimStart('[').TrimEnd(']');
            }
            JObject jo = JObject.Parse(myParsedJson);
            foreach (JToken token in jo.FindTokens(theNamespace))
            {
                LocResParser LocResParse = LocResParser.FromJson(token.ToString());
                if (LocResParse.LocResText != null)
                {
                    toReturn = LocResParse.LocResText;
                }
                else if (LocResParse.WorkerSetBonusTraitPattern != null)
                {
                    toReturn = LocResParse.WorkerSetBonusTraitPattern;
                }
            }

            parsedJsonToCheck = myParsedJson;
            return toReturn;
        }
    }
}
