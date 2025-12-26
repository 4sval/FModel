using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;

namespace FModel.ViewModels;

public class TreeItem : ViewModel
{
    private readonly string _header;
    public string Header
    {
        get => _header;
        private init => SetProperty(ref _header, value);
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private string _archive;
    public string Archive
    {
        get => _archive;
        private set => SetProperty(ref _archive, value);
    }

    private string _mountPoint;
    public string MountPoint
    {
        get => _mountPoint;
        private set => SetProperty(ref _mountPoint, value);
    }

    private FPackageFileVersion _version;
    public FPackageFileVersion Version
    {
        get => _version;
        private set => SetProperty(ref _version, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RefreshFilters();
            }
        }
    }

    private EAssetCategory _selectedCategory = EAssetCategory.All;
    public EAssetCategory SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                _ = OnSelectedCategoryChanged();
        }
    }

    public string PathAtThisPoint { get; }
    public AssetsListViewModel AssetsList { get; } = new();
    public RangeObservableCollection<TreeItem> Folders { get; } = [];

    private ICollectionView _foldersView;
    public ICollectionView FoldersView
    {
        get
        {
            _foldersView ??= new ListCollectionView(Folders)
            {
                SortDescriptions = { new SortDescription(nameof(Header), ListSortDirection.Ascending) }
            };
            return _foldersView;
        }
    }

    private ICollectionView? _filteredFoldersView;
    public ICollectionView? FilteredFoldersView
    {
        get
        {
            _filteredFoldersView ??= new ListCollectionView(Folders)
            {
                SortDescriptions = { new SortDescription(nameof(Header), ListSortDirection.Ascending) },
                Filter = e => ItemFilter(e, SearchText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            };
            return _filteredFoldersView;
        }
    }

    private CompositeCollection _combinedEntries;
    public CompositeCollection CombinedEntries
    {
        get
        {
            if (_combinedEntries == null)
            {
                void CreateCombinedEntries()
                {
                    _combinedEntries = new CompositeCollection
                    {
                        new CollectionContainer { Collection = FilteredFoldersView },
                        new CollectionContainer { Collection = AssetsList.AssetsView }
                    };
                }

                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(CreateCombinedEntries);
                }
                else
                {
                    CreateCombinedEntries();
                }
            }
            return _combinedEntries;
        }
    }

    public TreeItem Parent { get; init; }

    public TreeItem(string header, GameFile entry, string pathHere)
    {
        Header = header;
        if (entry is VfsEntry vfsEntry)
        {
            Archive = vfsEntry.Vfs.Name;
            MountPoint = vfsEntry.Vfs.MountPoint;
            Version = vfsEntry.Vfs.Ver;
        }
        PathAtThisPoint = pathHere;

        AssetsList.AssetsView.Filter = o => ItemFilter(o, SearchText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private void RefreshFilters()
    {
        AssetsList.AssetsView.Refresh();
        FilteredFoldersView?.Refresh();
    }

    private bool ItemFilter(object item, IEnumerable<string> filters)
    {
        var f = filters.ToArray();
        switch (item)
        {
            case GameFileViewModel entry:
            {
                bool matchesSearch = f.Length == 0 || f.All(x => entry.Asset.Name.Contains(x, StringComparison.OrdinalIgnoreCase));
                bool matchesCategory = SelectedCategory == EAssetCategory.All || entry.AssetCategory.IsOfCategory(SelectedCategory);

                return matchesSearch && matchesCategory;
            }
            case TreeItem folder:
            {
                bool matchesSearch = f.Length == 0 || f.All(x => folder.Header.Contains(x, StringComparison.OrdinalIgnoreCase));
                bool matchesCategory = SelectedCategory == EAssetCategory.All;

                return matchesSearch && matchesCategory;
            }
        }
        return false;
    }

    private async Task OnSelectedCategoryChanged()
    {
        await Task.WhenAll(AssetsList.Assets.Select(asset => asset.ResolveAsync(EResolveCompute.Category)));
        RefreshFilters();
    }

    public override string ToString() => $"{Header} | {Folders.Count} Folders | {AssetsList.Assets.Count} Files";
}

public class AssetsFolderViewModel
{
    public RangeObservableCollection<TreeItem> Folders { get; }
    public ICollectionView FoldersView { get; }

    public AssetsFolderViewModel()
    {
        Folders = [];
        FoldersView = new ListCollectionView(Folders) { SortDescriptions = { new SortDescription("Header", ListSortDirection.Ascending) } };
    }

    public void BulkPopulate(IReadOnlyCollection<GameFile> entries)
    {
        if (entries == null || entries.Count == 0)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var treeItems = new RangeObservableCollection<TreeItem>();
            treeItems.SetSuppressionState(true);

            foreach (var entry in entries)
            {
                TreeItem lastNode = null;
                TreeItem parentItem = null;
                var folders = entry.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var builder = new StringBuilder(64);
                var parentNode = treeItems;

                for (var i = 0; i < folders.Length - 1; i++)
                {
                    var folder = folders[i];
                    builder.Append(folder).Append('/');
                    lastNode = FindByHeaderOrNull(parentNode, folder);

                    static TreeItem FindByHeaderOrNull(IReadOnlyList<TreeItem> list, string header)
                    {
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (list[i].Header == header)
                                return list[i];
                        }

                        return null;
                    }

                    if (lastNode == null)
                    {
                        var nodePath = builder.ToString();
                        lastNode = new TreeItem(folder, entry, nodePath[..^1])
                        {
                            Parent = parentItem
                        };
                        lastNode.Folders.SetSuppressionState(true);
                        lastNode.AssetsList.Assets.SetSuppressionState(true);
                        parentNode.Add(lastNode);
                    }

                    parentItem = lastNode;
                    parentNode = lastNode.Folders;
                }

                lastNode?.AssetsList.Add(entry);
            }

            if (treeItems.Count > 0)
            {
                var projectName = ApplicationService.ApplicationView.CUE4Parse.Provider.ProjectName;
                (treeItems.FirstOrDefault(x => x.Header.Equals(projectName, StringComparison.OrdinalIgnoreCase)) ?? treeItems[0]).IsSelected = true;
            }

            Folders.AddRange(treeItems);
            ApplicationService.ApplicationView.CUE4Parse.SearchVm.ChangeCollection(entries);

            foreach (var folder in Folders)
                InvokeOnCollectionChanged(folder);

            static void InvokeOnCollectionChanged(TreeItem item)
            {
                item.Folders.SetSuppressionState(false);
                item.AssetsList.Assets.SetSuppressionState(false);

                if (item.Folders.Count != 0)
                {
                    item.Folders.InvokeOnCollectionChanged();

                    foreach (var folderItem in item.Folders)
                        InvokeOnCollectionChanged(folderItem);
                }

                if (item.AssetsList.Assets.Count != 0)
                    item.AssetsList.Assets.InvokeOnCollectionChanged();
            }
        });
    }
}
