using FModel.Methods.MessageBox;
using System.Windows;
using System;
using FProp = FModel.Properties.Settings;
using System.Windows.Controls;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Windows.Media;
using FModel.Methods.Assets;
using FModel.Methods;

namespace FModel.Forms
{
    /// <summary>
    /// Logique d'interaction pour FModel_Settings.xaml
    /// </summary>
    public partial class FModel_Settings : Window
    {
        public FModel_Settings()
        {
            InitializeComponent();
            this.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
        }

        #region ENUMS
        enum LIndexes
        {
            [Description("English")]
            English = 0,
            [Description("French")]
            French = 1,
            [Description("German")]
            German = 2,
            [Description("Italian")]
            Italian = 3,
            [Description("Spanish")]
            Spanish = 4,
            [Description("Spanish (LA)")]
            Spanish_LA = 5,
            [Description("Arabic")]
            Arabic = 6,
            [Description("Japanese")]
            Japanese = 7,
            [Description("Korean")]
            Korean = 8,
            [Description("Polish")]
            Polish = 9,
            [Description("Portuguese (Brazil)")]
            Portuguese_Brazil = 10,
            [Description("Russian")]
            Russian = 11,
            [Description("Turkish")]
            Turkish = 12,
            [Description("Chinese (S)")]
            Chinese_S = 13,
            [Description("Traditional Chinese")]
            Traditional_Chinese = 14
        }

        enum RIndexes
        {
            [Description("Default")]
            Default = 0,
            [Description("Flat")]
            Flat = 1,
            [Description("Minimalist")]
            Minimalist = 2
        }

        public static T GetEnumValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) { throw new ArgumentException(); }
            FieldInfo[] fields = type.GetFields();
            var field = fields
                            .SelectMany(f => f.GetCustomAttributes(
                                typeof(DescriptionAttribute), false), (
                                    f, a) => new { Field = f, Att = a })
                            .Where(a => ((DescriptionAttribute)a.Att)
                                .Description == description).SingleOrDefault();
            return field == null ? default(T) : (T)field.Field.GetRawConstantValue();
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetUserSettings();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetUserSettings();
            Close();
        }

        private void GetUserSettings()
        {
            InputTextBox.Text = FProp.Default.FPak_Path;    
            OutputTextBox.Text = FProp.Default.FOutput_Path;

            ComboBox_Language.SelectedIndex = (int)GetEnumValueFromDescription<LIndexes>(FProp.Default.FLanguage);
            ComboBox_Design.SelectedIndex = (int)GetEnumValueFromDescription<RIndexes>(FProp.Default.FRarity_Design);
        }

        private void SetUserSettings()
        {
            if (!string.Equals(FProp.Default.FPak_Path, InputTextBox.Text))
            {
                FProp.Default.FPak_Path = InputTextBox.Text;
                DarkMessageBox.Show("Please, restart FModel to apply your new input path", "FModel Input Path Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (!string.Equals(FProp.Default.FOutput_Path, OutputTextBox.Text))
            {
                FProp.Default.FOutput_Path = OutputTextBox.Text;
                DarkMessageBox.Show("Please, restart FModel to apply your new output path", "FModel Output Path Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (AssetEntries.AssetEntriesDict != null && !string.Equals(FProp.Default.FLanguage, ((ComboBoxItem)ComboBox_Language.SelectedItem).Content.ToString()))
            {
                AssetTranslations.SetAssetTranslation(((ComboBoxItem)ComboBox_Language.SelectedItem).Content.ToString());
            }
            FProp.Default.FLanguage = ((ComboBoxItem)ComboBox_Language.SelectedItem).Content.ToString();

            FProp.Default.FRarity_Design = ((ComboBoxItem)ComboBox_Design.SelectedItem).Content.ToString();

            FProp.Default.Save();
        }
    }
}
