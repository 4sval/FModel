using FModel.Methods;
using FModel.Methods.AESManager;
using FModel.Methods.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private static readonly string AESManager_PATH = FProp.Default.FOutput_Path + "\\FAESManager.xml";

        public AESManager()
        {
            InitializeComponent();
            this.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddLblTxtForDynamicPAKs();
            GetUserSettings();
            if (string.IsNullOrEmpty(FProp.Default.FPak_MainAES))
            {
                await SetMainKey();
            }
        }

        /// <summary>
        /// Fetch latest version's main aes key.
        /// </summary>
        /// <returns></returns>
        private async Task SetMainKey()
        {
            dynamic key = null;
            if (DLLImport.IsInternetAvailable())
            {
                try
                {
                    using (HttpClient web = new HttpClient())
                    {
                        //Not using BenBot api since Rate Limit
                        key = JsonConvert.DeserializeObject(await web.GetStringAsync("https://fnbot.shop/api/aes.json"));
                    }
                    MAesTextBox.Text = $"0x{key.aes}";
                    FProp.Default.FPak_MainAES = $"{key.aes}";
                }
                catch (Exception)
                {
                    new UpdateMyConsole("There was a problem getting the latest aes key for main pak files.", CColors.Blue, true).Append();
                }
               
            }
            else
            {
                new UpdateMyConsole("Your internet connection is currently unavailable, can't check for dynamic keys at the moment.", CColors.Blue, true).Append();
                
            }
           
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

                Directory.CreateDirectory(Path.GetDirectoryName(AESManager_PATH));
                using (var fileStream = new FileStream(AESManager_PATH, FileMode.Create))
                {
                    KeysManager.serializer.Serialize(fileStream, AESEntries.AESEntriesList);
                }
            }

            //SAVE
            FProp.Default.Save();
        }

        
    }
}
