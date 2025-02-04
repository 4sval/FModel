using System.ComponentModel;
using System.Windows.Data;
using CUE4Parse.Compression;
using CUE4Parse.Utils;
using FModel.Framework;

namespace FModel.ViewModels;

public class AssetItem : ViewModel
{
    private string _fullPath;
    public string FullPath
    {
        get => _fullPath;
        private set => SetProperty(ref _fullPath, value);
    }

    private bool _isEncrypted;
    public bool IsEncrypted
    {
        get => _isEncrypted;
        private set => SetProperty(ref _isEncrypted, value);
    }

    private long _offset;
    public long Offset
    {
        get => _offset;
        private set => SetProperty(ref _offset, value);
    }

    private long _size;
    public long Size
    {
        get => _size;
        private set => SetProperty(ref _size, value);
    }

    private string _archive;
    public string Archive
    {
        get => _archive;
        private set => SetProperty(ref _archive, value);
    }

    private CompressionMethod _compression;
    public CompressionMethod Compression
    {
        get => _compression;
        private set => SetProperty(ref _compression, value);
    }

    private string _directory;
    public string Directory
    {
        get => _directory;
        private set => SetProperty(ref _directory, value);
    }

    private string _fileName;
    public string FileName
    {
        get => _fileName;
        private set => SetProperty(ref _fileName, value);
    }

    private string _extension;
    public string Extension
    {
        get => _extension;
        private set => SetProperty(ref _extension, value);
    }

    public AssetItem(string titleExtra, AssetItem asset) : this(asset.FullPath, asset.IsEncrypted, asset.Offset, asset.Size, asset.Archive, asset.Compression)
    {
        FullPath += titleExtra;
    }

    public AssetItem(string fullPath, bool isEncrypted = false, long offset = 0, long size = 0, string archive = "", CompressionMethod compression = CompressionMethod.None)
    {
        FullPath = fullPath;
        IsEncrypted = isEncrypted;
        Offset = offset;
        Size = size;
        Archive = archive;
        Compression = compression;

        Directory = FullPath.SubstringBeforeLast('/');
        FileName = FullPath.SubstringAfterLast('/');
        Extension = FullPath.SubstringAfterLast('.').ToLowerInvariant();
    }

    public override string ToString() => FullPath;
}

public class AssetsListViewModel
{
    public RangeObservableCollection<AssetItem> Assets { get; }
    public ICollectionView AssetsView { get; }

    public AssetsListViewModel()
    {
        Assets = new RangeObservableCollection<AssetItem>();
        AssetsView = new ListCollectionView(Assets)
        {
            SortDescriptions = { new SortDescription("FullPath", ListSortDirection.Ascending) }
        };
    }
}
