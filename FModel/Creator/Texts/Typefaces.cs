using FModel.Utils;
using FModel.ViewModels.DataGrid;
using SkiaSharp;
using System;
using System.Windows;

namespace FModel.Creator.Texts
{
    public class Typefaces
    {
#pragma warning disable IDE0051
        private const string _FORTNITE_BASE_PATH = "/Game/UI/Foundation/Fonts/";
        private const string _ASIA_ERINM = "AsiaERINM"; // korean fortnite
        private const string _BURBANK_BIG_CONDENSED_BLACK = "BurbankBigCondensed-Black"; // russian
        private readonly Uri _BURBANK_BIG_CONDENSED_BOLD = new Uri("pack://application:,,,/Resources/BurbankBigCondensed-Bold.ttf"); // other languages fortnite unofficial
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

        private const string _VALORANT_BASE_PATH = "/Game/";
        private const string _TUNGSTEN_BOLD = "Tungsten-Bold";
        private const string _DINNEXT_LTARABIC_HEAVY = "UI/Fonts/FinalFonts/LOCFonts/DIN_Next_Arabic/DINNextLTArabic-Heavy";
        private const string _TUNGSTEN_CYRILLIC = "UI/Fonts/FinalFonts/LOCFonts/LOC_Tungsten/Cyrillic_Tungsten";
        private const string _TUNGSTEN_JAPANESE = "UI/Fonts/FinalFonts/LOCFonts/LOC_Tungsten/JP_Tungsten";
        private const string _TUNGSTEN_KOREAN = "UI/Fonts/FinalFonts/LOCFonts/LOC_Tungsten/KR_Tungsten";
        private const string _TUNGSTEN_SIMPLIFIED_CHINESE = "UI/Fonts/FinalFonts/LOCFonts/LOC_Tungsten/zh-CN_Tungsten";
        private const string _TUNGSTEN_TRADITIONAL_CHINESE = "UI/Fonts/FinalFonts/LOCFonts/LOC_Tungsten/zh-TW_Tungsten";
        private const string _DINNEXT_W1G_LIGHT = "UI/Fonts/FinalFonts/DINNextW1G-Light";
        private const string _DINNEXT_LTARABIC_LIGHT = "UI/Fonts/FinalFonts/LOCFonts/DIN_Next_Arabic/DINNextLTArabic-Light";
        private const string _NOTOSANS_CJK_LIGHT = "UI/Fonts/FinalFonts/LOCFonts/CJK/NotoSansCJK-Light"; // chinese, japanese, korean
        private const string _DINNEXT_W1G_BOLD = "UI/Fonts/FinalFonts/DINNextW1G-Bold";
#pragma warning restore IDE0051

        public SKTypeface DefaultTypeface; // used as default font for all untranslated strings (item source, ...)
        public SKTypeface BundleDefaultTypeface; // used for the last folder string
        public SKTypeface DisplayNameTypeface;
        public SKTypeface DescriptionTypeface;
        public SKTypeface BundleDisplayNameTypeface;

