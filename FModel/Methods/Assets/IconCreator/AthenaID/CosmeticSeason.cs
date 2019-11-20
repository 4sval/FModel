namespace FModel.Methods.Assets.IconCreator.AthenaID
{
    static class CosmeticSeason
    {
        public static string GetCosmeticSeason(string SeasonNumber)
        {
            string s = SeasonNumber;
            int snumber = int.Parse(SeasonNumber);
            if (snumber == 10)
                s = "X";

            string introduced = AssetTranslations.SearchTranslation("Fort.Cosmetics", "CosmeticItemDescription_Season", "\nIntroduced in <SeasonText>{0}</>.").Replace("<SeasonText>", string.Empty).Replace("</>", string.Empty); //2 separate because of Japanese
            string stext = AssetTranslations.SearchTranslation("AthenaSeasonItemDefinitionInternal", "SeasonTextFormat", "Season {0}");

            //display Chapter {First Letter Of SeasonNumber + 1 (S11 -> Chapter 1 + 1 = 2)}, Season {Last Letter Of SeasonNumber (S11 -> Season 1)}
            //this can easily break lol but idk where are the right numbers for each item
            if (snumber > 10)
            {
                string schapter = AssetTranslations.SearchTranslation("AthenaSeasonItemDefinitionInternal", "ChapterTextFormat", "Chapter {0}");
                string sformat = AssetTranslations.SearchTranslation("AthenaSeasonItemDefinitionInternal", "ChapterSeasonTextFormat", "{0}, {1}");

                stext = string.Format(introduced, string.Format(sformat, string.Format(schapter, int.Parse(s.Substring(1)) + 1), string.Format(stext, s.Substring(s.Length - 1))));
                return stext;
            }

            return string.Format(introduced, string.Format(stext, s));
        }
    }
}
