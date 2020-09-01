using FModel.Logger;
using FModel.Utils;
using FModel.Windows.CustomNotifier;
using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FModel.Windows.ImagesMerger
{
    /// <summary>
    /// Logique d'interaction pour ImagesMerger.xaml
    /// </summary>
    public partial class ImagesMerger : Window
    {
        private const int _MARGIN = 5;

        public ImagesMerger()
        {
            InitializeComponent();
        }

        private async void DrawPreview(object sender, RoutedEventArgs e)
        {
            if (Preview_Img.Source != null) await DrawPreview().ConfigureAwait(false);
        }
        private async Task DrawPreview()
        {
            AddBtn.IsEnabled = false;
            UpBtn.IsEnabled = false;
            DownBtn.IsEnabled = false;
            DeleteBtn.IsEnabled = false;
            ClearBtn.IsEnabled = false;
            ImTheSlider.IsEnabled = false;
            OpenImageBtn.IsEnabled = false;
            SaveImageBtn.IsEnabled = false;

            int num = 1;
            int curW = 0;
            int curH = 0;
            int maxWidth = 0;
            int maxHeight = 0;
            int lineMaxHeight = 0;
            int imagesPerRow = Convert.ToInt32(ImTheSlider.Value);
            Dictionary<int, SKPoint> positions = new Dictionary<int, SKPoint>();
            SKBitmap[] images = new SKBitmap[Images_LstBx.Items.Count];
            for (int i = 0; i < images.Length; i++)
            {
                SKBitmap img = SKBitmap.Decode(new FileInfo((Images_LstBx.Items[i] as ListBoxItem).ContentStringFormat).Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                positions[i] = new SKPoint(curW, curH);
                images[i] = img;

                if (img.Height > lineMaxHeight)
                    lineMaxHeight = img.Height;

                if (num % imagesPerRow == 0)
                {
                    maxWidth = curW + img.Width + _MARGIN;
                    curH += lineMaxHeight + _MARGIN;

                    curW = 0;
                    lineMaxHeight = 0;
                }
                else
                {
                    maxHeight = curH + lineMaxHeight + _MARGIN;
                    curW += img.Width + _MARGIN;
                    if (curW > maxWidth)
                        maxWidth = curW;
                }

                num++;
            }

            await Task.Run(() =>
            {
                using var ret = new SKBitmap(maxWidth - _MARGIN, maxHeight - _MARGIN, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                using var c = new SKCanvas(ret);
                for (int i = 0; i < images.Length; i++)
                {
                    using (images[i])
                    {
                        c.DrawBitmap(images[i], positions[i], new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });
                    }
                }

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
            }).ContinueWith(t =>
            {
                AddBtn.IsEnabled = true;
                UpBtn.IsEnabled = true;
                DownBtn.IsEnabled = true;
                DeleteBtn.IsEnabled = true;
                ClearBtn.IsEnabled = true;
                ImTheSlider.IsEnabled = true;
                OpenImageBtn.IsEnabled = true;
                SaveImageBtn.IsEnabled = true;
            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async void OnAddBtn_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = Properties.Resources.SelectFile,
                Filter = Properties.Resources.PngFilter,
                Multiselect = true,
                InitialDirectory = Properties.Settings.Default.OutputPath + "\\Icons\\"
            };
            if ((bool)ofd.ShowDialog())
            {
                foreach (string file in ofd.FileNames)
                {
                    Images_LstBx.Items.Add(new ListBoxItem
                    {
                        ContentStringFormat = file,
                        Content = Path.GetFileNameWithoutExtension(file)
                    });
                }

                ImTheSlider.Value = Math.Min(Images_LstBx.Items.Count, Math.Round(Math.Sqrt(Images_LstBx.Items.Count)));
                await DrawPreview().ConfigureAwait(false);
            }
        }

        private async void OnUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Images_LstBx.Items.Count > 0 && Images_LstBx.SelectedItems.Count > 0)
            {
                bool reloadImage = false;

                int[] indices = Images_LstBx.SelectedItems.Cast<object>().Select(i => Images_LstBx.Items.IndexOf(i)).ToArray();
                if (indices.Length > 0 && indices[0] > 0)
                {
                    for (int i = 0; i < Images_LstBx.Items.Count; i++)
                    {
                        if (indices.Contains(i))
                        {
                            object moveItem = Images_LstBx.Items[i];
                            Images_LstBx.Items.Remove(moveItem);
                            Images_LstBx.Items.Insert(i - 1, moveItem);
                            ((ListBoxItem)moveItem).IsSelected = true;
                            reloadImage = true;
                        }
                    }
                }

                Images_LstBx.SelectedItems.Add(indices);
                if (reloadImage) await DrawPreview().ConfigureAwait(false);
            }
        }
        private async void OnDownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Images_LstBx.Items.Count > 0 && Images_LstBx.SelectedItems.Count > 0)
            {
                bool reloadImage = false;

                int[] indices = Images_LstBx.SelectedItems.Cast<object>().Select(i => Images_LstBx.Items.IndexOf(i)).ToArray();
                if (indices.Length > 0 && indices[^1] < Images_LstBx.Items.Count - 1)
                {
                    for (int i = Images_LstBx.Items.Count - 1; i > -1; --i)
                    {
                        if (indices.Contains(i))
                        {
                            object moveItem = Images_LstBx.Items[i];
                            Images_LstBx.Items.Remove(moveItem);
                            Images_LstBx.Items.Insert(i + 1, moveItem);
                            ((ListBoxItem)moveItem).IsSelected = true;
                            reloadImage = true;
                        }
                    }
                }

                if (reloadImage) await DrawPreview().ConfigureAwait(false);
            }
        }

        private void OnClearBtn_Click(object sender, RoutedEventArgs e)
        {
            Images_LstBx.Items.Clear();
            Preview_Img.Source = null;
        }

        private async void OnDeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Images_LstBx.Items.Count > 0 && Images_LstBx.SelectedItems.Count > 0)
            {
                for (int i = Images_LstBx.SelectedItems.Count - 1; i >= 0; --i)
                {
                    Images_LstBx.Items.Remove(Images_LstBx.SelectedItems[i]);
                }

                await DrawPreview().ConfigureAwait(false);
            }
        }

        private void OnOpenImageBtn_Click(object sender, RoutedEventArgs e)
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
                    if (Preview_Img.Source.Height > 1000 || Preview_Img.Source.Width > 1800)
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

        private void OnSaveImageBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (Preview_Img.Source != null)
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        Title = Properties.Resources.Save,
                        FileName = Properties.Resources.Preview + ".png",
                        InitialDirectory = Properties.Settings.Default.OutputPath,
                        Filter = Properties.Resources.PngFilter
                    };
                    if ((bool)saveFileDialog.ShowDialog())
                    {
                        using var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create);
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create((BitmapSource)Preview_Img.Source));
                        encoder.Save(fileStream);

                        if (File.Exists(saveFileDialog.FileName))
                        {
                            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[ImagesMerger]", "Preview successfully saved");
                            Globals.gNotifier.ShowCustomMessage(Properties.Resources.Success, Properties.Resources.ImageSaved, string.Empty, saveFileDialog.FileName);
                        }
                    }
                }
                else Globals.gNotifier.ShowCustomMessage(Properties.Resources.Error, Properties.Resources.NoImageToSave);
            });
        }
    }
}
