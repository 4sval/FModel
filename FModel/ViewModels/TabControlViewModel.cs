using FModel.Framework;
using FModel.ViewModels.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FModel.Extensions;
using FModel.Settings;
using FModel.Views.Resources.Controls;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Serilog;

namespace FModel.ViewModels
{
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

        private BitmapImage _image;
        public BitmapImage Image
        {
            get => _image;
            set
            {
                if (_image == value) return;
                SetProperty(ref _image, value);
                RaisePropertyChanged("HasImage");
            }
        }

        private BitmapScalingMode _imageRender = BitmapScalingMode.Linear;
        public BitmapScalingMode ImageRender
        {
            get => _imageRender;
            set => SetProperty(ref _imageRender, value);
        }

        public bool HasImage => Image != null;
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
        }

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

        public void SaveProperty(bool autoSave)
        {
            var fileName = Path.ChangeExtension(Header, ".json");
            var directory = Path.Combine(UserSettings.Default.OutputDirectory, "Saves",
                UserSettings.Default.KeepDirectoryStructure == EEnabledDisabled.Enabled ? Directory : "", fileName).Replace('\\', '/');

            if (!autoSave)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Property",
                    FileName = fileName,
                    InitialDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Saves"),
                    Filter = "JSON Files (*.json)|*.json|INI Files (*.ini)|*.ini|XML Files (*.xml)|*.xml|All Files (*.*)|*.*"
                };

                if (!(bool) saveFileDialog.ShowDialog()) return;
                directory = saveFileDialog.FileName;
            }
            else
            {
                System.IO.Directory.CreateDirectory(directory.SubstringBeforeLast('/'));
            }

            Application.Current.Dispatcher.Invoke(() => File.WriteAllText(directory, Document.Text));
            SaveCheck(directory, fileName);
        }

        public void SaveImage(bool autoSave)
        {
            if (!HasImage) return;
            var fileName = Path.ChangeExtension(Header, ".png");
            var directory = Path.Combine(UserSettings.Default.OutputDirectory, "Textures",
                UserSettings.Default.KeepDirectoryStructure == EEnabledDisabled.Enabled ? Directory : "", fileName).Replace('\\', '/');

            if (!autoSave)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Texture",
                    FileName = fileName,
                    InitialDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Textures"),
                    Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*"
                };

                if (!(bool) saveFileDialog.ShowDialog()) return;
                directory = saveFileDialog.FileName;
            }
            else
            {
                System.IO.Directory.CreateDirectory(directory.SubstringBeforeLast('/'));
            }

            using var fileStream = new FileStream(directory, FileMode.Create);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(Image));
            encoder.Save(fileStream);
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                _tabItems.Add(new TabItem(header ?? "New Tab", directory ?? string.Empty));
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
            });
        }

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

        private IEnumerable<TabItem> EnumerateTabs()
        {
            yield return new TabItem("New Tab", string.Empty);
        }
    }
}