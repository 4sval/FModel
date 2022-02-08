using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using FModel.Framework;
using FModel.Settings;
using FModel.ViewModels.Commands;

namespace FModel.ViewModels
{
    public class Bookmark : ViewModel
    {
        private string _header;
        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        private string _directoryPath;
        public string DirectoryPath
        {
            get => _directoryPath;
            set => SetProperty(ref _directoryPath, value);
        }

        private string _aesKey;
        public string AESKey
        {
            get => _aesKey;
            set => SetProperty(ref _aesKey, value);
        }

        private FGame _fgame;
        public FGame FGame
        {
            get => _fgame;
            set => SetProperty(ref _fgame, value);
        }

        public Bookmark()
        {
            Header = string.Empty;
            DirectoryPath = string.Empty;
            AESKey = string.Empty;
            FGame = FGame.Unknown;
        }

        public Bookmark(string header, string path, string key, FGame game)
        {
            Header = header;
            DirectoryPath = path;
            AESKey = key;
            FGame = game;
        }
    }

    public class BookmarkList : ViewModel
    {
        private string _selectedBM;
        private readonly ObservableCollection<string> _allBookmarks;

        public string SelectedBM
        {
            get => _selectedBM;
            set => SetProperty(ref _selectedBM, value);
        }
        public ReadOnlyObservableCollection<string> AllBookmarks { get; }

        public BookmarkList(IDictionary<string, Bookmark> bookmarks)
        {
            _allBookmarks = new ObservableCollection<string>(bookmarks.Keys.ToArray());
            SelectedBM = _allBookmarks.FirstOrDefault();
            AllBookmarks = new ReadOnlyObservableCollection<string>(_allBookmarks);;
        }
    }

    public class BookmarksViewModel : ViewModel
    {
        //private data
        private readonly FGame _game;
        private bool _anyBookmark;
        private GoToBookmarkCommand _goToCommand;
        private AddBookmarkCommand _addBookmarkCommand;
        private DeleteBookmarkCommand _deleteBookmarkCommand;
        private readonly ObservableCollection<Control> _directories;

        //public data
        public GoToBookmarkCommand GoToCommand => _goToCommand ??= new GoToBookmarkCommand(this);
        public AddBookmarkCommand AddBookmarkCommand => _addBookmarkCommand ??= new AddBookmarkCommand(this);
        public DeleteBookmarkCommand DeleteBookmarkCommand => _deleteBookmarkCommand ??= new DeleteBookmarkCommand(this);
        public ReadOnlyObservableCollection<Control> Directories { get; }

        //logic
        public bool AnyBookmark
        {
            get => _anyBookmark;
            set => SetProperty(ref _anyBookmark, value);
        }
        
        public BookmarksViewModel(FGame game)
        {
            _game = game;
            _anyBookmark = UserSettings.Default.Bookmarks.Count > 0;
            _directories = new ObservableCollection<Control>(EnumerateBookmarks());
            Directories = new ReadOnlyObservableCollection<Control>(_directories);
        }

        public void Add(Bookmark bmark)
        {
            bmark.Header = bmark.Header.Trim();
            if (!UserSettings.Default.Bookmarks.ContainsKey(bmark.Header))
                _directories.Add(new MenuItem { Header = bmark.Header, Tag = bmark.DirectoryPath, Command = GoToCommand, CommandParameter = bmark.Header });
            UserSettings.Default.Bookmarks[bmark.Header] = bmark;
            AnyBookmark = UserSettings.Default.Bookmarks.Count > 0;
        }

        public void Delete(string key)
        {
            UserSettings.Default.Bookmarks.Remove(key);
            for (int i = _directories.Count - 1; i >= 0; i--)
                if (_directories[i] is MenuItem m && m.Header.ToString() == key)
                {
                    _directories.RemoveAt(i);
                    break;
                }
            AnyBookmark = UserSettings.Default.Bookmarks.Count > 0;
        }

        public void GoTo(string entry, ApplicationViewModel appviewcontext)
        {
            var bmark = UserSettings.Default.Bookmarks[entry];
            UserSettings.Default.GameDirectory = bmark.DirectoryPath;
            UserSettings.Default.AesKeys[bmark.FGame].MainKey = bmark.AESKey;
            appviewcontext.RestartWithWarning();
        }

        private IEnumerable<Control> EnumerateBookmarks()
        {
            var addMenu = new MenuItem
            {
                Header = "Add Bookmark",
                Icon = new Image {Source = new BitmapImage(new Uri("/FModel;component/Resources/add_directory.png", UriKind.Relative))},
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                Command = AddBookmarkCommand,
                CommandParameter = _game
            };
            var delMenu = new MenuItem
            {
                Header = "Remove Bookmark",
                Icon = new Image { Source = new BitmapImage(new Uri("/FModel;component/Resources/delete.png", UriKind.Relative)) },
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                IsEnabled = AnyBookmark,
                DataContext = this,
                Command = DeleteBookmarkCommand
            };
            Binding myBinding = new Binding() { Source = this, Path = new PropertyPath("AnyBookmark"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            BindingOperations.SetBinding(delMenu, UIElement.IsEnabledProperty, myBinding);

            yield return addMenu;
            yield return delMenu;
            yield return new Separator();

            foreach (var bmKV in UserSettings.Default.Bookmarks)
            {
                yield return new MenuItem
                {
                    Header = bmKV.Key,
                    Tag = bmKV.Value.DirectoryPath,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Command = GoToCommand,
                    CommandParameter = bmKV.Key
                };
            }
        }
    }
}
