using System;
using FModel.Extensions;
using FModel.Framework;
using FModel.Settings;
using FModel.ViewModels.Commands;
using FModel.Views.Resources.Controls;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Serilog;
using SkiaSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;

namespace FModel.ViewModels
{
    public class TabImage : ViewModel
    {
        public string ExportName { get; }
        public byte[] ImageBuffer { get; set; }

        public TabImage(string name, bool rnn, SKBitmap img)
        {
            ExportName = name;
            RenderNearestNeighbor = rnn;
            SetImage(img);
        }

        private BitmapImage _image;
        public BitmapImage Image
        {
            get => _image;
            set
            {
                if (_image == value) return;
                SetProperty(ref _image, value);
            }
        }

        private bool _renderNearestNeighbor;
        public bool RenderNearestNeighbor
        {
            get => _renderNearestNeighbor;
            set => SetProperty(ref _renderNearestNeighbor, value);
        }

        private bool _noAlpha;
        public bool NoAlpha
        {
            get => _noAlpha;
            set
            {
                SetProperty(ref _noAlpha, value);
                ResetImage();
            }
        }

        private void SetImage(SKBitmap bitmap)
        {
            _bmp = bitmap;

            using var data = _bmp.Encode(NoAlpha ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(ImageBuffer = data.ToArray(), false);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            Image = image;
        }

        private SKBitmap _bmp;
        private void ResetImage() => SetImage(_bmp);
    }

    public class TabItem : ViewModel
    {
        private string _header;
        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        private string _directory;
        public string Directory
        {
            get => _directory;
            set => SetProperty(ref _directory, value);
        }

        private bool _hasSearchOpen;
        public bool HasSearchOpen
        {
            get => _hasSearchOpen;
            set => SetProperty(ref _hasSearchOpen, value);
        }

        private string _textToFind;
        public string TextToFind
        {
            get => _textToFind;
            set => SetProperty(ref _textToFind, value);
        }

        private bool _searchUp;
        public bool SearchUp
        {
            get => _searchUp;
            set => SetProperty(ref _searchUp, value);
        }

        private bool _caseSensitive;
        public bool CaseSensitive
        {
            get => _caseSensitive;
            set => SetProperty(ref _caseSensitive, value);
        }

        private bool _useRegEx;
        public bool UseRegEx
        {
            get => _useRegEx;
            set => SetProperty(ref _useRegEx, value);
        }

        private bool _wholeWord;
        public bool WholeWord
        {
            get => _wholeWord;
            set => SetProperty(ref _wholeWord, value);
        }

        private TextDocument _document;
        public TextDocument Document
        {
            get => _document;
            set => SetProperty(ref _document, value);
        }

        private double _fontSize = 11.0;
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        private double _scrollPosition;
        public double ScrollPosition
        {
            get => _scrollPosition;
            set => SetProperty(ref _scrollPosition, value);
        }

        private string _scrollTrigger;
        public string ScrollTrigger
        {
            get => _scrollTrigger;
            set => SetProperty(ref _scrollTrigger, value);
        }

        private IHighlightingDefinition _highlighter;
        public IHighlightingDefinition Highlighter
        {
            get => _highlighter;
            set
            {
                if (_highlighter == value) return;
                SetProperty(ref _highlighter, value);
            }
        }

        private TabImage _selectedImage;
        public TabImage SelectedImage
        {
            get => _selectedImage;
            set
            {
                if (_selectedImage == value) return;
                SetProperty(ref _selectedImage, value);
                RaisePropertyChanged("HasImage");
                RaisePropertyChanged("Page");
            }
        }

        public bool HasImage => SelectedImage != null;
        public bool HasMultipleImages => _images.Count > 1;
        public string Page => $"{_images.IndexOf(_selectedImage) + 1} / {_images.Count}";

        private readonly ObservableCollection<TabImage> _images;

        public bool ShouldScroll => !string.IsNullOrEmpty(ScrollTrigger);

        private TabCommand _tabCommand;
        public TabCommand TabCommand => _tabCommand ??= new TabCommand(this);
        private ImageCommand _imageCommand;
        public ImageCommand ImageCommand => _imageCommand ??= new ImageCommand(this);
        private GoToCommand _goToCommand;
        public GoToCommand GoToCommand => _goToCommand ??= new GoToCommand(null);

        public TabItem(string header, string directory)
        {
            Header = header;
            Directory = directory;
            _images = new ObservableCollection<TabImage>();
        }

