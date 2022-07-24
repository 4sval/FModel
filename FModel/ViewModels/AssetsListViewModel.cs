using System.ComponentModel;
using System.Windows.Data;
using CUE4Parse.Compression;
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

    private string _package;
    public string Package
    {
        get => _package;
        private set => SetProperty(ref _package, value);
    }

    private CompressionMethod _compression;
    public CompressionMethod Compression
    {
        get => _compression;
        private set => SetProperty(ref _compression, value);
    }

    public AssetItem(string fullPath, bool isEncrypted, long offset, long size, string package, CompressionMethod compression)
    {
        FullPath = fullPath;
        IsEncrypted = isEncrypted;
        Offset = offset;
        Size = size;
        Package = package;
        Compression = compression;
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