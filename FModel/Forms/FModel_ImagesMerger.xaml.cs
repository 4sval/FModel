using FModel.Methods.Utilities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FProp = FModel.Properties.Settings;

namespace FModel.Forms
{
    /// <summary>
    /// Logique d'interaction pour FModel_ImagesMerger.xaml
    /// </summary>
    public partial class FModel_ImagesMerger : Window
    {
        private static List<string> _imagePath { get; set; }

        public FModel_ImagesMerger()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ImagesListBox.Items.Clear();
        }

        private async void UpdatePreview(object sender, RoutedEventArgs e)
        {
            if (MergerPreview_Image.Source != null)
            {
                await UpdateMergerPreview();
            }
        }

        private async Task UpdateMergerPreview()
        {
            AddImages_Button.IsEnabled = false;
            RemoveImage_Button.IsEnabled = false;
            ClearImages_Button.IsEnabled = false;
            ImagesPerRow_Slider.IsEnabled = false;
            OpenImage_Button.IsEnabled = false;
            SaveImage_Button.IsEnabled = false;

            if ((_imagePath != null && _imagePath.Count > 0) || ImagesListBox.Items.Count > 0)
            {
                _imagePath = new List<string>();
                for (int i = 0; i < ImagesListBox.Items.Count; ++i)
                {
                    _imagePath.Add(((ListBoxItem)ImagesListBox.Items[i]).ContentStringFormat);
                }
            }
            int imageCount = _imagePath.Count;
            int numperrow = Convert.ToInt32(ImagesPerRow_Slider.Value);

            await Task.Run(() =>
            {
                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    //INITIALIZATION
                    drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), new Size(515, 515)));

                    var w = 520 * numperrow;
                    if (imageCount * 520 < 520 * numperrow)
                    {
                        w = imageCount * 520;
                    }

                    int h = int.Parse(Math.Ceiling(double.Parse(imageCount.ToString()) / numperrow).ToString(CultureInfo.InvariantCulture)) * 520;

                    int num = 1;
                    int curW = 0;
                    int curH = 0;
                    int maxHeight = 0;

