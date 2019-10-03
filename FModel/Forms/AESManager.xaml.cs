using FModel.Methods;
using FModel.Methods.AESManager;
using FModel.Methods.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using FProp = FModel.Properties.Settings;

namespace FModel.Forms
{
    /// <summary>
    /// Logique d'interaction pour AESManager.xaml
    /// </summary>
    public partial class AESManager : Window
    {
        public AESManager()
        {
            InitializeComponent();
            this.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddLblTxtForDynamicPAKs();
            GetUserSettings();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetUserSettings();
            PAKsUtility.DisableNonKeyedPAKs();
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void AddLblTxtForDynamicPAKs()
        {
            if (PAKEntries.PAKEntriesList != null && PAKEntries.PAKEntriesList.Any())
            {
                if (AESEntries.AESEntriesList == null) { KeysManager.Deserialize(); }
                int yPos = 4;

                foreach (PAKInfosEntry Pak in PAKEntries.PAKEntriesList.Where(x => x.bTheDynamicPAK == true))
                {
                    Label PakLabel = new Label();
                    PakLabel.Content = Path.GetFileNameWithoutExtension(Pak.ThePAKPath);
                    PakLabel.HorizontalAlignment = HorizontalAlignment.Left;
                    PakLabel.Margin = new Thickness(2, yPos - 2, 0, 0);
                    PakLabel.VerticalAlignment = VerticalAlignment.Top;
                    PakLabel.Foreground = new SolidColorBrush(Color.FromRgb(239, 239, 239));

                    TextBox PakTextBox = new TextBox();
                    PakTextBox.Height = 19;
                    PakTextBox.TextWrapping = TextWrapping.NoWrap;
                    PakTextBox.AcceptsReturn = false;
                    PakTextBox.Margin = new Thickness(160, yPos, 5, 0);
                    PakTextBox.VerticalAlignment = VerticalAlignment.Top;
                    PakTextBox.Foreground = new SolidColorBrush(Color.FromRgb(239, 239, 239));
                    PakTextBox.Name = $"TxtBox_{Regex.Match(Path.GetFileNameWithoutExtension(Pak.ThePAKPath), @"\d+").Value}";

                    if (AESEntries.AESEntriesList != null && AESEntries.AESEntriesList.Any())
                    {
                        string PAKKeyFromXML = AESEntries.AESEntriesList.Where(x => string.Equals(x.ThePAKName, Path.GetFileNameWithoutExtension(Pak.ThePAKPath))).Select(x => x.ThePAKKey).FirstOrDefault();
                        PakTextBox.Text = $"0x{PAKKeyFromXML}";
                    }

                    yPos += 28;
                    Grid_DynamicKeys.Children.Add(PakLabel);
                    Grid_DynamicKeys.Children.Add(PakTextBox);
                }
            }
        }

        private void GetUserSettings()
        {
            MAesTextBox.Text = $"0x{FProp.Default.FPak_MainAES}";
        }

        private void SetUserSettings()
        {
            //MAIN AES
            if (!string.IsNullOrEmpty(MAesTextBox.Text))
            {
                if (MAesTextBox.Text.StartsWith("0x"))
                {
                    FProp.Default.FPak_MainAES = Regex.Replace(MAesTextBox.Text.Substring(2).ToUpper(), @"\s+", string.Empty);
                }
                else { FProp.Default.FPak_MainAES = Regex.Replace(MAesTextBox.Text.ToUpper(), @"\s+", string.Empty); }
            }
            else { FProp.Default.FPak_MainAES = string.Empty; }

            //DYNAMIC AESs
            AESEntries.AESEntriesList = new List<AESInfosEntry>();
            if (PAKEntries.PAKEntriesList != null && PAKEntries.PAKEntriesList.Any())
            {
                foreach (PAKInfosEntry Pak in PAKEntries.PAKEntriesList.Where(x => x.bTheDynamicPAK == true))
                {
                    TextBox PakTextBox = UIHelper.FindChild<TextBox>(this, $"TxtBox_{Regex.Match(Path.GetFileNameWithoutExtension(Pak.ThePAKPath), @"\d+").Value}");
                    if (!string.IsNullOrEmpty(PakTextBox.Text))
                    {
                        if (PakTextBox.Text.StartsWith("0x"))
                        {
                            KeysManager.Serialize(Path.GetFileNameWithoutExtension(Pak.ThePAKPath), Regex.Replace(PakTextBox.Text.Substring(2).ToUpper(), @"\s+", string.Empty));
                        }
                        else { KeysManager.Serialize(Path.GetFileNameWithoutExtension(Pak.ThePAKPath), Regex.Replace(PakTextBox.Text.ToUpper(), @"\s+", string.Empty)); }
                    }
                    else { KeysManager.Serialize(Path.GetFileNameWithoutExtension(Pak.ThePAKPath), string.Empty); }
                }
            }

            //SAVE
            FProp.Default.Save();
        }
    }
}
