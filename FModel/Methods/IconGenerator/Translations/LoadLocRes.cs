namespace FModel
{
    static class LoadLocRes
    {
        public static string myLocRes { get; set; }

        public static void LoadMySelectedLocRes(string selectedLanguage)
        {
            switch (selectedLanguage)
            {
                case "English":
                    myLocRes = getMyLocRes("en");
                    break;
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
                default:
                    break;
            }
        }

        private static string getMyLocRes(string selectedLanguage)
        {
            string locResPath = JohnWick.ExtractAsset(ThePak.AllpaksDictionary["Game_BR.locres"], "Game_BR.locres");
            return LocResSerializer.StringFinder(locResPath.Replace("zh-Hant", selectedLanguage));
        }
    }
}
