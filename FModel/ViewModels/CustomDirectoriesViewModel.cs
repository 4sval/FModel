using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FModel.Framework;
using FModel.Settings;
using FModel.ViewModels.Commands;

namespace FModel.ViewModels
{
    public class CustomDirectory : ViewModel
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

        public CustomDirectory()
        {
            Header = string.Empty;
            DirectoryPath = string.Empty;
        }

        public CustomDirectory(string header, string path)
        {
            Header = header;
            DirectoryPath = path;
        }

        public override string ToString() => Header;
    }

    public class CustomDirectoriesViewModel : ViewModel
    {
        private GoToCommand _goToCommand;
        public GoToCommand GoToCommand => _goToCommand ??= new GoToCommand(this);
        private AddEditDirectoryCommand _addEditDirectoryCommand;
        public AddEditDirectoryCommand AddEditDirectoryCommand => _addEditDirectoryCommand ??= new AddEditDirectoryCommand(this);
        private DeleteDirectoryCommand _deleteDirectoryCommand;
        public DeleteDirectoryCommand DeleteDirectoryCommand => _deleteDirectoryCommand ??= new DeleteDirectoryCommand(this);

        public readonly ObservableCollection<Control> _directories;
        public ReadOnlyObservableCollection<Control> Directories { get; }

        private readonly FGame _game;

        public CustomDirectoriesViewModel(FGame game)
        {
            _game = game;
            _directories = new ObservableCollection<Control>(EnumerateDirectories());
            Directories = new ReadOnlyObservableCollection<Control>(_directories);
        }

        public int GetIndex(CustomDirectory dir)
        {
            return _directories.IndexOf(_directories.FirstOrDefault(x =>
                x is MenuItem m && m.Header.ToString() == dir.Header && m.Tag.ToString() == dir.DirectoryPath));
        }

        public void Add(CustomDirectory dir)
        {
            _directories.Add(new MenuItem {Header = dir.Header, Tag = dir.DirectoryPath, ItemsSource = EnumerateCommands(dir)});
        }

        public void Edit(int index, CustomDirectory newDir)
        {
            if (_directories.ElementAt(index) is not MenuItem dir) return;

            dir.Header = newDir.Header;
            dir.Tag = newDir.DirectoryPath;
        }

        public void Delete(int index)
        {
            _directories.RemoveAt(index);
        }

        public void Save()
        {
            UserSettings.Default.CustomDirectories[_game] = new List<CustomDirectory>();
            for (var i = 2; i < _directories.Count; i++)
            {
                if (_directories[i] is not MenuItem m) continue;
                UserSettings.Default.CustomDirectories[_game].Add(new CustomDirectory(m.Header.ToString(), m.Tag.ToString()));
            }
        }

        private IEnumerable<Control> EnumerateDirectories()
        {
            yield return new MenuItem
            {
                Header = "Add Directory",
                Icon = new Image {Source = new BitmapImage(new Uri("/FModel;component/Resources/add_directory.png", UriKind.Relative))},
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                Command = AddEditDirectoryCommand
            };
            yield return new Separator();

            foreach (var setting in UserSettings.Default.CustomDirectories[_game])
            {
                if (setting.DirectoryPath.EndsWith('/'))
                    setting.DirectoryPath = setting.DirectoryPath[..^1];

                yield return new MenuItem
                {
                    Header = setting.Header,
                    Tag = setting.DirectoryPath,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    ItemsSource = EnumerateCommands(setting)
                };
            }
        }

        private IEnumerable<MenuItem> EnumerateCommands(CustomDirectory dir)
        {
            yield return new MenuItem
            {
                Header = "Go To",
                Icon = new Image {Source = new BitmapImage(new Uri("/FModel;component/Resources/go_to_directory.png", UriKind.Relative))},
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                Command = GoToCommand,
                CommandParameter = dir.DirectoryPath
            };
            yield return new MenuItem
            {
                Header = "Edit Directory",
                Icon = new Image {Source = new BitmapImage(new Uri("/FModel;component/Resources/edit.png", UriKind.Relative))},
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                Command = AddEditDirectoryCommand,
                CommandParameter = dir
            };
            yield return new MenuItem
            {
                Header = "Delete Directory",
                StaysOpenOnClick = true,
                Icon = new Image {Source = new BitmapImage(new Uri("/FModel;component/Resources/delete.png", UriKind.Relative))},
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                Command = DeleteDirectoryCommand,
                CommandParameter = dir
            };
        }
    }
}