                    for (int i = 0; i < imageCount; i++)
                    {
                        int percentage = (i + 1) * 100 / imageCount;

                        BitmapImage source = new BitmapImage(new Uri(_imagePath[i]));
                        source.DecodePixelWidth = 515;

                        double width = source.Width;
                        double height = source.Height;
                        if (height > maxHeight) { maxHeight = Convert.ToInt32(height); }

                        drawingContext.DrawImage(source, new Rect(new Point(curW, curH), new Size(width, height)));
                        if (num % numperrow == 0)
                        {
                            curW = 0;
                            curH += maxHeight + 5;
                            num += 1;

                            maxHeight = 0; //reset max height for each new row
                        }
                        else
                        {
                            curW += Convert.ToInt32(width) + 5;
                            num += 1;
                        }
                    }
                }

                if (drawingVisual != null)
                {
                    RenderTargetBitmap RTB = new RenderTargetBitmap((int)Math.Floor(drawingVisual.DescendantBounds.Width), (int)Math.Floor(drawingVisual.DescendantBounds.Height), 96, 96, PixelFormats.Pbgra32);
                    RTB.Render(drawingVisual);
                    RTB.Freeze(); //We freeze to apply the RTB to our imagesource from the UI Thread

                    this.Dispatcher.InvokeAsync(() =>
                    {
                        MergerPreview_Image.Source = BitmapFrame.Create(RTB); //thread safe and fast af
                    });
                }

            }).ContinueWith(TheTask =>
            {
                TasksUtility.TaskCompleted(TheTask.Exception);
            });

            GC.Collect();
            GC.WaitForPendingFinalizers();

            AddImages_Button.IsEnabled = true;
            RemoveImage_Button.IsEnabled = true;
            ClearImages_Button.IsEnabled = true;
            ImagesPerRow_Slider.IsEnabled = true;
            OpenImage_Button.IsEnabled = true;
            SaveImage_Button.IsEnabled = true;
        }

        private async void AddImages_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFiledialog = new OpenFileDialog();
            openFiledialog.Title = "Choose your images";
            openFiledialog.InitialDirectory = FProp.Default.FOutput_Path + "\\Icons\\";
            openFiledialog.Multiselect = true;
            openFiledialog.Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*";
            if (openFiledialog.ShowDialog() == true)
            {
                AddFiles(openFiledialog.FileNames);
                await UpdateMergerPreview();
            }
        }

        private void AddFiles(string[] files)
        {
            if (files.Count() > 0)
            {
                foreach (string file in files)
                {
                    ListBoxItem itm = new ListBoxItem();
                    itm.ContentStringFormat = file;
                    itm.Content = Path.GetFileNameWithoutExtension(file);

                    ImagesListBox.Items.Add(itm);
                }
            }
        }

        private async void RemoveImage_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ImagesListBox.Items.Count > 0 && ImagesListBox.SelectedItems.Count > 0)
            {
                for (int i = ImagesListBox.SelectedItems.Count - 1; i >= 0; --i)
                {
                    ImagesListBox.Items.Remove(ImagesListBox.SelectedItems[i]);
                }

                await UpdateMergerPreview();
            }
        }

        private void ClearImages_Button_Click(object sender, RoutedEventArgs e)
        {
            ImagesListBox.Items.Clear();
            MergerPreview_Image.Source = null;
        }

        private void OpenImage_Button_Click(object sender, RoutedEventArgs e)
        {
            if (MergerPreview_Image.Source != null)
            {
                if (!FormsUtility.IsWindowOpen<Window>("Merged Image"))
                {
                    Window win = new Window();
                    win.Title = "Merged Image";
                    win.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
                    win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    win.Width = MergerPreview_Image.Source.Width;
                    win.Height = MergerPreview_Image.Source.Height;
                    if (MergerPreview_Image.Source.Height > 1000)
                    {
                        win.WindowState = WindowState.Maximized;
                    }

                    DockPanel dockPanel = new DockPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    Image img = new Image();
                    img.UseLayoutRounding = true;
                    img.Source = MergerPreview_Image.Source;
                    dockPanel.Children.Add(img);

                    win.Content = dockPanel;
                    win.Show();
                }
                else { FormsUtility.GetOpenedWindow<Window>("Merged Image").Focus(); }
            }
        }

        private void SaveImage_Button_Click(object sender, RoutedEventArgs e)
        {
            if (MergerPreview_Image.Source != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "Save Image";
                saveFileDialog.FileName = "Merger";
                saveFileDialog.InitialDirectory = FProp.Default.FOutput_Path;
                saveFileDialog.Filter = "PNG Files (*.png)|*.png";
                if (saveFileDialog.ShowDialog() == true)
                {
                    string path = saveFileDialog.FileName;
                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create((BitmapSource)MergerPreview_Image.Source));
                        encoder.Save(fileStream);

                        if (File.Exists(path))
                        {
                            new UpdateMyConsole(System.IO.Path.GetFileNameWithoutExtension(path), CColors.Blue).Append();
                            new UpdateMyConsole(" successfully saved", CColors.White, true).Append();
                        }
                        else //just in case
                        {
                            new UpdateMyConsole("Bruh moment\nCouldn't save ", CColors.White).Append();
                            new UpdateMyConsole(System.IO.Path.GetFileNameWithoutExtension(path), CColors.Blue, true).Append();
                        }
                    }
                }
            }
        }

        private async void Up_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ImagesListBox.Items.Count > 0 && ImagesListBox.SelectedItems.Count > 0)
            {
                bool reloadImage = false;

                int[] indices = ImagesListBox.SelectedItems.Cast<object>().Select(i => ImagesListBox.Items.IndexOf(i)).ToArray();
                if (indices.Length > 0 && indices[0] > 0)
                {
                    for (int i = 0; i < ImagesListBox.Items.Count; i++)
                    {
                        if (indices.Contains(i))
                        {
                            object moveItem = ImagesListBox.Items[i];
                            ImagesListBox.Items.Remove(moveItem);
                            ImagesListBox.Items.Insert(i - 1, moveItem);
                            ((ListBoxItem)moveItem).IsSelected = true;
                            reloadImage = true;
                        }
                    }
                }
                ImagesListBox.SelectedItems.Add(indices);

                if (reloadImage)
                    await UpdateMergerPreview();
            }
        }

        private async void Down_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ImagesListBox.Items.Count > 0 && ImagesListBox.SelectedItems.Count > 0)
            {
                bool reloadImage = false;

                int[] indices = ImagesListBox.SelectedItems.Cast<object>().Select(i => ImagesListBox.Items.IndexOf(i)).ToArray();
                if (indices.Length > 0 && indices[indices.Length - 1] < ImagesListBox.Items.Count - 1)
                {
                    for (int i = ImagesListBox.Items.Count - 1; i > -1; --i)
                    {
                        if (indices.Contains(i))
                        {
                            object moveItem = ImagesListBox.Items[i];
                            ImagesListBox.Items.Remove(moveItem);
                            ImagesListBox.Items.Insert(i + 1, moveItem);
                            ((ListBoxItem)moveItem).IsSelected = true;
                            reloadImage = true;
                        }
                    }
                }

                if (reloadImage)
                    await UpdateMergerPreview();
            }
        }
    }
}
