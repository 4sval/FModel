using System;
using System.IO;
using System.Windows;
using CUE4Parse.UE4.Versions;
using FModel.Settings;
using FModel.ViewModels;
using SkiaSharp;

namespace FModel.Creator
{
    public class Typefaces
    {
        private readonly Uri _BURBANK_BIG_CONDENSED_BOLD = new Uri("pack://application:,,,/Resources/BurbankBigCondensed-Bold.ttf");
        private const string _EXT = ".ufont";

        // FortniteGame
        private const string _FORTNITE_BASE_PATH = "/Game/UI/Foundation/Fonts/";
        private const string _ASIA_ERINM = "AsiaERINM"; // korean fortnite
        private const string _BURBANK_BIG_CONDENSED_BLACK = "BurbankBigCondensed-Black"; // russian
        private const string _BURBANK_BIG_REGULAR_BLACK = "BurbankBigRegular-Black";
        private const string _BURBANK_BIG_REGULAR_BOLD = "BurbankBigRegular-Bold"; // official fortnite ig
        private const string _BURBANK_SMALL_MEDIUM = "BurbankSmall-Medium";
        private const string _DROID_SANS_FORTNITE_SUBSET = "DroidSans-Fortnite-Subset";
        private const string _NIS_JYAU = "NIS_JYAU"; // japanese fortnite
        private const string _NOTO_COLOR_EMOJI = "NotoColorEmoji";
        private const string _NOTO_SANS_BOLD = "NotoSans-Bold";
        private const string _NOTO_SANS_FORTNITE_BOLD = "NotoSans-Fortnite-Bold";
        private const string _NOTO_SANS_FORTNITE_ITALIC = "NotoSans-Fortnite-Italic";
        private const string _NOTO_SANS_FORTNITE_REGULAR = "NotoSans-Fortnite-Regular";
        private const string _NOTO_SANS_ITALIC = "NotoSans-Italic";
        private const string _NOTO_SANS_REGULAR = "NotoSans-Regular";
        private const string _NOTO_SANS_ARABIC_BLACK = "NotoSansArabic-Black"; // arabic fortnite
        private const string _NOTO_SANS_ARABIC_BOLD = "NotoSansArabic-Bold";
        private const string _NOTO_SANS_ARABIC_REGULAR = "NotoSansArabic-Regular";
        private const string _NOTO_SANS_JP_BOLD = "NotoSansJP-Bold";
        private const string _NOTO_SANS_KR_REGULAR = "NotoSansKR-Regular";
        private const string _NOTO_SANS_SC_BLACK = "NotoSansSC-Black"; // simplified chinese fortnite
        private const string _NOTO_SANS_SC_REGULAR = "NotoSansSC-Regular";
        private const string _NOTO_SANS_TC_BLACK = "NotoSansTC-Black"; // traditional chinese fortnite
        private const string _NOTO_SANS_TC_REGULAR = "NotoSansTC-Regular";
        private const string _BURBANK_SMALL_BLACK = "burbanksmall-black";
        private const string _BURBANK_SMALL_BOLD = "burbanksmall-bold";

        // WorldExplorers
        private const string _BATTLE_BREAKERS_BASE_PATH = "/Game/UMG/Fonts/Faces/";
        private const string _HEMIHEAD426 = "HemiHead426";
        private const string _NOTO_SANS_JP_REGULAR = "NotoSansJP-Regular";
        private const string _LATO_BLACK = "Lato-Black";
        private const string _LATO_BLACK_ITALIC = "Lato-BlackItalic";
        private const string _LATO_LIGHT = "Lato-Light";
        private const string _LATO_MEDIUM = "Lato-Medium";

        private readonly CUE4ParseViewModel _viewModel;

        public readonly SKTypeface Default; // used as a fallback font for all untranslated strings (item source, ...)
        public readonly SKTypeface DisplayName;
        public readonly SKTypeface Description;
        public readonly SKTypeface Bottom; // must be null for non-latin base languages
        public readonly SKTypeface Bundle;
        public readonly SKTypeface BundleNumber;

