using System.Drawing;

namespace FModel
{
    static class LoadLocRes
    {
        public static string myLocRes { get; set; }
        public static string myLocResSTW { get; set; }

        public static void LoadMySelectedLocRes(string selectedLanguage)
        {
            switch (selectedLanguage)
            {
                case "French":
                    myLocRes = getMyLocRes("fr");
                    myLocResSTW = getMyLocRes("fr", true);
                    break;
                case "German":
                    myLocRes = getMyLocRes("de");
                    myLocResSTW = getMyLocRes("de", true);
                    break;
                case "Italian":
                    myLocRes = getMyLocRes("it");
                    myLocResSTW = getMyLocRes("it", true);
                    break;
                case "Spanish":
                    myLocRes = getMyLocRes("es");
                    myLocResSTW = getMyLocRes("es", true);
                    break;
                case "Spanish (LA)":
                    myLocRes = getMyLocRes("es-419");
                    myLocResSTW = getMyLocRes("es-419", true);
                    break;
                case "Arabic":
                    myLocRes = getMyLocRes("ar");
                    myLocResSTW = getMyLocRes("ar", true);
                    break;
                case "Japanese":
                    myLocRes = getMyLocRes("ja");
                    myLocResSTW = getMyLocRes("ja", true);
                    break;
                case "Korean":
                    myLocRes = getMyLocRes("ko");
                    myLocResSTW = getMyLocRes("ko", true);
                    break;
                case "Polish":
                    myLocRes = getMyLocRes("pl");
                    myLocResSTW = getMyLocRes("pl", true);
                    break;
                case "Portuguese (Brazil)":
                    myLocRes = getMyLocRes("pt-BR");
                    myLocResSTW = getMyLocRes("pt-BR", true);
                    break;
                case "Russian":
                    myLocRes = getMyLocRes("ru");
                    myLocResSTW = getMyLocRes("ru", true);
                    break;
                case "Turkish":
                    myLocRes = getMyLocRes("tr");
                    myLocResSTW = getMyLocRes("tr", true);
                    break;
                case "Chinese (S)":
                    myLocRes = getMyLocRes("zh-CN");
                    myLocResSTW = getMyLocRes("zh-CN", true);
                    break;
                case "Traditional Chinese":
                    myLocRes = getMyLocRes("zh-Hant");
                    myLocResSTW = getMyLocRes("zh-Hant", true);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectedLanguage"></param>
        /// <returns></returns>
        private static string getMyLocRes(string selectedLanguage, bool isSTW = false)
        {
            if (ThePak.AllpaksDictionary != null)
            {
                if (ThePak.AllpaksDictionary.ContainsKey(isSTW ? "Game_StW.locres" : "Game_BR.locres"))
                {
                    string locResPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary[isSTW ? "Game_StW.locres" : "Game_BR.locres"], isSTW ? "Game_StW.locres" : "Game_BR.locres");

                    return LocResSerializer.StringFinder(locResPath.Replace("zh-Hant", selectedLanguage));
                }
                else
                {
                    new UpdateMyConsole("[FModel] "+ (isSTW ? "STW" : "BR")  +" Localization File Not Found - Icon Language set to English", Color.DarkRed, true).AppendToConsole();
                    new UpdateMyConsole("", Color.Black, true).AppendToConsole();

                    Properties.Settings.Default.IconLanguage = "English";
                    Properties.Settings.Default.Save();

                    return "";
                }
            }
            else { return ""; }
        }
    }
}
