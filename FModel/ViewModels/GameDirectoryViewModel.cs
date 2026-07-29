using FModel.Framework;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using CUE4Parse.Compression;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.VirtualFileSystem;

namespace FModel.ViewModels;

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

    private bool _isLooseFilesContainer;
    public bool IsLooseFilesContainer
    {
        get => _isLooseFilesContainer;
        set => SetProperty(ref _isLooseFilesContainer, value);
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

    private CompressionMethod[] _compressionMethods;
    public CompressionMethod[] CompressionMethods
    {
        get => _compressionMethods;
        set => SetProperty(ref  _compressionMethods, value);
    }

    public FileItem(string name, long length)
    {
        Name = name;
        Length = length;
    }

    public FileItem(string name, int fileCount, long length, bool isLooseFile)
    {
        Name = name;
        Length = length;
        FileCount = fileCount;
        IsLooseFilesContainer = isLooseFile;
        IsEnabled = true;
        Key = string.Empty;
        MountPoint = string.Empty;
        CompressionMethods = [];
    }

    public FileItem(IAesVfsReader reader)
    {
        Name = reader.Name;
        Length = reader.Length;
        Guid = reader.EncryptionKeyGuid;
        IsEncrypted = reader.IsEncrypted;
        IsEnabled = false;
        IsLooseFilesContainer = false;
        Key = string.Empty;
        FileCount = reader is IoStoreReader storeReader ? (int) storeReader.TocResource.Header.TocEntryCount - 1 : 0;
        CompressionMethods = reader.CompressionMethods;
    }

    public override string ToString()
    {
        return $"{Name} | {Key}";
    }
}

public partial class GameDirectoryViewModel : ViewModel
{
    public readonly ObservableCollection<FileItem> DirectoryFiles;

    public ICollectionView DirectoryFilesView { get; }

    private readonly Regex _hiddenArchives = ArchivesRegex();

    public GameDirectoryViewModel()
    {
        DirectoryFiles = [];
        DirectoryFilesView = new ListCollectionView(DirectoryFiles)
        {
            SortDescriptions =
            {
                new SortDescription(nameof(FileItem.IsLooseFilesContainer), ListSortDirection.Ascending),
                new SortDescription(nameof(FileItem.Name), ListSortDirection.Ascending)
            }
        };
    }

    public void Add(IAesVfsReader reader)
    {
        if (!_hiddenArchives.IsMatch(reader.Name)) return;

        var fileItem = new FileItem(reader);
        Application.Current.Dispatcher.Invoke(() => DirectoryFiles.Add(fileItem));
    }

    public void AddLooseFiles(int fileCount)
    {
        if (fileCount < 1)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var looseFilesContainer = DirectoryFiles.FirstOrDefault(x => x.IsLooseFilesContainer);
            if (looseFilesContainer is not null)
            {
                looseFilesContainer.FileCount += fileCount;
            }
            else
            {
                DirectoryFiles.Add(new FileItem("Loose Files", fileCount, 0, true));
            }
        });
    }

    public void Verify(IAesVfsReader reader)
    {
        if (DirectoryFiles.FirstOrDefault(x => x.Name == reader.Name) is not { } file) return;

        file.IsEnabled = true;
        file.MountPoint = reader.MountPoint;
        file.FileCount = reader.FileCount;
    }

    public void Disable(IAesVfsReader reader)
    {
        if (DirectoryFiles.FirstOrDefault(x => x.Name == reader.Name) is not { } file) return;
        file.IsEnabled = false;
    }

    [GeneratedRegex(@"^(?!global|pakchunk.+(optional|ondemand)\-).+(pak|utoc)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ArchivesRegex();
}
