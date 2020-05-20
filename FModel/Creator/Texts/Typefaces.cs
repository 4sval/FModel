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
        private const string _BASE_PATH = "/Game/UI/Foundation/Fonts/";
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
#pragma warning restore IDE0051

        public SKTypeface DefaultTypeface; // used as default font for all untranslated strings (item source, ...)
        public SKTypeface BundleDefaultTypeface; // used for the last folder string
        public SKTypeface DisplayNameTypeface;
        public SKTypeface DescriptionTypeface;
        public SKTypeface BundleDisplayNameTypeface;

        public Typefaces()
        {
            DefaultTypeface = SKTypeface.FromStream(Application.GetResourceStream(_BURBANK_BIG_CONDENSED_BOLD).Stream);

            ArraySegment<byte>[] t = Utils.GetPropertyArraySegmentByte(_BASE_PATH + _BURBANK_BIG_CONDENSED_BLACK);
            if (t != null && t.Length == 3)
                BundleDefaultTypeface = SKTypeface.FromStream(t[2].AsStream());
            else BundleDefaultTypeface = DefaultTypeface;

            string namePath = _BASE_PATH + (
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Korean ? _ASIA_ERINM :
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Russian ? _BURBANK_BIG_CONDENSED_BLACK :
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Japanese ? _NIS_JYAU :
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Arabic ? _NOTO_SANS_ARABIC_BLACK :
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.TraditionalChinese ? _NOTO_SANS_TC_BLACK :
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Chinese ? _NOTO_SANS_SC_BLACK :
                string.Empty);
            if (!namePath.Equals(_BASE_PATH))
            {
                t = Utils.GetPropertyArraySegmentByte(namePath);
                if (t != null && t.Length == 3)
                    DisplayNameTypeface = SKTypeface.FromStream(t[2].AsStream());
            }
            else DisplayNameTypeface = DefaultTypeface;

            string descriptionPath = _BASE_PATH + (
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

            string bundleNamePath = _BASE_PATH + (
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Korean ? _ASIA_ERINM :
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Russian ? _BURBANK_BIG_CONDENSED_BLACK :
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Japanese ? _NIS_JYAU :
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Arabic ? _NOTO_SANS_ARABIC_BLACK :
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.TraditionalChinese ? _NOTO_SANS_TC_BLACK :
                Properties.Settings.Default.AssetsLanguage == (long)ELanguage.Chinese ? _NOTO_SANS_SC_BLACK :
                string.Empty);
            if (!bundleNamePath.Equals(_BASE_PATH))
            {
                t = Utils.GetPropertyArraySegmentByte(bundleNamePath);
                if (t != null && t.Length == 3)
                    BundleDisplayNameTypeface = SKTypeface.FromStream(t[2].AsStream());
            }
            else BundleDisplayNameTypeface = BundleDefaultTypeface;
        }

        public bool NeedReload(bool forceReload) => forceReload ?
            DataGridVm.dataGridViewModel.Count > 0 : //reload only if at least one pak is loaded
            DataGridVm.dataGridViewModel.Count > 0 && (BundleDefaultTypeface == DefaultTypeface && DisplayNameTypeface == DefaultTypeface && DescriptionTypeface == DefaultTypeface && BundleDisplayNameTypeface == BundleDefaultTypeface);
    }
}
