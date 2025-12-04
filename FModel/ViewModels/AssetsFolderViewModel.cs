using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using static FModel.ViewModels.GameFileViewModel;

namespace FModel.ViewModels;

public class TreeItem : ViewModel
{
    public IEnumerable<EAssetCategory> Categories { get; } =
        Enum.GetValues(typeof(EAssetCategory)).Cast<EAssetCategory>();

    private string _header;
    public string Header
    {
        get => _header;
        private set => SetProperty(ref _header, value);
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
                RefreshCombinedEntries();
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
                ApplySearchbarFilter(_searchText);
        }
    }

    private GameFileViewModel.EAssetCategory? _selectedCategory;
    public GameFileViewModel.EAssetCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                ApplySearchbarFilter(SearchText);
        }
    }

    public string PathAtThisPoint { get; }
    public AssetsListViewModel AssetsList { get; }
    public RangeObservableCollection<TreeItem> Folders { get; }
    public ICollectionView FoldersView { get; }
    public ObservableCollection<object> CombinedEntries { get; } = [];
    public ICollectionView CombinedView { get; }
    public TreeItem Parent { get; set; }

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
        CombinedEntries = [];
        CombinedView = CollectionViewSource.GetDefaultView(CombinedEntries);
    }

    public void RefreshCombinedEntries()
    {
        CombinedEntries.Clear();

        foreach (var f in FoldersView)
            CombinedEntries.Add(f);
        foreach (GameFile asset in AssetsList.Assets.OrderBy(a => a.Path)) // We want to keep the sorting but ignore the filter
            CombinedEntries.Add(new GameFileViewModel(asset));

        if (CombinedEntries.Count > 0)
            SearchBarVisibility = Visibility.Visible;
    }

    private void ApplySearchbarFilter(string filterText)
    {
        var filters = filterText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        AssetsList.AssetsView.Filter = o =>
            filters.Length == 0 || (o is GameFile entry && filters.All(x => entry.Name.Contains(x, StringComparison.OrdinalIgnoreCase)));

        CombinedView.Filter = o =>
        {
            if (filters.Length == 0 && SelectedCategory == EAssetCategory.All)
                return true;

            return o switch
            {
                GameFileViewModel assetVm =>
                (filters.Length == 0 || filters.All(t => assetVm.Asset.Name.Contains(t, StringComparison.OrdinalIgnoreCase))) &&
                (SelectedCategory == EAssetCategory.All || assetVm.AssetCategory == SelectedCategory),
                TreeItem folderItem => filters.All(t => folderItem.Header.Contains(t, StringComparison.OrdinalIgnoreCase)),
                _ => true
            };
        };
    }

    public static void SelectTreeItem(TreeItem item, TreeView treeView)
    {
        if (item == null)
            return;

        for (var parent = item.Parent; parent != null; parent = parent.Parent)
            parent.IsExpanded = true;

        item.IsSelected = true;

        if (treeView.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem tvi)
        {
            tvi.BringIntoView();
        }
    }

    public override string ToString() => $"{Header} | {Folders.Count} Folders | {AssetsList.Assets.Count} Files";
}

public class AssetsFolderViewModel
{
    public RangeObservableCollection<TreeItem> Folders { get; }
    public ICollectionView FoldersView { get; }

    public AssetsFolderViewModel()
    {
        Folders = new RangeObservableCollection<TreeItem>();
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

                lastNode?.AssetsList.Assets.Add(entry);
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
