using FModel.Methods;
using FModel.Methods.Assets;
using FModel.Methods.MessageBox;
using FModel.Methods.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FProp = FModel.Properties.Settings;

namespace FModel.Forms
{
    /// <summary>
    /// Logique d'interaction pour FModel_UpdateMode.xaml
    /// </summary>
    public partial class FModel_UpdateMode : Window
    {
        private const string CHALLENGE_TEMPLATE_ICON = "pack://application:,,,/Resources/Template_Challenge.png";
        private const string RARITY_DEFAULT_FEATURED = "pack://application:,,,/Resources/Template_D_F.png";
        private const string RARITY_DEFAULT_NORMAL = "pack://application:,,,/Resources/Template_D_N.png";
        private const string RARITY_FLAT_FEATURED = "pack://application:,,,/Resources/Template_F_F.png";
        private const string RARITY_FLAT_NORMAL = "pack://application:,,,/Resources/Template_F_N.png";
        private const string RARITY_MINIMALIST_FEATURED = "pack://application:,,,/Resources/Template_M_F.png";
        private const string RARITY_MINIMALIST_NORMAL = "pack://application:,,,/Resources/Template_M_N.png";
        private const string RARITY_ACCURATECOLORS_FEATURED = "pack://application:,,,/Resources/Template_AC_F.png";
        private const string RARITY_ACCURATECOLORS_NORMAL = "pack://application:,,,/Resources/Template_AC_N.png";

        #region CLASS
        public class AssetProperties : INotifyPropertyChanged
        {
            public string Name { get; set; }
            public string Path { get; set; }

            //Provide change-notification for IsChecked
            [JsonIgnore]
            private bool _fIsChecked = false;
            public bool IsChecked
            {
                get { return _fIsChecked; }
                set
                {
                    _fIsChecked = value;
                    this.OnPropertyChanged("IsChecked");
                }
            }
            //Provide change-notification for IsSelected
            [JsonIgnore]
            private bool _fIsSelected = false;
            public bool IsSelected
            {
                get { return _fIsSelected; }
                set
                {
                    _fIsSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string strPropertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(strPropertyName));
            }
        }

        #endregion

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
            Minimalist = 2,
            [Description("Accurate Colors")]
            Accurate = 3
        }

