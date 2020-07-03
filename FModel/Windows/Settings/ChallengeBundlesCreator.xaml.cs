using FModel.Creator.Bundles;
using FModel.Creator.Fortnite;
using FModel.Logger;
using FModel.Windows.ColorPicker;
using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FModel.Windows.Settings
{
    /// <summary>
    /// Logique d'interaction pour ChallengeBundlesCreator.xaml
    /// </summary>
    public partial class ChallengeBundlesCreator : Window
    {
        public ChallengeBundlesCreator()
        {
            InitializeComponent();
        }

        private void OnClick(object sender, RoutedEventArgs e) => SaveAndExit();
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DrawPreview();
        }

        private void SaveAndExit()
        {
            Properties.Settings.Default.Save();
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Closing Challenge Bundles Creator Settings");
            Close();
        }

        private void AddWatermark_Btn_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = Properties.Resources.SelectFile,
                Filter = Properties.Resources.PngFilter
            };
            if ((bool)ofd.ShowDialog())
            {
                Properties.Settings.Default.ChallengeBannerPath = ofd.FileName;
                Properties.Settings.Default.UseChallengeBanner = true;
                Properties.Settings.Default.Save();

                DrawPreview();
            }
        }
        private void DeleteWatermark_Btn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ChallengeBannerPath = string.Empty;
            Properties.Settings.Default.Save();

            DrawPreview();
        }

        private void PrimaryColor_Btn(object sender, RoutedEventArgs e)
        {
            if (ColorPickerWindow.ShowDialog(out Color color))
            {
                Properties.Settings.Default.ChallengeBannerPrimaryColor = color.ToHexString().Substring(1);
                Properties.Settings.Default.Save();
                DrawPreview();
            }
        }
        private void SecondaryColor_Btn(object sender, RoutedEventArgs e)
        {
            if (ColorPickerWindow.ShowDialog(out Color color))
            {
                Properties.Settings.Default.ChallengeBannerSecondaryColor = color.ToHexString().Substring(1);
                Properties.Settings.Default.Save();
                DrawPreview();
            }
        }

        private void OnTextChanged(object sender, KeyEventArgs e) => DrawPreview();
        private void DrawPreview(object sender, RoutedEventArgs e) => DrawPreview();
        private void DrawPreview()
        {
            if (Properties.Settings.Default.UseChallengeBanner)
            {
                BaseBundle icon = new BaseBundle(Watermark_TxtBox.Text);
                using var ret = new SKBitmap(icon.Width, icon.HeaderHeight + icon.AdditionalSize, SKColorType.Rgba8888, SKAlphaType.Opaque);
                using var c = new SKCanvas(ret);

                HeaderStyle.DrawHeaderPaint(c, icon);
                HeaderStyle.DrawHeaderText(c, icon);
                QuestStyle.DrawQuests(c, icon);

                SKImage image = SKImage.FromBitmap(ret);
                using var encoded = image.Encode();
                using var stream = encoded.AsStream();
                BitmapImage photo = new BitmapImage();
                photo.BeginInit();
                photo.CacheOption = BitmapCacheOption.OnLoad;
                photo.StreamSource = stream;
                photo.EndInit();
                photo.Freeze();

                Application.Current.Dispatcher.Invoke(delegate
                {
                    Preview_Img.Source = photo;
                });
            }
            else
            {
                using SKBitmap challengeBase = SKBitmap.Decode(Application.GetResourceStream(new Uri($"pack://application:,,,/Resources/T_Placeholder_Challenge_Image.png")).Stream);
                SKImage image = SKImage.FromBitmap(challengeBase);
                using var encoded = image.Encode();
                using var stream = encoded.AsStream();
                BitmapImage photo = new BitmapImage();
                photo.BeginInit();
                photo.CacheOption = BitmapCacheOption.OnLoad;
                photo.StreamSource = stream;
                photo.EndInit();
                photo.Freeze();

                Application.Current.Dispatcher.Invoke(delegate
                {
                    Preview_Img.Source = photo;
                });
            }
        }
    }
}
