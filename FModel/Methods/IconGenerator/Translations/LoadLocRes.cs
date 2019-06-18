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

        private static string getMyLocRes(string selectedLanguage)
        {
            string oldKey = JohnWick.MyKey;
            JohnWick.MyKey = Properties.Settings.Default.AESKey;
            string locResPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary["Game_BR.locres"], "Game_BR.locres");
            JohnWick.MyKey = oldKey;

            return LocResSerializer.StringFinder(locResPath.Replace("zh-Hant", selectedLanguage));
        }
    }
}