        public static T GetEnumValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) { throw new ArgumentException("Enum type is null, bruh"); }
            FieldInfo[] fields = type.GetFields();
            var field = fields
                            .SelectMany(f => f.GetCustomAttributes(
                                typeof(DescriptionAttribute), false), (
                                    f, a) => new { Field = f, Att = a })
                            .SingleOrDefault(a => ((DescriptionAttribute)a.Att)
                                .Description == description);
            return field == null ? default(T) : (T)field.Field.GetRawConstantValue();
        }
        #endregion

        public ObservableCollection<AssetProperties> Assets { get; set; }
        public static Dictionary<string, Dictionary<string, string>> AssetsEntriesDict { get; set; }

        public FModel_UpdateMode()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FoldersUtility.CheckWatermark();

            ComboBox_Language.SelectedIndex = (int)GetEnumValueFromDescription<LIndexes>(FProp.Default.FLanguage);
            ComboBox_Design.SelectedIndex = (int)GetEnumValueFromDescription<RIndexes>(FProp.Default.FRarity_Design);

            bFeaturedIcon.IsChecked = FProp.Default.FIsFeatured;
            bWatermarkIcon.IsChecked = FProp.Default.FUseWatermark;
            Watermark_Label.Content += Path.GetFileName(FProp.Default.FWatermarkFilePath);

            Opacity_Slider.Value = FProp.Default.FWatermarkOpacity;

            AssetsEntriesDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(FProp.Default.FUM_AssetsType);
            Assets = new ObservableCollection<AssetProperties>();
            foreach (KeyValuePair<string, Dictionary<string, string>> a in AssetsEntriesDict)
            {
                Assets.Add(new AssetProperties
                {
                    Name = a.Key,
                    Path = a.Value["Path"],
                    IsChecked = bool.Parse(a.Value["isChecked"])
                });
            }

            DataContext = this;
            await UpdateImageWithWatermark();
        }

        private async void UpdateImageBox(object sender, RoutedEventArgs e)
        {
            await UpdateImageWithWatermark();
        }
        private async void UpdateImageWithWatermark(object sender, RoutedEventArgs e)
        {
            await UpdateImageWithWatermark();
        }
        private async Task UpdateImageWithWatermark()
        {
            bool watermarkEnabled = (bool)bWatermarkIcon.IsChecked;
            bool isFeatured = (bool)bFeaturedIcon.IsChecked;
            string rarityDesign = ((ComboBoxItem)ComboBox_Design.SelectedItem).Content.ToString();
            int opacity = Convert.ToInt32(Opacity_Slider.Value);
            double scale = FProp.Default.FWatermarkScale;

            await Task.Run(() =>
            {
                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    //INITIALIZATION
                    drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Size(515, 515)));

                    BitmapImage source = null;
                    switch (rarityDesign)
                    {
                        case "Default":
                            source = new BitmapImage(new Uri(isFeatured ? RARITY_DEFAULT_FEATURED : RARITY_DEFAULT_NORMAL));
                            break;
                        case "Flat":
                            source = new BitmapImage(new Uri(isFeatured ? RARITY_FLAT_FEATURED : RARITY_FLAT_NORMAL));
                            break;
                        case "Minimalist":
                            source = new BitmapImage(new Uri(isFeatured ? RARITY_MINIMALIST_FEATURED : RARITY_MINIMALIST_NORMAL));
                            break;
                        case "Accurate Colors":
                            source = new BitmapImage(new Uri(isFeatured ? RARITY_ACCURATECOLORS_FEATURED : RARITY_ACCURATECOLORS_NORMAL));
                            break;
                    }
                    drawingContext.DrawImage(source, new Rect(new Point(0, 0), new Size(515, 515)));

                    if (!string.IsNullOrEmpty(FProp.Default.FWatermarkFilePath) && watermarkEnabled)
                    {
                        using (StreamReader image = new StreamReader(FProp.Default.FWatermarkFilePath))
                        {
                            BitmapImage bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = image.BaseStream;
                            bmp.EndInit();

                            drawingContext.DrawImage(ImagesUtility.CreateTransparency(bmp, opacity), new Rect(FProp.Default.FWatermarkXPos, FProp.Default.FWatermarkYPos, scale, scale));
                        }
                    }
                }

                RenderTargetBitmap RTB = new RenderTargetBitmap(515, 515, 96, 96, PixelFormats.Pbgra32);
                RTB.Render(drawingVisual);
                RTB.Freeze(); //We freeze to apply the RTB to our imagesource from the UI Thread

                FWindow.FMain.Dispatcher.InvokeAsync(() =>
                {
                    ImageBox_RarityPreview.Source = BitmapFrame.Create(RTB); //thread safe and fast af
                });

            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });
        }
        private async void EnableDisableWatermark(object sender, RoutedEventArgs e)
        {
            OpenFile_Button.IsEnabled = (bool)bWatermarkIcon.IsChecked;
            Opacity_Slider.IsEnabled = (bool)bWatermarkIcon.IsChecked;

            await UpdateImageWithWatermark();
        }
        private async void OpenFile_Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFiledialog = new Microsoft.Win32.OpenFileDialog();
            openFiledialog.Title = "Choose your watermark";
            openFiledialog.Multiselect = false;
            openFiledialog.Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*";
            if (openFiledialog.ShowDialog() == true)
            {
                Watermark_Label.Content = "File Name: " + Path.GetFileName(openFiledialog.FileName);
                FProp.Default.FWatermarkFilePath = openFiledialog.FileName;
                FProp.Default.Save();

                await UpdateImageWithWatermark();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AssetsEntriesDict = new Dictionary<string, Dictionary<string, string>>();
            foreach (AssetProperties a in Assets)
            {
                AssetsEntriesDict[a.Name] = new Dictionary<string, string>();
                AssetsEntriesDict[a.Name]["Path"] = a.Path;
                AssetsEntriesDict[a.Name]["isChecked"] = a.IsChecked.ToString();

                if (a.IsChecked)
                    DebugHelper.WriteLine("UpdateMode: User is about to extract everything in " + a.Path);
            }

            string selectedLang = ((ComboBoxItem)ComboBox_Language.SelectedItem).Content.ToString();
            if (AssetEntries.AssetEntriesDict != null && !string.Equals(FProp.Default.FLanguage, selectedLang))
            {
                AssetTranslations.SetAssetTranslation(selectedLang);
            }
            FProp.Default.FLanguage = selectedLang;

            FProp.Default.FRarity_Design = ((ComboBoxItem)ComboBox_Design.SelectedItem).Content.ToString();
            FProp.Default.FIsFeatured = (bool)bFeaturedIcon.IsChecked;
            FProp.Default.FUseWatermark = (bool)bWatermarkIcon.IsChecked;

            FProp.Default.FWatermarkOpacity = Convert.ToInt32(Opacity_Slider.Value);
            FProp.Default.FUM_AssetsType = JsonConvert.SerializeObject(AssetsEntriesDict, Formatting.Indented);
            FProp.Default.Save();

            DebugHelper.WriteLine("=============== UPDATE MODE ===============");
            DebugHelper.WriteLine("FRarity_Design > " + FProp.Default.FRarity_Design);
            DebugHelper.WriteLine("FLanguage > " + FProp.Default.FLanguage);
            DebugHelper.WriteLine("FIsFeatured > " + FProp.Default.FIsFeatured);
            DebugHelper.WriteLine("FUseWatermark > " + FProp.Default.FUseWatermark);
            DebugHelper.WriteLine("FWatermarkFilePath > " + FProp.Default.FWatermarkFilePath);
            DebugHelper.WriteLine("FWatermarkOpacity > " + FProp.Default.FWatermarkOpacity);
            DebugHelper.WriteLine("FWatermarkScale > " + FProp.Default.FWatermarkScale);
            DebugHelper.WriteLine("FWatermarkXPos > " + FProp.Default.FWatermarkXPos);
            DebugHelper.WriteLine("FWatermarkYPos > " + FProp.Default.FWatermarkYPos);
            DebugHelper.WriteLine("FOpenSounds > " + FProp.Default.FOpenSounds);
            DebugHelper.WriteLine("FAutoExtractRaw > " + FProp.Default.FAutoExtractRaw);
            DebugHelper.WriteLine("FAutoSaveJson > " + FProp.Default.FAutoSaveJson);
            DebugHelper.WriteLine("FAutoSaveImg > " + FProp.Default.FAutoSaveImg);
            DebugHelper.WriteLine("=============================================");

            Close();
        }

        private void AssetsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0) { ((ListBox)sender).ScrollIntoView(e.AddedItems[0]); }
            Button_RemoveAssetType.IsEnabled = AssetsListBox.SelectedIndex >= 0;
        }

        private void Button_RemoveAssetType_Click(object sender, RoutedEventArgs e)
        {
            if (AssetsListBox.Items.Count > 0 && AssetsListBox.SelectedItems.Count > 0)
            {
                DebugHelper.WriteLine("UpdateMode: Removing " + (AssetsListBox.SelectedItem as AssetProperties).Path + " to the list of assets type");
                Assets.Remove(AssetsListBox.SelectedItem as AssetProperties);
            }
        }

        private void Button_AddAssetType_Click(object sender, RoutedEventArgs e)
        {
            string path = PathTextBox.Text.Trim();
            if (!path.StartsWith("/"))
                path = path.Insert(0, "/");
            if (!path.EndsWith("/"))
                path += "/";

            DebugHelper.WriteLine("UpdateMode: Adding " + path + " to the list of assets type");
            Assets.Add(new AssetProperties
            {
                Name = NameTextBox.Text,
                Path = path,
                IsSelected = true
            });
        }

        private void RC_Properties_Click(object sender, RoutedEventArgs e)
        {
            if (AssetsListBox.SelectedIndex >= 0)
            {
                AssetProperties a = AssetsListBox.SelectedItem as AssetProperties;
                string infos = GetAssetInfos(a);
                if (DarkMessageBox.ShowYesNo(infos, a.Name, "Copy Properties", "OK") == MessageBoxResult.Yes)
                {
                    Clipboard.SetText(infos);

                    new UpdateMyConsole(a.Name, CColors.Blue).Append();
                    new UpdateMyConsole("'s properties successfully copied", CColors.White, true).Append();
                }
            }
        }

        private static string GetAssetInfos(AssetProperties a)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(
                "\n- Name:\t\t" + a.Name +
                "\n- Path:\t\t" + a.Path +
                "\n- Checked:\t" + a.IsChecked +
                "\n- Selected:\tTrue" +
                "\n"
                );

            return sb.ToString();
        }
    }
}