        public Typefaces(CUE4ParseViewModel viewModel)
        {
            byte[] data;
            _viewModel = viewModel;
            var language = UserSettings.Default.AssetLanguage;
            Default = SKTypeface.FromStream(Application.GetResourceStream(_BURBANK_BIG_CONDENSED_BOLD)?.Stream);

            switch (viewModel.Game)
            {
                case FGame.FortniteGame:
                {
                    var namePath = _FORTNITE_BASE_PATH +
                                   language switch
                                   {
                                       ELanguage.Korean => _ASIA_ERINM,
                                       ELanguage.Russian => _BURBANK_BIG_CONDENSED_BLACK,
                                       ELanguage.Japanese => _NIS_JYAU,
                                       ELanguage.Arabic => _NOTO_SANS_ARABIC_BLACK,
                                       ELanguage.TraditionalChinese => _NOTO_SANS_TC_BLACK,
                                       ELanguage.Chinese => _NOTO_SANS_SC_BLACK,
                                       _ => string.Empty
                                   };
                    if (viewModel.Provider.TrySaveAsset(namePath + _EXT, out data))
                    {
                        var m = new MemoryStream(data) {Position = 0};
                        DisplayName = SKTypeface.FromStream(m);
                    }
                    else DisplayName = Default;


                    var descriptionPath = _FORTNITE_BASE_PATH +
                                          language switch
                                          {
                                              ELanguage.Korean => _NOTO_SANS_KR_REGULAR,
                                              ELanguage.Japanese => _NOTO_SANS_JP_BOLD,
                                              ELanguage.Arabic => _NOTO_SANS_ARABIC_REGULAR,
                                              ELanguage.TraditionalChinese => _NOTO_SANS_TC_REGULAR,
                                              ELanguage.Chinese => _NOTO_SANS_SC_REGULAR,
                                              _ => _NOTO_SANS_REGULAR
                                          };
                    if (viewModel.Provider.TrySaveAsset(descriptionPath + _EXT, out data))
                    {
                        var m = new MemoryStream(data) {Position = 0};
                        Description = SKTypeface.FromStream(m);
                    }
                    else Description = Default;


                    var bottomPath = _FORTNITE_BASE_PATH +
                                     language switch
                                     {
                                         ELanguage.Korean => string.Empty,
                                         ELanguage.Japanese => string.Empty,
                                         ELanguage.Arabic => string.Empty,
                                         ELanguage.TraditionalChinese => string.Empty,
                                         ELanguage.Chinese => string.Empty,
                                         _ => _BURBANK_SMALL_BOLD
                                     };
                    if (viewModel.Provider.TrySaveAsset(bottomPath + _EXT, out data))
                    {
                        var m = new MemoryStream(data) {Position = 0};
                        Bottom = SKTypeface.FromStream(m);
                    }
                    // else keep it null


                    if (viewModel.Provider.TrySaveAsset(_FORTNITE_BASE_PATH + _BURBANK_BIG_CONDENSED_BLACK + _EXT, out data))
                    {
                        var m = new MemoryStream(data) {Position = 0};
                        BundleNumber = SKTypeface.FromStream(m);
                    }
                    else BundleNumber = Default;


                    var bundleNamePath = _FORTNITE_BASE_PATH +
                                         language switch
                                         {
                                             ELanguage.Korean => _ASIA_ERINM,
                                             ELanguage.Russian => _BURBANK_BIG_CONDENSED_BLACK,
                                             ELanguage.Japanese => _NIS_JYAU,
                                             ELanguage.Arabic => _NOTO_SANS_ARABIC_BLACK,
                                             ELanguage.TraditionalChinese => _NOTO_SANS_TC_BLACK,
                                             ELanguage.Chinese => _NOTO_SANS_SC_BLACK,
                                             _ => string.Empty
                                         };
                    if (viewModel.Provider.TrySaveAsset(bundleNamePath + _EXT, out data))
                    {
                        var m = new MemoryStream(data) {Position = 0};
                        Bundle = SKTypeface.FromStream(m);
                    }
                    else Bundle = BundleNumber;

                    break;
                }
                case FGame.WorldExplorers:
                {
                    var namePath = _BATTLE_BREAKERS_BASE_PATH +
                                   language switch
                                   {
                                       ELanguage.Korean => _NOTO_SANS_KR_REGULAR,
                                       ELanguage.Russian => _LATO_BLACK,
                                       ELanguage.Japanese => _NOTO_SANS_JP_REGULAR,
                                       ELanguage.Chinese => _NOTO_SANS_SC_REGULAR,
                                       _ => _HEMIHEAD426
                                   };
                    if (viewModel.Provider.TrySaveAsset(namePath + _EXT, out data))
                    {
                        var m = new MemoryStream(data) {Position = 0};
                        DisplayName = SKTypeface.FromStream(m);
                    }
                    else DisplayName = Default;

                    var descriptionPath = _BATTLE_BREAKERS_BASE_PATH +
                                          language switch
                                          {
                                              ELanguage.Korean => _NOTO_SANS_KR_REGULAR,
                                              ELanguage.Russian => _LATO_BLACK,
                                              ELanguage.Japanese => _NOTO_SANS_JP_REGULAR,
                                              ELanguage.Chinese => _NOTO_SANS_SC_REGULAR,
                                              _ => _HEMIHEAD426
                                          };
                    if (viewModel.Provider.TrySaveAsset(descriptionPath + _EXT, out data))
                    {
                        var m = new MemoryStream(data) {Position = 0};
                        Description = SKTypeface.FromStream(m);
                    }
                    else Description = Default;

                    break;
                }
                case FGame.ShooterGame:
                    break;
                case FGame.DeadByDaylight:
                    break;
                case FGame.OakGame:
                    break;
                case FGame.Dungeons:
                    break;
                case FGame.g3:
                    break;
                case FGame.StateOfDecay2:
                    break;
                case FGame.Prospect:
                    break;
                case FGame.Indiana:
                    break;
                case FGame.RogueCompany:
                    break;
                case FGame.SwGame:
                    break;
                case FGame.Platform:
                    break;
            }
        }

        public SKTypeface OnTheFly(string path)
        {
            if (!_viewModel.Provider.TrySaveAsset(path, out var data)) return Default;
            var m = new MemoryStream(data) {Position = 0};
            return SKTypeface.FromStream(m);
        }
    }
}