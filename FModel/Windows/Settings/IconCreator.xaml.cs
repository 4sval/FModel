using FModel.Creator;
using FModel.Creator.Icons;
using FModel.Logger;
using FModel.Utils;
using FModel.ViewModels.ComboBox;
using Microsoft.Win32;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FModel.Windows.Settings
{
    /// <summary>
    /// Logique d'interaction pour IconCreator.xaml
    /// </summary>
    public partial class IconCreator : Window
    {
        private bool _lastCheckState;

        public IconCreator()
        {
            InitializeComponent();
        }

        private void OnClick(object sender, RoutedEventArgs e) => SaveAndExit();
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Designs_CbBox.ItemsSource = ComboBoxVm.designCbViewModel;
            Designs_CbBox.SelectedItem = ComboBoxVm.designCbViewModel.Where(x => x.Id == Properties.Settings.Default.AssetsIconDesign).FirstOrDefault();
            _lastCheckState = Properties.Settings.Default.UseGameColors;
        }

        private void SaveAndExit()
        {
            Properties.Settings.Default.AssetsIconDesign = Designs_CbBox.SelectedIndex;

            Properties.Settings.Default.Save();
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Closing Icon Creator Settings");
            Close();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.SelectedItem is ComboBoxViewModel selectedItem)
            {
                if (UseGameColor_CkBox.IsEnabled) _lastCheckState = (bool)UseGameColor_CkBox.IsChecked;

                DrawPreview();

                bool b = (EIconDesign)selectedItem.Property == EIconDesign.Flat;
                UseGameColor_CkBox.IsEnabled = !b;

                if (b) UseGameColor_CkBox.IsChecked = false;
                else UseGameColor_CkBox.IsChecked = _lastCheckState;
            }
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
                Properties.Settings.Default.IconWatermarkPath = ofd.FileName;
                Properties.Settings.Default.UseIconWatermark = true;
                Properties.Settings.Default.Save();

                DrawPreview();
            }
        }
        private void DeleteWatermark_Btn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.IconWatermarkPath = string.Empty;
            Properties.Settings.Default.UseIconWatermark = false;
            Properties.Settings.Default.Save();

            DrawPreview();
        }
        private void OnImageOpenClick(object sender, RoutedEventArgs e)
        {
            if (Preview_Img.Source != null)
            {
                if (!FWindows.IsWindowOpen<Window>(Properties.Resources.Preview))
                {
                    Window win = new Window
                    {
                        Title = Properties.Resources.Preview,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Width = Preview_Img.Source.Width,
                        Height = Preview_Img.Source.Height
                    };
                    win.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
                    if (Preview_Img.Source.Height > 1000)
                    {
                        win.WindowState = WindowState.Maximized;
                    }

                    DockPanel dockPanel = new DockPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    Image img = new Image
                    {
                        UseLayoutRounding = true,
                        Source = Preview_Img.Source
                    };
                    dockPanel.Children.Add(img);

                    win.Content = dockPanel;
                    win.Show();
                }
                else { FWindows.GetOpenedWindow<Window>(Properties.Resources.Preview).Focus(); }
            }
        }

        private void DrawPreview(object sender, RoutedEventArgs e) => DrawPreview();
        private void DrawPreview()
        {
            if (Designs_CbBox.HasItems && Designs_CbBox.SelectedIndex >= 0 && Designs_CbBox.SelectedItem is ComboBoxViewModel selectedItem)
            {
                using SKBitmap rarityBase = SKBitmap.Decode(Application.GetResourceStream(new Uri($"pack://application:,,,/Resources/EIconDesign_{(EIconDesign)selectedItem.Property}.png")).Stream);
                using var ret = new SKBitmap(512, 512, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var c = new SKCanvas(ret);
                c.DrawBitmap(rarityBase, new SKRect(0, 0, 512, 512),
                    new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

                Watermark.DrawWatermark(c);

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
        }
    }
}
