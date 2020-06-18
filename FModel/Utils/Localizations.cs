using FModel.Creator.Texts;
using FModel.Logger;
using FModel.ViewModels.StatusBar;
using PakReader;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FModel.Utils
{
    static class Localizations
    {
        // namespace -> key -> string
        private static Dictionary<string, Dictionary<string, string>> _hotfixLocalizationDict = new Dictionary<string, Dictionary<string, string>>();
        private static readonly Dictionary<string, Dictionary<string, string>> _fortniteLocalizationDict = new Dictionary<string, Dictionary<string, string>>();

        public static async Task SetLocalization(long lang, bool forceReload) => await SetLocalization((ELanguage)lang, forceReload).ConfigureAwait(false);
        public static async Task SetLocalization(ELanguage lang, bool forceReload) => await SetLocalization(GetLanguageCode(lang), forceReload).ConfigureAwait(false);
        public static async Task SetLocalization(string langCode, bool forceReload)
        {
            StatusBarVm.statusBarViewModel.Set(
                string.Format(Properties.Resources.InternationalizationStatus, Properties.Resources.ResourceManager.GetString(((ELanguage)Properties.Settings.Default.AssetsLanguage).ToString())),
                Properties.Resources.Loading);

            if (forceReload)
            {
                // forceReload is used to clear the current dict
                // that way we don't clear when we load pak files (if dict is null, it's still gonna be filled, else it's not gonna be touched)
                // and we clear after selecting a new language in the settings
                // it also avoid the dict to be cleared then filled twice if user already loaded a pak, change the language, then load a new pak
                if (Text.TypeFaces.NeedReload(forceReload)) Text.TypeFaces = new Typefaces();
                Assets.ClearCachedFiles();
                _fortniteLocalizationDict.Clear();
                _hotfixLocalizationDict.Clear();
            }

            // local
            if (_fortniteLocalizationDict.Count <= 0)
            {
                foreach (var fileReader in Globals.CachedPakFiles.Values)
                {
                    foreach (var KvP in fileReader)
                    {
                        Match m = null;
                        string mount = fileReader.MountPoint;
                        string gameName = Folders.GetGameName();
                        if (Globals.Game.ActualGame == EGame.Fortnite)
                            m = Regex.Match(mount + KvP.Value.Name, $"{gameName}/Content/Localization/Fortnite.*?/{langCode}/Fortnite.*", RegexOptions.IgnoreCase);
                        else if (Globals.Game.ActualGame == EGame.Valorant)
                            m = Regex.Match(mount + KvP.Value.Name, $"{gameName}/Content/Localization/Game/{langCode}/Game.locres", RegexOptions.IgnoreCase);
                        else if (Globals.Game.ActualGame == EGame.DeadByDaylight)
                            m = Regex.Match(mount + KvP.Value.Name, $"{gameName}/Content/Localization/{gameName}/{langCode}/{gameName}.locres", RegexOptions.IgnoreCase);
                        else if (Globals.Game.ActualGame == EGame.MinecraftDungeons)
                            m = Regex.Match(mount + KvP.Value.Name, $"{gameName}/Content/Localization/Game/{langCode}/Game.locres", RegexOptions.IgnoreCase);
                        else if (Globals.Game.ActualGame == EGame.BattleBreakers)
                            m = Regex.Match(mount + KvP.Value.Name, $"{gameName}/Content/Localization/Game/{langCode}/Game.locres", RegexOptions.IgnoreCase);

                        if (m != null && m.Success)
                        {
                            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[Localizations]", "[GameDict]", $"Feeding with {KvP.Value.Name} from {KvP.Value.PakFileName} Miam Miam!");

                            using var asset = Assets.GetMemoryStream(fileReader.FileName, mount + KvP.Value.GetPathWithoutExtension());
                            asset.Position = 0;
                            foreach (var namespac in new LocResReader(asset).Entries)
                            {
                                if (!_fortniteLocalizationDict.ContainsKey(namespac.Key))
                                    _fortniteLocalizationDict.Add(namespac.Key, new Dictionary<string, string>());

                                foreach (var key in namespac.Value)
                                    _fortniteLocalizationDict[namespac.Key][key.Key] = key.Value;
                            }
                        }
                    }
                }
            }
            // online
            if (_hotfixLocalizationDict.Count <= 0)
            {
                if (Globals.Game.ActualGame == EGame.Fortnite && NetworkInterface.GetIsNetworkAvailable())
                {
                    var hotfix = await Endpoints.GetJsonEndpoint<Dictionary<string, Dictionary<string, string>>>(Endpoints.BENBOT_HOTFIXES, GetLanguageCode()).ConfigureAwait(false);
                    if (hotfix?.Count > 0)
                    {
                        DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[Localizations]", "[CloudDict]", "Feeding thank to Paul Baran & Donald Davies");
                        _hotfixLocalizationDict = hotfix;
                    }
                }
            }

            if (forceReload)
            {
                StatusBarVm.statusBarViewModel.Set(
                    string.Format(Properties.Resources.InternationalizationStatus, Properties.Resources.ResourceManager.GetString(((ELanguage)Properties.Settings.Default.AssetsLanguage).ToString())),
                    Properties.Resources.Success);
            }
        }

        public static string GetLocalization(string sNamespace, string sKey, string defaultText)
        {
            if (_hotfixLocalizationDict.Count > 0 &&
                _hotfixLocalizationDict.TryGetValue(sNamespace, out var dDict) &&
                dDict.TryGetValue(sKey, out var dRet))
            {
                return dRet;
            }

            if (_fortniteLocalizationDict.Count > 0 &&
                _fortniteLocalizationDict.TryGetValue(sNamespace, out var dict) &&
                dict.TryGetValue(sKey, out var ret))
            {
                return ret;
            }

            return defaultText;
        }

        private static string GetLanguageCode() => GetLanguageCode((ELanguage)Properties.Settings.Default.AssetsLanguage);
        private static string GetLanguageCode(ELanguage lang)
        {
            if (Globals.Game.ActualGame == EGame.Fortnite)
                return lang switch
                {
                    ELanguage.English => "en",
                    ELanguage.French => "fr",
                    ELanguage.German => "de",
                    ELanguage.Italian => "it",
                    ELanguage.Spanish => "es",
                    ELanguage.SpanishLatin => "es-419",
                    ELanguage.Arabic => "ar",
                    ELanguage.Japanese => "ja",
                    ELanguage.Korean => "ko",
                    ELanguage.Polish => "pl",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru",
                    ELanguage.Turkish => "tr",
                    ELanguage.Chinese => "zh-CN",
                    ELanguage.TraditionalChinese => "zh-Hant",
                    _ => "en",
                };
            else if (Globals.Game.ActualGame == EGame.Valorant)
                return lang switch
                {
                    //Indonesian id-ID
                    //Mexican Spanish es-MX
                    //Thailand th-TH
                    //Vietnam vi-VN
                    ELanguage.English => "en-US",
                    ELanguage.French => "fr-FR",
                    ELanguage.German => "de-DE",
                    ELanguage.Italian => "it-IT",
                    ELanguage.Spanish => "es-ES",
                    ELanguage.SpanishLatin => "es-419",
                    ELanguage.Arabic => "ar-AE",
                    ELanguage.Japanese => "ja-JP",
                    ELanguage.Korean => "ko-KR",
                    ELanguage.Polish => "pl-PL",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru-RU",
                    ELanguage.Turkish => "tr-TR",
                    ELanguage.Chinese => "zh-CN",
                    ELanguage.TraditionalChinese => "zh-TW",
                    _ => "en",
                };
            else if (Globals.Game.ActualGame == EGame.DeadByDaylight)
                return lang switch
                {
                    //Thailand th
                    ELanguage.English => "en",
                    ELanguage.French => "fr",
                    ELanguage.German => "de",
                    ELanguage.Italian => "it",
                    ELanguage.Spanish => "es",
                    ELanguage.SpanishLatin => "es-MX",
                    ELanguage.Arabic => "ar",
                    ELanguage.Japanese => "ja",
                    ELanguage.Korean => "ko",
                    ELanguage.Polish => "pl",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru",
                    ELanguage.Turkish => "tr",
                    ELanguage.Chinese => "zh-Hans",
                    ELanguage.TraditionalChinese => "zh-Hant",
                    _ => "en",
                };
            else if (Globals.Game.ActualGame == EGame.MinecraftDungeons)
                return lang switch
                {
                    //Swedish sv-SE
                    //Mexican Spanish es-MX
                    //Portugal Portuguese pt-PT
                    //British English en-GB
                    ELanguage.English => "en",
                    ELanguage.French => "fr-FR",
                    ELanguage.German => "de-DE",
                    ELanguage.Italian => "it-IT",
                    ELanguage.Spanish => "es-ES",
                    ELanguage.Japanese => "ja-JP",
                    ELanguage.Korean => "ko-KR",
                    ELanguage.Polish => "pl-PL",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Russian => "ru-RU",
                    _ => "en"
                };
            else if (Globals.Game.ActualGame == EGame.BattleBreakers)
                return lang switch
                {
                    ELanguage.English => "en",
                    ELanguage.Russian => "ru",
                    ELanguage.French => "fr",
                    ELanguage.Spanish => "es",
                    ELanguage.Italian => "it",
                    ELanguage.Japanese => "ja",
                    ELanguage.Korean => "ko",
                    ELanguage.PortugueseBrazil => "pt-BR",
                    ELanguage.Chinese => "zh-Hans",
                    _ => "en"
                };
            else
                return "en";
        }
    }
}
