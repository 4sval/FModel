using System.Drawing;

namespace FModel
{
    static class LoadLocRes
    {
        public static void LoadMySelectedLocRes(string selectedLanguage)
        {
            switch (selectedLanguage)
            {
                case "French":
                    getMyLocRes("fr");
                    break;
                case "German":
                    getMyLocRes("de");
                    break;
                case "Italian":
                    getMyLocRes("it");
                    break;
                case "Spanish":
                    getMyLocRes("es");
                    break;
                case "Spanish (LA)":
                    getMyLocRes("es-419");
                    break;
                case "Arabic":
                    getMyLocRes("ar");
                    break;
                case "Japanese":
                    getMyLocRes("ja");
                    break;
                case "Korean":
                    getMyLocRes("ko");
                    break;
                case "Polish":
                    getMyLocRes("pl");
                    break;
                case "Portuguese (Brazil)":
                    getMyLocRes("pt-BR");
                    break;
                case "Russian":
                    getMyLocRes("ru");
                    break;
                case "Turkish":
                    getMyLocRes("tr");
                    break;
                case "Chinese (S)":
                    getMyLocRes("zh-CN");
                    break;
                case "Traditional Chinese":
                    getMyLocRes("zh-Hant");
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
        private static void getMyLocRes(string selectedLanguage)
        {
            if (ThePak.AllpaksDictionary != null)
            {
                if (ThePak.AllpaksDictionary.ContainsKey("Game_BR.locres"))
                {
                    string locResPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary["Game_BR.locres"], "Game_BR.locres");

                    LocResSerializer.setLocRes(locResPath.Replace("zh-Hant", selectedLanguage));
                }
                else
                {
                    new UpdateMyConsole("Game_BR.locres ", Color.Crimson).AppendToConsole();
                    new UpdateMyConsole("not found", Color.Black, true).AppendToConsole();
                    new UpdateMyConsole("Icon language set to ", Color.Black).AppendToConsole();
                    new UpdateMyConsole("English", Color.CornflowerBlue, true).AppendToConsole();

                    Properties.Settings.Default.IconLanguage = "English";
                    Properties.Settings.Default.Save();
                }

                if (ThePak.AllpaksDictionary.ContainsKey("Game_StW.locres"))
                {
                    string locResPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary["Game_StW.locres"], "Game_StW.locres");

                    LocResSerializer.setLocRes(locResPath.Replace("zh-Hant", selectedLanguage), true);
                }
                else
                {
                    new UpdateMyConsole("Game_StW.locres ", Color.Crimson).AppendToConsole();
                    new UpdateMyConsole("not found", Color.Black, true).AppendToConsole();
                }
            }
        }
    }
}