        public Typefaces()
        {
            DefaultTypeface = SKTypeface.FromStream(Application.GetResourceStream(_BURBANK_BIG_CONDENSED_BOLD).Stream);
            if (Globals.Game.ActualGame == EGame.Fortnite)
            {
                ArraySegment<byte>[] t = Utils.GetPropertyArraySegmentByte(_FORTNITE_BASE_PATH + _BURBANK_BIG_CONDENSED_BLACK);
                if (t != null && t.Length == 3)
                    BundleDefaultTypeface = SKTypeface.FromStream(t[2].AsStream());
                else BundleDefaultTypeface = DefaultTypeface;

                string namePath = _FORTNITE_BASE_PATH + (
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Korean ? _ASIA_ERINM :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Russian ? _BURBANK_BIG_CONDENSED_BLACK :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Japanese ? _NIS_JYAU :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Arabic ? _NOTO_SANS_ARABIC_BLACK :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.TraditionalChinese ? _NOTO_SANS_TC_BLACK :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Chinese ? _NOTO_SANS_SC_BLACK :
                    string.Empty);
                if (!namePath.Equals(_FORTNITE_BASE_PATH))
                {
                    t = Utils.GetPropertyArraySegmentByte(namePath);
                    if (t != null && t.Length == 3)
                        DisplayNameTypeface = SKTypeface.FromStream(t[2].AsStream());
                }
                else DisplayNameTypeface = DefaultTypeface;

                string descriptionPath = _FORTNITE_BASE_PATH + (
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Korean ? _NOTO_SANS_KR_REGULAR :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Japanese ? _NOTO_SANS_JP_BOLD :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Arabic ? _NOTO_SANS_ARABIC_REGULAR :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.TraditionalChinese ? _NOTO_SANS_TC_REGULAR :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Chinese ? _NOTO_SANS_SC_REGULAR :
                    _NOTO_SANS_REGULAR);
                t = Utils.GetPropertyArraySegmentByte(descriptionPath);
                if (t != null && t.Length == 3)
                    DescriptionTypeface = SKTypeface.FromStream(t[2].AsStream());
                else DescriptionTypeface = DefaultTypeface;

                string bundleNamePath = _FORTNITE_BASE_PATH + (
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Korean ? _ASIA_ERINM :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Russian ? _BURBANK_BIG_CONDENSED_BLACK :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Japanese ? _NIS_JYAU :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Arabic ? _NOTO_SANS_ARABIC_BLACK :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.TraditionalChinese ? _NOTO_SANS_TC_BLACK :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Chinese ? _NOTO_SANS_SC_BLACK :
                    string.Empty);
                if (!bundleNamePath.Equals(_FORTNITE_BASE_PATH))
                {
                    t = Utils.GetPropertyArraySegmentByte(bundleNamePath);
                    if (t != null && t.Length == 3)
                        BundleDisplayNameTypeface = SKTypeface.FromStream(t[2].AsStream());
                }
                else BundleDisplayNameTypeface = BundleDefaultTypeface;
            }
            else if (Globals.Game.ActualGame == EGame.Valorant)
            {
                string namePath = _VALORANT_BASE_PATH + (
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Korean ? _TUNGSTEN_KOREAN :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Russian ? _TUNGSTEN_CYRILLIC :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Japanese ? _TUNGSTEN_JAPANESE :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Arabic ? _DINNEXT_LTARABIC_HEAVY :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.TraditionalChinese ? _TUNGSTEN_TRADITIONAL_CHINESE :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Chinese ? _TUNGSTEN_SIMPLIFIED_CHINESE :
                    _TUNGSTEN_BOLD);
                ArraySegment<byte>[] t = Utils.GetPropertyArraySegmentByte(namePath);
                if (t != null && t.Length == 3)
                    DisplayNameTypeface = SKTypeface.FromStream(t[2].AsStream());
                else DisplayNameTypeface = DefaultTypeface;

                string descriptionPath = _VALORANT_BASE_PATH + (
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Korean ? _NOTOSANS_CJK_LIGHT :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Japanese ? _NOTOSANS_CJK_LIGHT :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Arabic ? _DINNEXT_LTARABIC_LIGHT :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.TraditionalChinese ? _NOTOSANS_CJK_LIGHT :
                    Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Chinese ? _NOTOSANS_CJK_LIGHT :
                    _DINNEXT_W1G_LIGHT);
                t = Utils.GetPropertyArraySegmentByte(descriptionPath);
                if (t != null && t.Length == 3)
                    DescriptionTypeface = SKTypeface.FromStream(t[2].AsStream());
                else DescriptionTypeface = DefaultTypeface;

                t = Utils.GetPropertyArraySegmentByte(_VALORANT_BASE_PATH + _DINNEXT_W1G_BOLD);
                if (t != null && t.Length == 3)
                    BundleDefaultTypeface = SKTypeface.FromStream(t[2].AsStream());
                else BundleDefaultTypeface = DefaultTypeface;
            }
        }

        public bool NeedReload(bool forceReload) => forceReload ?
            DataGridVm.dataGridViewModel.Count > 0 : //reload only if at least one pak is loaded
            DataGridVm.dataGridViewModel.Count > 0 && (BundleDefaultTypeface == DefaultTypeface && DisplayNameTypeface == DefaultTypeface && DescriptionTypeface == DefaultTypeface && BundleDisplayNameTypeface == BundleDefaultTypeface);
    }
}
