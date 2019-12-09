using FModel.Methods.MessageBox;
using System.Windows;
using System;
using System.Windows.Controls;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Windows.Media;
using FModel.Methods.Assets;
using FModel.Methods;
using System.Windows.Media.Imaging;
using FModel.Methods.Utilities;
using System.IO;
using System.Threading.Tasks;
using Ookii.Dialogs.Wpf;
using System.Globalization;
using FModel.Methods.Assets.IconCreator;
using ColorPickerWPF;
using FProp = FModel.Properties.Settings;

namespace FModel.Forms
{
    /// <summary>
    /// Logique d'interaction pour FModel_Settings.xaml
    /// </summary>
    public partial class FModel_Settings : Window
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
                            .Where(a => ((DescriptionAttribute)a.Att)
                                .Description == description).SingleOrDefault();
            return field == null ? default(T) : (T)field.Field.GetRawConstantValue();
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FoldersUtility.CheckWatermark();
            GetUserSettings();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetUserSettings();
            DebugHelper.WriteUserSettings();
            Close();
        }

        private async void GetUserSettings()
        {
            InputTextBox.Text = FProp.Default.FPak_Path;
            bDiffFileSize.IsChecked = FProp.Default.FDiffFileSize;
            OutputTextBox.Text = FProp.Default.FOutput_Path;
            bReloadAES.IsChecked = FProp.Default.ReloadAES;
            bOpenSounds.IsChecked = FProp.Default.FOpenSounds;

            ComboBox_Language.SelectedIndex = (int)GetEnumValueFromDescription<LIndexes>(FProp.Default.FLanguage);
            ComboBox_Design.SelectedIndex = (int)GetEnumValueFromDescription<RIndexes>(FProp.Default.FRarity_Design);

            bFeaturedIcon.IsChecked = FProp.Default.FIsFeatured;
            bWatermarkIcon.IsChecked = FProp.Default.FUseWatermark;
            Watermark_Label.Content += Path.GetFileName(FProp.Default.FWatermarkFilePath);

            Opacity_Slider.Value = FProp.Default.FWatermarkOpacity;
            Scale_Slider.Value = FProp.Default.FWatermarkScale;
            xPos_Slider.Value = FProp.Default.FWatermarkXPos;
            yPos_Slider.Value = FProp.Default.FWatermarkYPos;

            WatermarkChallenge_TextBox.Text = FProp.Default.FChallengeWatermark;
            bCustomChallenge.IsChecked = FProp.Default.FUseChallengeWatermark;
            Banner_Label.Content += Path.GetFileName(FProp.Default.FBannerFilePath);
            OpacityBanner_Slider.Value = FProp.Default.FBannerOpacity;

            await UpdateImageWithWatermark();
            UpdateChallengeCustomTheme();
        }

        private void SetUserSettings()
        {
            bool restart = false;

            if (!string.Equals(FProp.Default.FPak_Path, InputTextBox.Text))
            {
                FProp.Default.FPak_Path = InputTextBox.Text;
                restart = true;
            }

            FProp.Default.FDiffFileSize = (bool)bDiffFileSize.IsChecked;
            FProp.Default.ReloadAES     = (bool)bReloadAES.IsChecked;
            FProp.Default.FOpenSounds   = (bool)bOpenSounds.IsChecked;

            if (!string.Equals(FProp.Default.FOutput_Path, OutputTextBox.Text))
            {
                FProp.Default.FOutput_Path = OutputTextBox.Text;
                restart = true;
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

            FProp.Default.FChallengeWatermark = WatermarkChallenge_TextBox.Text;
            FProp.Default.FUseChallengeWatermark = (bool)bCustomChallenge.IsChecked;
            FProp.Default.FBannerOpacity = Convert.ToInt32(OpacityBanner_Slider.Value);

            FProp.Default.Save();

            if (restart)
            {
                DarkMessageBox.Show("Please, restart FModel to apply your new path(s)", "FModel Path(s) Changed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void UpdateImageBox(object sender, RoutedEventArgs e)
        {
            await UpdateImageWithWatermark();
        }
        private async void EnableDisableWatermark(object sender, RoutedEventArgs e)
        {
            OpenFile_Button.IsEnabled = (bool)bWatermarkIcon.IsChecked;
            xPos_Slider.IsEnabled = (bool)bWatermarkIcon.IsChecked;
            yPos_Slider.IsEnabled = (bool)bWatermarkIcon.IsChecked;
            Opacity_Slider.IsEnabled = (bool)bWatermarkIcon.IsChecked;
            Scale_Slider.IsEnabled = (bool)bWatermarkIcon.IsChecked;

            await UpdateImageWithWatermark();
        }
        private void EnableDisableCustomTheme(object sender, RoutedEventArgs e)
        {
            AddBanner_Button.IsEnabled = (bool)bCustomChallenge.IsChecked;
            DeleteBanner_Button.IsEnabled = (bool)bCustomChallenge.IsChecked;
            PrimaryColor_Button.IsEnabled = (bool)bCustomChallenge.IsChecked;
            SecondaryColor_Button.IsEnabled = (bool)bCustomChallenge.IsChecked;
            OpacityBanner_Slider.IsEnabled = (bool)bCustomChallenge.IsChecked;

            UpdateChallengeCustomTheme();
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

                            drawingContext.DrawImage(ImagesUtility.CreateTransparency(bmp, opacity), new Rect(xPos, yPos, scale, scale));
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
        private void UpdateChallengeCustomTheme(object sender, RoutedEventArgs e)
        {
            UpdateChallengeCustomTheme();
        }
        private void UpdateChallengeCustomTheme()
        {
            bool watermarkEnabled = (bool)bCustomChallenge.IsChecked;
            string watermark = WatermarkChallenge_TextBox.Text;
            string path = FProp.Default.FBannerFilePath;
            int opacity = Convert.ToInt32(OpacityBanner_Slider.Value);
            string[] primaryParts = FProp.Default.FPrimaryColor.Split(':');
            string[] secondaryParts = FProp.Default.FSecondaryColor.Split(':');
            SolidColorBrush PrimaryColor = new SolidColorBrush(Color.FromRgb(Convert.ToByte(primaryParts[0]), Convert.ToByte(primaryParts[1]), Convert.ToByte(primaryParts[2])));
            SolidColorBrush SecondaryColor = new SolidColorBrush(Color.FromRgb(Convert.ToByte(secondaryParts[0]), Convert.ToByte(secondaryParts[1]), Convert.ToByte(secondaryParts[2])));

            if (watermarkEnabled)
            {
                DrawingVisual drawingVisual = new DrawingVisual();
                double PPD = VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip;
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    //INITIALIZATION
                    drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Size(1024, 410)));

                    Point dStart = new Point(0, 256);
                    LineSegment[] dSegments = new[]
                    {
                                new LineSegment(new Point(1024, 256), true),
                                new LineSegment(new Point(1024, 241), true),
                                new LineSegment(new Point(537, 236), true),
                                new LineSegment(new Point(547, 249), true),
                                new LineSegment(new Point(0, 241), true)
                            };
                    PathFigure dFigure = new PathFigure(dStart, dSegments, true);
                    PathGeometry dGeo = new PathGeometry(new[] { dFigure });

                    Typeface typeface = new Typeface(TextsUtility.Burbank, FontStyles.Normal, FontWeights.Black, FontStretches.Normal);
                    FormattedText formattedText =
                        new FormattedText(
                            "{BUNDLE DISPLAY NAME HERE}",
                            CultureInfo.CurrentUICulture,
                            FlowDirection.LeftToRight,
                            typeface,
                            55,
                            Brushes.White,
                            PPD
                            );
                    formattedText.TextAlignment = TextAlignment.Left;
                    formattedText.MaxTextWidth = 768;
                    formattedText.MaxLineCount = 1;
                    Point textLocation = new Point(50, 165 - formattedText.Height);

                    drawingContext.DrawRectangle(PrimaryColor, null, new Rect(0, 0, 1024, 256));
                    if (!string.IsNullOrEmpty(path))
                    {
                        BitmapImage bmp = new BitmapImage(new Uri(path));
                        drawingContext.DrawImage(ImagesUtility.CreateTransparency(bmp, opacity), new Rect(0, 0, 1024, 256));
                    }
                    drawingContext.DrawGeometry(SecondaryColor, null, dGeo);
                    drawingContext.DrawText(formattedText, textLocation);

                    formattedText =
                new FormattedText(
                    "{LAST FOLDER HERE}",
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    30,
                    SecondaryColor,
                    IconCreator.PPD
                    );
                    formattedText.TextAlignment = TextAlignment.Left;
                    formattedText.MaxTextWidth = 768;
                    formattedText.MaxLineCount = 1;
                    textLocation = new Point(50, 100 - formattedText.Height);
                    Geometry geometry = formattedText.BuildGeometry(textLocation);
                    Pen pen = new Pen(ChallengesUtility.DarkBrush(SecondaryColor, 0.3f), 1);
                    pen.LineJoin = PenLineJoin.Round;
                    drawingContext.DrawGeometry(SecondaryColor, pen, geometry);

                    typeface = new Typeface(TextsUtility.FBurbank, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                    formattedText =
                        new FormattedText(
                            watermark,
                            CultureInfo.CurrentUICulture,
                            FlowDirection.LeftToRight,
                            typeface,
                            20,
                            new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
                            IconCreator.PPD
                            );
                    formattedText.TextAlignment = TextAlignment.Right;
                    formattedText.MaxTextWidth = 1014;
                    formattedText.MaxLineCount = 1;
                    textLocation = new Point(0, 205);
                    drawingContext.DrawText(formattedText, textLocation);

                    LinearGradientBrush linGrBrush = new LinearGradientBrush();
                    linGrBrush.StartPoint = new Point(0, 0);
                    linGrBrush.EndPoint = new Point(0, 1);
                    linGrBrush.GradientStops.Add(new GradientStop(Color.FromArgb(75, SecondaryColor.Color.R, SecondaryColor.Color.G, SecondaryColor.Color.B), 0));
                    linGrBrush.GradientStops.Add(new GradientStop(Color.FromArgb(25, PrimaryColor.Color.R, PrimaryColor.Color.G, PrimaryColor.Color.B), 0.15));
                    linGrBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));

                    drawingContext.DrawRectangle(ChallengesUtility.DarkBrush(PrimaryColor, 0.3f), null, new Rect(0, 256, 1024, 144));
                    drawingContext.DrawRectangle(linGrBrush, null, new Rect(0, 256, 1024, 144));

                    typeface = new Typeface(TextsUtility.Burbank, FontStyles.Normal, FontWeights.Black, FontStretches.Normal);
                    int y = 300;

                    drawingContext.DrawRectangle(ChallengesUtility.DarkBrush(PrimaryColor, 0.3f), null, new Rect(0, y, 1024, 90));
                    drawingContext.DrawRectangle(PrimaryColor, null, new Rect(25, y, 1024 - 50, 70));

                    dStart = new Point(32, y + 5);
                    dSegments = new[]
                    {
                        new LineSegment(new Point(29, y + 67), true),
                        new LineSegment(new Point(1024 - 160, y + 62), true),
                        new LineSegment(new Point(1024 - 150, y + 4), true)
                    };
                    dFigure = new PathFigure(dStart, dSegments, true);
                    dGeo = new PathGeometry(new[] { dFigure });
                    drawingContext.DrawGeometry(ChallengesUtility.LightBrush(PrimaryColor, 0.04f), null, dGeo);

                    drawingContext.DrawRectangle(SecondaryColor, null, new Rect(60, y + 47, 500, 7));

                    dStart = new Point(39, y + 35);
                    dSegments = new[]
                    {
                        new LineSegment(new Point(45, y + 32), true),
                        new LineSegment(new Point(48, y + 37), true),
                        new LineSegment(new Point(42, y + 40), true)
                    };
                    dFigure = new PathFigure(dStart, dSegments, true);
                    dGeo = new PathGeometry(new[] { dFigure });
                    drawingContext.DrawGeometry(SecondaryColor, null, dGeo);
                }

                if (drawingVisual != null)
                {
                    RenderTargetBitmap RTB = new RenderTargetBitmap(1024, 410, 96, 96, PixelFormats.Pbgra32);
                    RTB.Render(drawingVisual);
                    RTB.Freeze(); //We freeze to apply the RTB to our imagesource from the UI Thread

                    FWindow.FMain.Dispatcher.InvokeAsync(() =>
                    {
                        ImageBox_ChallengePreview.Source = BitmapFrame.Create(RTB); //thread safe and fast af
                    });
                }
            }
            else
            {
                BitmapImage source = new BitmapImage(new Uri(CHALLENGE_TEMPLATE_ICON));
                ImageBox_ChallengePreview.Source = source;
            }
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

        private void BrowseInput_Button_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true; // This applies to the Vista style dialog only, not the old dialog.

            if ((bool)dialog.ShowDialog(this))
            {
                InputTextBox.Text = dialog.SelectedPath;
            }
        }

        private void BrowseOutput_Button_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true; // This applies to the Vista style dialog only, not the old dialog.

            if ((bool)dialog.ShowDialog(this))
            {
                OutputTextBox.Text = dialog.SelectedPath;
            }
        }

        private void OpenChallengeTheme_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ImageBox_ChallengePreview.Source != null)
            {
                if (!FormsUtility.IsWindowOpen<Window>("Challenge Theme Template"))
                {
                    Window win = new Window();
                    win.Title = "Challenge Theme Template";
                    win.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
                    win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    win.Width = ImageBox_ChallengePreview.Source.Width;
                    win.Height = ImageBox_ChallengePreview.Source.Height;

                    DockPanel dockPanel = new DockPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    Image img = new Image();
                    img.UseLayoutRounding = true;
                    img.Source = ImageBox_ChallengePreview.Source;
                    dockPanel.Children.Add(img);

                    win.Content = dockPanel;
                    win.Show();
                }
                else { FormsUtility.GetOpenedWindow<Window>("Challenge Theme Template").Focus(); }
            }
        }

        private void PrimaryColor_Button_Click(object sender, RoutedEventArgs e)
        {
            Color color;
            bool ok = ColorPickerWindow.ShowDialog(out color);
            if (ok)
            {
                FProp.Default.FPrimaryColor = color.R + ":" + color.G + ":" + color.B;
                FProp.Default.Save();

                UpdateChallengeCustomTheme();
            }
        }

        private void SecondaryColor_Button_Click(object sender, RoutedEventArgs e)
        {
            Color color;
            bool ok = ColorPickerWindow.ShowDialog(out color);
            if (ok)
            {
                FProp.Default.FSecondaryColor = color.R + ":" + color.G + ":" + color.B;
                FProp.Default.Save();

                UpdateChallengeCustomTheme();
            }
        }

        private void AddBanner_Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFiledialog = new Microsoft.Win32.OpenFileDialog();
            openFiledialog.Title = "Choose your banner";
            openFiledialog.Multiselect = false;
            openFiledialog.Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*";
            if (openFiledialog.ShowDialog() == true)
            {
                Banner_Label.Content = "File Name: " + Path.GetFileName(openFiledialog.FileName);
                FProp.Default.FBannerFilePath = openFiledialog.FileName;
                FProp.Default.Save();

                UpdateChallengeCustomTheme();
            }
        }

        private void DeleteBanner_Button_Click(object sender, RoutedEventArgs e)
        {
            Banner_Label.Content = "File Name: ";
            FProp.Default.FBannerFilePath = string.Empty;
            FProp.Default.Save();

            UpdateChallengeCustomTheme();
        }
    }
}
