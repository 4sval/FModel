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
            {
                if (value == EAssetCategory.All)
                {
                    RefreshFilters();
                }
                else
                {
                    _ = OnSelectedCategoryChanged();
                }
            }
        }
    }

    public string PathAtThisPoint { get; }
    public AssetsListViewModel AssetsList { get; } = new();
    public RangeObservableCollection<TreeItem> Folders { get; } = [];

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
    public CompositeCollection CombinedEntries => _combinedEntries ??=
    [
        new CollectionContainer { Collection = FilteredFoldersView },
        new CollectionContainer { Collection = AssetsList.AssetsView }
    ];

    public TreeItem Parent { get; init; }
    public int Depth => Parent?.Depth + 1 ?? 0;
    public Thickness Indent => new(Depth * 16, 0, 0, 0); // For folder tree indentation

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

public class AssetsFolderViewModel : ViewModel
{
    public RangeObservableCollection<TreeItem> Folders { get; } = [];
    public RangeObservableCollection<TreeItem> VisibleFolders { get; } = [];

    private TreeItem? _selectedFolder;
    public TreeItem? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            var previous = _selectedFolder;
            if (!SetProperty(ref _selectedFolder, value))
                return;

            previous?.IsSelected = false;
            value?.IsSelected = true;
        }
    }

    public void Clear()
    {
        SelectedFolder = null;
        VisibleFolders.Clear();
        Folders.Clear();
    }

    public void CollapseAll()
    {
        static void CollapseChildren(TreeItem folder)
        {
            folder.IsExpanded = false;
            foreach (var child in folder.Folders)
            {
                CollapseChildren(child);
            }
        }

        var selected = SelectedFolder;
        while (selected?.Parent != null)
            selected = selected.Parent;

        foreach (var folder in Folders)
        {
            CollapseChildren(folder);
        }

        SelectedFolder = selected;
        for (var i = VisibleFolders.Count - 1; i >= 0; i--)
        {
            if (VisibleFolders[i].Parent != null)
            {
                VisibleFolders.RemoveAt(i);
            }
        }
    }

    public void Expand(TreeItem folder)
    {
        if (folder.Folders.Count == 0)
            return;

        var index = VisibleFolders.IndexOf(folder);
        if (index < 0)
            return;

        if (index + 1 < VisibleFolders.Count && ReferenceEquals(VisibleFolders[index + 1].Parent, folder))
        {
            folder.IsExpanded = true;
            return;
        }

        foreach (var child in EnumerateVisibleChildren(folder))
        {
            VisibleFolders.Insert(++index, child);
        }

        folder.IsExpanded = true;
    }

    public void Collapse(TreeItem folder)
    {
        if (!folder.IsExpanded)
            return;

        var index = VisibleFolders.IndexOf(folder);
        if (index < 0)
            return;

        var selected = SelectedFolder;
        var start = index + 1;
        var count = 0;
        while (start + count < VisibleFolders.Count && VisibleFolders[start + count].Depth > folder.Depth)
            count++;

        if (selected != null && IsDescendantOf(selected, folder))
        {
            SelectedFolder = folder;
        }

        for (var i = start + count - 1; i >= start; i--)
        {
            VisibleFolders.RemoveAt(i);
        }

        folder.IsExpanded = false;
    }

    public void Toggle(TreeItem folder)
    {
        if (folder.IsExpanded)
        {
            Collapse(folder);
        }
        else
        {
            Expand(folder);
        }
    }

    public void Reveal(TreeItem folder)
    {
        var parents = new Stack<TreeItem>();

        for (var parent = folder.Parent; parent != null; parent = parent.Parent)
            parents.Push(parent);

        while (parents.Count > 0)
            Expand(parents.Pop());

        SelectedFolder = folder;
    }

    private static bool IsDescendantOf(TreeItem item, TreeItem parent)
    {
        for (var current = item.Parent; current != null; current = current.Parent)
        {
            if (ReferenceEquals(current, parent))
                return true;
        }

        return false;
    }

    private static IEnumerable<TreeItem> EnumerateVisibleChildren(TreeItem folder)
    {
        foreach (var child in folder.Folders)
        {
            yield return child;

            if (!child.IsExpanded)
                continue;

            foreach (var descendant in EnumerateVisibleChildren(child))
            {
                yield return descendant;
            }
        }
    }

    private static void SortFolders(RangeObservableCollection<TreeItem> folders)
    {
        if (folders.Count > 1)
        {
            folders.ReplaceRange([.. folders.OrderBy(x => x.Header, StringComparer.OrdinalIgnoreCase)]);
        }

        foreach (var folder in folders)
        {
            SortFolders(folder.Folders);
        }
    }

    public void BulkPopulate(IReadOnlyCollection<GameFile> entries)
    {
        if (entries == null || entries.Count == 0)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var treeItems = new RangeObservableCollection<TreeItem>();
            treeItems.SetSuppressionState(true);

            static TreeItem FindByHeaderOrNull(IReadOnlyList<TreeItem> list, string header)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].Header == header)
                        return list[i];
                }

                return null;
            }

            foreach (var entry in entries)
            {
                TreeItem lastNode = null;
                TreeItem parentItem = null;

                var folders = entry.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var builder = new StringBuilder(64);
                var parentNode = treeItems;

                if (folders.Length <= 1)
                {
                    var rootNode = FindByHeaderOrNull(treeItems, "Content");
                    if (rootNode == null)
                    {
                        rootNode = new TreeItem("Content", entry, "Content")
                        {
                            Parent = null
                        };

                        rootNode.Folders.SetSuppressionState(true);
                        rootNode.AssetsList.Assets.SetSuppressionState(true);
                        treeItems.Add(rootNode);
                    }

                    rootNode.AssetsList.Add(entry);
                    continue;
                }

                for (var i = 0; i < folders.Length - 1; i++)
                {
                    var folder = folders[i];
                    builder.Append(folder).Append('/');
                    lastNode = FindByHeaderOrNull(parentNode, folder);

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

            SortFolders(treeItems);

            Folders.AddRange(treeItems);
            VisibleFolders.AddRange(treeItems);

            if (treeItems.Count > 0)
            {
                var projectName = ApplicationService.ApplicationView.CUE4Parse.Provider.ProjectName;
                SelectedFolder = treeItems.FirstOrDefault(x => x.Header.Equals(projectName, StringComparison.OrdinalIgnoreCase)) ?? treeItems[0];
            }

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
