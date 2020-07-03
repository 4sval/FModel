using FModel.Creator.Fortnite;
using FModel.Creator.Icons;
using FModel.Utils;
using PakReader.Pak;
using PakReader.Parsers.Class;
using PakReader.Parsers.Objects;
using PakReader.Parsers.PropertyTagData;

namespace FModel.Creator.Texts
{
    static class GameplayTag
    {
        public static void GetGameplayTags(BaseIcon icon, StructProperty s, string exportType)
        {
            if (s.Value is FGameplayTagContainer g)
            {
                if (g.GameplayTags.TryGetGameplayTag("Cosmetics.Source.", out var source))
                    icon.CosmeticSource = source.String.Substring("Cosmetics.Source.".Length);
                else if(g.GameplayTags.TryGetGameplayTag("Athena.ItemAction.", out var action))
                    icon.CosmeticSource = action.String.Substring("Athena.ItemAction.".Length);

                if (g.GameplayTags.TryGetGameplayTag("Cosmetics.Set.", out var set))
                    icon.Description += GetCosmeticSet(set.String);
                if (g.GameplayTags.TryGetGameplayTag("Cosmetics.Filter.Season.", out var season))
                    icon.Description += GetCosmeticSeason(season.String);

                UserFacingFlag.GetUserFacingFlags(
                    g.GameplayTags.GetAllGameplayTag("Cosmetics.UserFacingFlags.", "Homebase.Class.", "NPC.CharacterType.Survivor.Defender."),
                    icon, exportType);
            }
        }

        private static string GetCosmeticSet(string setName)
        {
            PakPackage p = Utils.GetPropertyPakPackage("/Game/Athena/Items/Cosmetics/Metadata/CosmeticSets");
            if (p.HasExport() && !p.Equals(default))
            {
                var d = p.GetExport<UDataTable>();
                if (d != null && d.TryGetValue(setName, out var obj) && obj is UObject o)
                {
                    if (o.TryGetValue("DisplayName", out var displayName) && displayName is TextProperty t)
                    {
                        (string n, string k, string s) = Text.GetTextPropertyBases(t);
                        string set = Localizations.GetLocalization(n, k, s);
                        string format = Localizations.GetLocalization("Fort.Cosmetics", "CosmeticItemDescription_SetMembership_NotRich", "\nPart of the {0} set.");
                        return string.Format(format, set);
                    }
                }
            }
            return string.Empty;
        }

        private static string GetCosmeticSeason(string seasonNumber)
        {
            string s = seasonNumber.Substring("Cosmetics.Filter.Season.".Length);
            int number = int.Parse(s);
            if (number == 10)
                s = "X";

            string season = Localizations.GetLocalization("AthenaSeasonItemDefinitionInternal", "SeasonTextFormat", "Season {0}");
            string introduced = Localizations.GetLocalization("Fort.Cosmetics", "CosmeticItemDescription_Season", "\nIntroduced in <SeasonText>{0}</>.")
                .Replace("<SeasonText>", string.Empty).Replace("</>", string.Empty);
            if (number > 10)
            {
                string chapter = Localizations.GetLocalization("AthenaSeasonItemDefinitionInternal", "ChapterTextFormat", "Chapter {0}");
                string chapterFormat = Localizations.GetLocalization("AthenaSeasonItemDefinitionInternal", "ChapterSeasonTextFormat", "{0}, {1}");
                string d = string.Format(chapterFormat, string.Format(chapter, number / 10 + 1), string.Format(season, s[^1..]));
                return string.Format(introduced, d);
            }
            else return string.Format(introduced, string.Format(season, s));
        }
    }
}
