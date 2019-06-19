using System;

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
            if (ThePak.AllpaksDictionary != null && ThePak.AllpaksDictionary["Game_BR.locres"] != null)
            {
                string oldKey = JohnWick.MyKey; //get the old key
                string oldGuid = ThePak.CurrentUsedPakGuid; //get the old guid

                JohnWick.MyKey = Properties.Settings.Default.AESKey; //set the main key to extract
                ThePak.CurrentUsedPakGuid = "0-0-0-0"; //fake the guid -> writeFile need this guid to get the mountPoint, otherwise it crashes

                string locResPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary["Game_BR.locres"], "Game_BR.locres");

                JohnWick.MyKey = oldKey; //set the old key
                ThePak.CurrentUsedPakGuid = oldGuid; //set the old guid

                return LocResSerializer.StringFinder(locResPath.Replace("zh-Hant", selectedLanguage));
            }
            else { return ""; }
        }
    }
}
