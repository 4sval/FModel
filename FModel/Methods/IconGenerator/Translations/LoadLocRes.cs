using System.Drawing;

namespace FModel
{
    static class LoadLocRes
    {
        public static string myLocRes { get; set; }

        public static void LoadMySelectedLocRes(string selectedLanguage)
        {
            switch (selectedLanguage)
            {
                case "French":
                    myLocRes = getMyLocRes("fr");
                    break;
                case "German":
                    myLocRes = getMyLocRes("de");
                    break;
                case "Italian":
                    myLocRes = getMyLocRes("it");
                    break;
                case "Spanish":
                    myLocRes = getMyLocRes("es");
                    break;
                case "Spanish (LA)":
                    myLocRes = getMyLocRes("es-419");
                    break;
                case "Arabic":
                    myLocRes = getMyLocRes("ar");
                    break;
                case "Japanese":
                    myLocRes = getMyLocRes("ja");
                    break;
                case "Korean":
                    myLocRes = getMyLocRes("ko");
                    break;
                case "Polish":
                    myLocRes = getMyLocRes("pl");
                    break;
                case "Portuguese (Brazil)":
                    myLocRes = getMyLocRes("pt-BR");
                    break;
                case "Russian":
                    myLocRes = getMyLocRes("ru");
                    break;
                case "Turkish":
                    myLocRes = getMyLocRes("tr");
                    break;
                case "Chinese (S)":
                    myLocRes = getMyLocRes("zh-CN");
                    break;
                case "Traditional Chinese":
                    myLocRes = getMyLocRes("zh-Hant");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 1. if loading a dynamic pak we have to switch between keys because the translation file is the main paks hence string oldKey is there
        /// 2. smh if loading a dynamic pak, the guid isn't reset when registering, the temp solution is to fake the guid
        /// </summary>
        /// <param name="selectedLanguage"></param>
        /// <returns></returns>
        private static string getMyLocRes(string selectedLanguage)
        {
            if (ThePak.AllpaksDictionary != null)
            {
                if (ThePak.AllpaksDictionary.ContainsKey("Game_BR.locres"))
                {
                    string locResPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary["Game_BR.locres"], "Game_BR.locres");

                    return LocResSerializer.StringFinder(locResPath.Replace("zh-Hant", selectedLanguage));
                }
                else
                {
                    new UpdateMyConsole("[FModel] Localization File Not Found - Icon Language set to English", Color.DarkRed, true).AppendToConsole();
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
