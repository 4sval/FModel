using FModel.Logger;
using FModel.Utils;
using FModel.Windows.CustomNotifier;
using Microsoft.Win32;
using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FModel.ViewModels.ImageBox
{
    public static class ImageBoxVm
    {
        public static readonly ImageBoxViewModel imageBoxViewModel = new ImageBoxViewModel();

        public static void Set(this ImageBoxViewModel vm, BitmapImage image, string name)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Image = image;
                vm.Name = name;
            });

            if (Properties.Settings.Default.AutoSaveImage) vm.Save(true);
        }
        public static void Set(this ImageBoxViewModel vm, SKBitmap image, string name) => vm.Set(SKImage.FromBitmap(image), name);
        public static void Set(this ImageBoxViewModel vm, SKImage image, string name)
        {
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
                vm.Image = photo;
                vm.Name = name;
            });

            if (Properties.Settings.Default.AutoSaveImage) vm.Save(true);
        }

        public static void Reset(this ImageBoxViewModel vm)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Image = null;
            });
        }

        public static void OpenImage(this ImageBoxViewModel vm)
        {
            if (vm.Image != null)
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", $"Opening image of {vm.Name}");
                if (!FWindows.IsWindowOpen<Window>(vm.Name))
                {
                    Window win = new Window
                    {
                        Title = vm.Name,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Width = vm.Image.Width,
                        Height = vm.Image.Height
                    };
                    win.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
                    if (vm.Image.Height > 1000)
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
                        Source = vm.Image
                    };
                    dockPanel.Children.Add(img);

                    win.Content = dockPanel;
                    win.Show();
                }
                else { FWindows.GetOpenedWindow<Window>(vm.Name).Focus(); }
            }
        }

        public static void Copy(this ImageBoxViewModel vm)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (vm.Image != null)
                {
                    Clipboard.SetImage(vm.Image);
                }
                else Globals.gNotifier.ShowCustomMessage(Properties.Resources.Error, Properties.Resources.NoImageToCopy);
            });
        }

        public static void Save(this ImageBoxViewModel vm, bool autoSave)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (vm.Image != null)
                {
                    if (autoSave)
                    {
                        string path = Properties.Settings.Default.OutputPath + "\\Icons\\" + Path.ChangeExtension(vm.Name, ".png");
                        using var fileStream = new FileStream(path, FileMode.Create);
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(vm.Image));
                        encoder.Save(fileStream);

                        if (File.Exists(path))
                        {
                            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AvalonEditViewModel]", $"{vm.Name} successfully saved");
                            FConsole.AppendText(string.Format(Properties.Resources.SaveSuccess, Path.ChangeExtension(vm.Name, ".png")), FColors.Green, true);
                        }
                    }
                    else
                    {
                        var saveFileDialog = new SaveFileDialog
                        {
                            Title = Properties.Resources.Save,
                            FileName = Path.ChangeExtension(vm.Name, ".png"),
                            InitialDirectory = Properties.Settings.Default.OutputPath + "\\Icons\\",
                            Filter = Properties.Resources.PngFilter
                        };
                        if ((bool)saveFileDialog.ShowDialog())
                        {
                            using var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create);
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(vm.Image));
                            encoder.Save(fileStream);

                            if (File.Exists(saveFileDialog.FileName))
                            {
                                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AvalonEditViewModel]", $"{vm.Name} successfully saved");
                                Globals.gNotifier.ShowCustomMessage(Properties.Resources.Success, Properties.Resources.ImageSaved, string.Empty, saveFileDialog.FileName);
                            }
                        }
                    }
                }
                else Globals.gNotifier.ShowCustomMessage(Properties.Resources.Error, Properties.Resources.NoImageToSave);
            });
        }
    }

    public class ImageBoxViewModel : PropertyChangedBase
    {
        private BitmapImage _image;
        public BitmapImage Image
        {
            get { return _image; }

            set { this.SetProperty(ref this._image, value); }
        }
        private string _name;
        public string Name
        {
            get { return _name; }

            set { this.SetProperty(ref this._name, value); }
        }
    }
}
