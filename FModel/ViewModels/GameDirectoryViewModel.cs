using FModel.Framework;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Vfs;

namespace FModel.ViewModels
{
    public class FileItem : ViewModel
    {
        private string _name;
        public string Name
        {
            get => _name;
            private set => SetProperty(ref _name, value);
        }

        private long _length;
        public long Length
        {
            get => _length;
            private set => SetProperty(ref _length, value);
        }

        private int _fileCount;
        public int FileCount
        {
            get => _fileCount;
            set => SetProperty(ref _fileCount, value);
        }

        private string _mountPoint;
        public string MountPoint
        {
            get => _mountPoint;
            set => SetProperty(ref _mountPoint, value);
        }

        private bool _isEncrypted;
        public bool IsEncrypted
        {
            get => _isEncrypted;
            set => SetProperty(ref _isEncrypted, value);
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private string _key;
        public string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        private FGuid _guid;
        public FGuid Guid
        {
            get => _guid;
            set => SetProperty(ref _guid, value);
        }

        public FileItem(string name, long length)
        {
            Name = name;
            Length = length;
        }

        public override string ToString()
        {
            return $"{Name} | {Key}";
        }
    }

    public class GameDirectoryViewModel : ViewModel
    {
        public readonly ObservableCollection<FileItem> DirectoryFiles;
        public ICollectionView DirectoryFilesView { get; }

        public GameDirectoryViewModel()
        {
            DirectoryFiles = new ObservableCollection<FileItem>();
            DirectoryFilesView = new ListCollectionView(DirectoryFiles) {SortDescriptions = {new SortDescription("Name", ListSortDirection.Ascending)}};
        }

        public void DeactivateAll()
        {
            foreach (var file in DirectoryFiles)
            {
                file.IsEnabled = false;
            }
        }

        public void Add(IAesVfsReader reader)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DirectoryFiles.Add(new FileItem(reader.Name, reader.Length)
                {
                    Guid = reader.EncryptionKeyGuid,
                    IsEncrypted = reader.IsEncrypted,
                    IsEnabled = false,
                    Key = string.Empty
                });
            });
        }
    }
}