        public void ClearImages()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _images.Clear();
                SelectedImage = null;
                RaisePropertyChanged("HasMultipleImages");
            });
        }

        public void AddImage(UTexture2D texture) => AddImage(texture.Name, texture.bRenderNearestNeighbor, texture.Decode());
        public void AddImage(string name, bool rnn, SKBitmap img)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var t = new TabImage(name, rnn, img);
                if (UserSettings.Default.IsAutoSaveTextures)
                    SaveImage(t, true);

                _images.Add(t);
                SelectedImage ??= t;
                RaisePropertyChanged("Page");
                RaisePropertyChanged("HasMultipleImages");
            });
        }

        public void GoPreviousImage() => SelectedImage = _images.Previous(SelectedImage);
        public void GoNextImage() => SelectedImage = _images.Next(SelectedImage);

        public void SetDocumentText(string text, bool bulkSave)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Document ??= new TextDocument();
                Document.Text = text;

                if (UserSettings.Default.IsAutoSaveProps || bulkSave)
                    SaveProperty(true);
            });
        }
        public void ResetDocumentText()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Document ??= new TextDocument();
                Document.Text = string.Empty;
            });
        }

        public void SaveImage(bool autoSave) => SaveImage(SelectedImage, autoSave);
        private void SaveImage(TabImage image, bool autoSave)
        {
            if (image == null) return;
            var fileName = $"{image.ExportName}.png";
            var directory = Path.Combine(UserSettings.Default.TextureDirectory,
                UserSettings.Default.KeepDirectoryStructure ? Directory : "", fileName!).Replace('\\', '/');

            if (!autoSave)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Texture",
                    FileName = fileName,
                    InitialDirectory = UserSettings.Default.TextureDirectory,
                    Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*"
                };
                var result = saveFileDialog.ShowDialog();
                if (!result.HasValue || !result.Value) return;
                directory = saveFileDialog.FileName;
            }
            else
            {
                System.IO.Directory.CreateDirectory(directory.SubstringBeforeLast('/'));
            }

            using (var fs = new FileStream(directory, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                fs.Write(image.ImageBuffer, 0, image.ImageBuffer.Length);
            }

            SaveCheck(directory, fileName);
        }

        public void SaveProperty(bool autoSave)
        {
            var fileName = Path.ChangeExtension(Header, ".json");
            var directory = Path.Combine(UserSettings.Default.PropertiesDirectory,
                UserSettings.Default.KeepDirectoryStructure ? Directory : "", fileName).Replace('\\', '/');

            if (!autoSave)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Property",
                    FileName = fileName,
                    InitialDirectory = UserSettings.Default.PropertiesDirectory,
                    Filter = "JSON Files (*.json)|*.json|INI Files (*.ini)|*.ini|XML Files (*.xml)|*.xml|All Files (*.*)|*.*"
                };
                var result = saveFileDialog.ShowDialog();
                if (!result.HasValue || !result.Value) return;
                directory = saveFileDialog.FileName;
            }
            else
            {
                System.IO.Directory.CreateDirectory(directory.SubstringBeforeLast('/'));
            }

            Application.Current.Dispatcher.Invoke(() => File.WriteAllText(directory, Document.Text));
            SaveCheck(directory, fileName);
        }

        private void SaveCheck(string path, string fileName)
        {
            if (File.Exists(path))
            {
                Log.Information("{FileName} successfully saved", fileName);
                FLogger.AppendInformation();
                FLogger.AppendText($"Successfully saved '{fileName}'", Constants.WHITE, true);
            }
            else
            {
                Log.Error("{FileName} could not be saved", fileName);
                FLogger.AppendError();
                FLogger.AppendText($"Could not save '{fileName}'", Constants.WHITE, true);
            }
        }
    }

    public class TabControlViewModel : ViewModel
    {
        private TabItem _selectedTab;
        public TabItem SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        private AddTabCommand _addTabCommand;
        public AddTabCommand AddTabCommand => _addTabCommand ??= new AddTabCommand(this);

        private readonly ObservableCollection<TabItem> _tabItems;
        public ReadOnlyObservableCollection<TabItem> TabsItems { get; }

        public bool HasNoTabs => _tabItems.Count == 0;
        public bool CanAddTabs => _tabItems.Count < 25;

        public TabControlViewModel()
        {
            _tabItems = new ObservableCollection<TabItem>(EnumerateTabs());
            TabsItems = new ReadOnlyObservableCollection<TabItem>(_tabItems);
            SelectedTab = TabsItems.FirstOrDefault();
        }

        public void AddTab(string header = null, string directory = null)
        {
            if (!CanAddTabs) return;

            var h = header ?? "New Tab";
            var d = directory ?? string.Empty;
            if (SelectedTab is { Header : "New Tab" })
            {
                SelectedTab.Header = h;
                SelectedTab.Directory = d;
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _tabItems.Add(new TabItem(h, d));
                SelectedTab = _tabItems.Last();
            });
        }

        public void RemoveTab(TabItem tab = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var tabCount = _tabItems.Count;
                var tabToDelete = tab ?? SelectedTab;
                switch (tabCount)
                {
                    case <= 0:
                        return;
                    // select previous tab before deleting current to avoid "ScrollToZero" issue on tab delete
                    case > 1:
                        SelectedTab = _tabItems.Previous(tabToDelete); // will select last if previous is -1 but who cares anyway, still better than having +1 to scroll 0
                        break;
                }

                _tabItems.Remove(tabToDelete);
                OnTabRemove?.Invoke(this, new TabEventArgs(tabToDelete));
            });
        }

        public class TabEventArgs : EventArgs
        {
            public TabItem TabToRemove { get; set; }
            public TabEventArgs(TabItem tab) { TabToRemove = tab; }
        }
        public event EventHandler OnTabRemove;
        public void GoLeftTab() => SelectedTab = _tabItems.Previous(SelectedTab);
        public void GoRightTab() => SelectedTab = _tabItems.Next(SelectedTab);

        public void RemoveOtherTabs(TabItem tab)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var t in _tabItems.Where(t => t != tab).ToList())
                {
                    _tabItems.Remove(t);
                }
            });
        }

        public void RemoveAllTabs()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedTab = null;
                _tabItems.Clear();
            });
        }

        private static IEnumerable<TabItem> EnumerateTabs()
        {
            yield return new TabItem("New Tab", string.Empty);
        }
    }
}
