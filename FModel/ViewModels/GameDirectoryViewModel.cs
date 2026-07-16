using FModel.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
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
    private readonly record struct FileItemUpdate(FileItem File, bool IsEnabled, string MountPoint,
        int FileCount, bool HasMountInfo);

    public readonly RangeObservableCollection<FileItem> DirectoryFiles;

    public ICollectionView DirectoryFilesView { get; }

    private readonly Regex _hiddenArchives = ArchivesRegex();
    private readonly ConcurrentDictionary<IAesVfsReader, FileItem> _filesByReader =
        new(ReferenceEqualityComparer.Instance);
    private readonly ConcurrentQueue<FileItem> _pendingAdditions = new();
    private readonly ConcurrentQueue<FileItemUpdate> _pendingUpdates = new();
    private FileItem _looseFilesContainer;
    private int _pendingLooseFileCount;
    private int _publishScheduled;

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
        if (!_filesByReader.TryAdd(reader, fileItem))
            return;

        _pendingAdditions.Enqueue(fileItem);
        SchedulePublish();
    }

    public void AddLooseFiles(int fileCount)
    {
        if (fileCount < 1)
            return;

        Interlocked.Add(ref _pendingLooseFileCount, fileCount);
        SchedulePublish();
    }

    public void Verify(IAesVfsReader reader)
    {
        if (!_filesByReader.TryGetValue(reader, out var file)) return;

        _pendingUpdates.Enqueue(new FileItemUpdate(file, true, reader.MountPoint, reader.FileCount, true));
        SchedulePublish();
    }

    public void Disable(IAesVfsReader reader)
    {
        if (!_filesByReader.TryGetValue(reader, out var file)) return;

        _pendingUpdates.Enqueue(new FileItemUpdate(file, false, string.Empty, 0, false));
        SchedulePublish();
    }

    public void FlushPendingChanges()
    {
        if (Application.Current.Dispatcher.CheckAccess())
            PublishPendingChanges();
        else
            Application.Current.Dispatcher.Invoke(PublishPendingChanges);
    }

    private void SchedulePublish()
    {
        if (Interlocked.CompareExchange(ref _publishScheduled, 1, 0) != 0)
            return;

        _ = Application.Current.Dispatcher.BeginInvoke(PublishPendingChanges, DispatcherPriority.Background);
    }

    private void PublishPendingChanges()
    {
        var additions = new List<FileItem>();
        while (_pendingAdditions.TryDequeue(out var file))
            additions.Add(file);

        var looseFileCount = Interlocked.Exchange(ref _pendingLooseFileCount, 0);
        if (looseFileCount > 0)
        {
            if (_looseFilesContainer is null)
            {
                _looseFilesContainer = new FileItem("Loose Files", looseFileCount, 0, true);
                additions.Add(_looseFilesContainer);
            }
            else
            {
                _looseFilesContainer.FileCount += looseFileCount;
            }
        }

        if (additions.Count > 0)
            DirectoryFiles.AddRange(additions);

        while (_pendingUpdates.TryDequeue(out var update))
        {
            update.File.IsEnabled = update.IsEnabled;
            if (!update.HasMountInfo)
                continue;

            update.File.MountPoint = update.MountPoint;
            update.File.FileCount = update.FileCount;
        }

        Interlocked.Exchange(ref _publishScheduled, 0);
        if (!_pendingAdditions.IsEmpty || !_pendingUpdates.IsEmpty || Volatile.Read(ref _pendingLooseFileCount) > 0)
            SchedulePublish();
    }

    [GeneratedRegex(@"^(?!global|pakchunk.+(optional|ondemand)\-).+(pak|utoc)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ArchivesRegex();
}
