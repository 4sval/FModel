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
using System.Windows.Media.Imaging;
using FModel.Methods.Utilities;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;

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

        private async void GetUserSettings()
        {
            InputTextBox.Text = FProp.Default.FPak_Path;    
            OutputTextBox.Text = FProp.Default.FOutput_Path;

            ComboBox_Language.SelectedIndex = (int)GetEnumValueFromDescription<LIndexes>(FProp.Default.FLanguage);
            ComboBox_Design.SelectedIndex = (int)GetEnumValueFromDescription<RIndexes>(FProp.Default.FRarity_Design);

            bFeaturedIcon.IsChecked = FProp.Default.FIsFeatured;
            bWatermarkIcon.IsChecked = FProp.Default.FUseWatermark;
            Watermark_Label.Content += Path.GetFileName(FProp.Default.FWatermarkFilePath);

            Opacity_Slider.Value = FProp.Default.FWatermarkOpacity;
            Scale_Slider.Value = FProp.Default.FWatermarkScale;
            xPos_Slider.Value = FProp.Default.FWatermarkXPos;
            yPos_Slider.Value = FProp.Default.FWatermarkYPos;

            await UpdateImageWithWatermark();
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
            FProp.Default.FIsFeatured = (bool)bFeaturedIcon.IsChecked;
            FProp.Default.FUseWatermark = (bool)bWatermarkIcon.IsChecked;

            FProp.Default.FWatermarkOpacity = Convert.ToInt32(Opacity_Slider.Value);
            FProp.Default.FWatermarkScale = Scale_Slider.Value;
            FProp.Default.FWatermarkXPos = xPos_Slider.Value;
            FProp.Default.FWatermarkYPos = yPos_Slider.Value;

            FProp.Default.Save();
        }

        private async void UpdateImageBox(object sender, RoutedEventArgs e)
        {
            await UpdateImageWithWatermark();
        }
        private void EnableDisableWatermark(object sender, RoutedEventArgs e)
        {
            OpenFile_Button.IsEnabled = (bool)bWatermarkIcon.IsChecked;
            xPos_Slider.IsEnabled = (bool)bWatermarkIcon.IsChecked;
            yPos_Slider.IsEnabled = (bool)bWatermarkIcon.IsChecked;
            Opacity_Slider.IsEnabled = (bool)bWatermarkIcon.IsChecked;
            Scale_Slider.IsEnabled = (bool)bWatermarkIcon.IsChecked;
        }
        private async void UpdateImageWithWatermark(object sender, RoutedEventArgs e)
        {
            await UpdateImageWithWatermark();
        }
        private async Task UpdateImageWithWatermark()
        {
            bool watermarkEnabled = (bool)bWatermarkIcon.IsChecked;
            string rarityDesign = ((ComboBoxItem)ComboBox_Design.SelectedItem).Content.ToString();
            bool isFeatured = (bool)bFeaturedIcon.IsChecked;
            int opacity = Convert.ToInt32(Opacity_Slider.Value);
            double scale = Scale_Slider.Value;
            double xPos = xPos_Slider.Value;
            double yPos = yPos_Slider.Value;

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
                            source = new BitmapImage(new Uri(isFeatured ? "pack://application:,,,/Resources/Template_D_F.png" : "pack://application:,,,/Resources/Template_D_N.png"));
                            break;
                        case "Flat":
                            source = new BitmapImage(new Uri(isFeatured ? "pack://application:,,,/Resources/Template_F_F.png" : "pack://application:,,,/Resources/Template_F_N.png"));
                            break;
                        case "Minimalist":
                            source = new BitmapImage(new Uri(isFeatured ? "pack://application:,,,/Resources/Template_M_F.png" : "pack://application:,,,/Resources/Template_M_N.png"));
                            break;
                    }
                    drawingContext.DrawImage(source, new Rect(new Point(0, 0), new Size(515, 515)));

                    if (!string.IsNullOrEmpty(FProp.Default.FWatermarkFilePath) && watermarkEnabled)
                    {
                        using (StreamReader image = new StreamReader(FProp.Default.FWatermarkFilePath))
                        {
                            if (image != null)
                            {
                                BitmapImage bmp = new BitmapImage();
                                bmp.BeginInit();
                                bmp.CacheOption = BitmapCacheOption.OnLoad;
                                bmp.StreamSource = image.BaseStream;
                                bmp.EndInit();

                                drawingContext.DrawImage(ImagesUtility.CreateTransparency(bmp, opacity), new Rect(xPos, yPos, scale, scale));
                            }
                        }
                    }
                }

                if (drawingVisual != null)
                {
                    RenderTargetBitmap RTB = new RenderTargetBitmap(515, 515, 96, 96, PixelFormats.Pbgra32);
                    RTB.Render(drawingVisual);
                    RTB.Freeze(); //We freeze to apply the RTB to our imagesource from the UI Thread

                    FWindow.FMain.Dispatcher.InvokeAsync(() =>
                    {
                        ImageBox_RarityPreview.Source = BitmapFrame.Create(RTB); //thread safe and fast af
                    });
                }

            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });
        }

        private void OpenIconCreator_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ImageBox_RarityPreview.Source != null)
            {
                if (!FormsUtility.IsWindowOpen<Window>("Icon Template"))
                {
                    Window win = new Window();
                    win.Title = "Icon Template";
                    win.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
                    win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    win.Width = ImageBox_RarityPreview.Source.Width;
                    win.Height = ImageBox_RarityPreview.Source.Height;

                    DockPanel dockPanel = new DockPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    Image img = new Image();
                    img.UseLayoutRounding = true;
                    img.Source = ImageBox_RarityPreview.Source;
                    dockPanel.Children.Add(img);

                    win.Content = dockPanel;
                    win.Show();
                }
                else { FormsUtility.GetOpenedWindow<Window>("Icon Template").Focus(); }
            }
        }

        private async void OpenFile_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFiledialog = new OpenFileDialog();
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
    }
}
