using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
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
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                for (var parent = Parent; parent != null; parent = parent.Parent)
                {
                    parent.IsExpanded = true;
                }
            }
        }
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

    private Visibility _searchBarVisibility = Visibility.Collapsed;
    public Visibility SearchBarVisibility
    {
        get => _searchBarVisibility;
        set => SetProperty(ref _searchBarVisibility, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilters(_searchText);
        }
    }

    private EAssetCategory _selectedCategory = EAssetCategory.All;
    public EAssetCategory SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                ApplyFilters(SearchText);
        }
    }

    public string PathAtThisPoint { get; }
    public AssetsListViewModel AssetsList { get; }
    public RangeObservableCollection<TreeItem> Folders { get; }
    public ICollectionView FoldersView { get; }
    public ICollectionView FilteredFoldersView { get; }
    public CompositeCollection CombinedEntries { get; } = [];
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
        AssetsList = new AssetsListViewModel();
        Folders = [];
        FoldersView = new ListCollectionView(Folders) { SortDescriptions = { new SortDescription(nameof(Header), ListSortDirection.Ascending) } };
        // Separate folders view for combined entries because I don't want to filter folders tree
        FilteredFoldersView = new ListCollectionView(Folders) { SortDescriptions = { new SortDescription(nameof(Header), ListSortDirection.Ascending) } };
        CombinedEntries =
        [
            new CollectionContainer { Collection = FilteredFoldersView },
            new CollectionContainer { Collection = AssetsList.AssetsView }
        ];
        SearchBarVisibility = Visibility.Visible;
    }

    private void ApplyFilters(string filterText)
    {
        var filters = filterText
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        AssetsList.AssetsView.Filter = o =>
        {
            if (o is not GameFileViewModel entry)
                return false;

            bool matchesSearch = filters.Length == 0 || filters.All(x => entry.Asset.Name.Contains(x, StringComparison.OrdinalIgnoreCase));
            bool matchesCategory = SelectedCategory == EAssetCategory.All || entry.AssetCategory == SelectedCategory;

            return matchesSearch && matchesCategory;
        };
        AssetsList.AssetsView.Refresh();

        FilteredFoldersView.Filter = o =>
        {
            if (o is not TreeItem folder)
                return false;

            bool matchesSearch = filters.Length == 0 || filters.All(x => folder.Header.Contains(x, StringComparison.OrdinalIgnoreCase));

            return matchesSearch;
        };
        FilteredFoldersView.Refresh();
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

            Folders.AddRange(treeItems);
            ApplicationService.ApplicationView.CUE4Parse.SearchVm.SearchResults.AddRange(entries);

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
