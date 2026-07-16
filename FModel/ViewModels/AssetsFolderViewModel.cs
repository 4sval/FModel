using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

public sealed class TreeItem : ViewModel
{
    public string Header { get; }

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

    public string Archive { get; }
    public string MountPoint { get; }
    public FPackageFileVersion Version { get; }

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

        AssetsList.SetFilter(o => ItemFilter(o, SearchText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)));
    }

    private void RefreshFilters()
    {
        AssetsList.RefreshView();
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

    public override string ToString() => $"{Header} | {Folders.Count} Folders | {AssetsList.Count} Files";
}

public class AssetsFolderViewModel
{
    private Dictionary<string, TreeItem> _foldersByPath = new(StringComparer.Ordinal);

    public RangeObservableCollection<TreeItem> Folders { get; }
    public ICollectionView FoldersView { get; }

    public AssetsFolderViewModel()
    {
        Folders = [];
        FoldersView = new ListCollectionView(Folders) { SortDescriptions = { new SortDescription("Header", ListSortDirection.Ascending) } };
    }

    public void Clear()
    {
        Folders.Clear();
        _foldersByPath = new Dictionary<string, TreeItem>(StringComparer.Ordinal);
    }

    public bool TryGetFolder(string directory, out TreeItem folder)
    {
        folder = null;
        if (string.IsNullOrEmpty(directory))
            return false;

        directory = directory.TrimEnd(Path.AltDirectorySeparatorChar);
        if (_foldersByPath.TryGetValue(directory, out folder))
            return true;

        // Preserve the previous behavior where only the root segment was case-insensitive.
        var separator = directory.IndexOf(Path.AltDirectorySeparatorChar);
        var rootName = separator < 0 ? directory : directory[..separator];
        var root = Folders.FirstOrDefault(x => x.Header.Equals(rootName, StringComparison.OrdinalIgnoreCase));
        if (root == null)
            return false;

        if (separator < 0)
        {
            folder = root;
            return true;
        }

        return _foldersByPath.TryGetValue(string.Concat(root.Header, directory[separator..]), out folder);
    }

    public void BulkPopulate(IReadOnlyCollection<GameFile> entries)
    {
        if (entries == null || entries.Count == 0)
            return;

        var treeItems = new List<TreeItem>();
        var foldersByPath = new Dictionary<string, TreeItem>(StringComparer.Ordinal);
        var folderLookup = foldersByPath.GetAlternateLookup<ReadOnlySpan<char>>();
        TreeItem previousFolder = null;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (var entry in entries)
        {
            var path = entry.Path.AsSpan();
            if (path.StartsWith(localAppData.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                path = path[localAppData.Length..];
                while (!path.IsEmpty &&
                       (path[0] == Path.DirectorySeparatorChar || path[0] == Path.AltDirectorySeparatorChar))
                    path = path[1..];
            }

            var pathEnd = path.Length;
            while (pathEnd > 0 && path[pathEnd - 1] == Path.AltDirectorySeparatorChar)
                pathEnd--;

            var lastSeparator = path[..pathEnd].LastIndexOf(Path.AltDirectorySeparatorChar);
            if (lastSeparator < 0)
            {
                previousFolder = GetOrAddContentFolder(foldersByPath, entry, treeItems);
                previousFolder.AssetsList.Add(entry);
                continue;
            }

            var directories = path[..lastSeparator];
            if (previousFolder != null && directories.SequenceEqual(previousFolder.PathAtThisPoint.AsSpan()))
            {
                previousFolder.AssetsList.Add(entry);
                continue;
            }

            if (folderLookup.TryGetValue(directories, out var leafFolder))
            {
                leafFolder.AssetsList.Add(entry);
                previousFolder = leafFolder;
                continue;
            }

            TreeItem parentNode = null;
            var segmentStart = 0;

            while (segmentStart < directories.Length)
            {
                while (segmentStart < directories.Length && directories[segmentStart] == Path.AltDirectorySeparatorChar)
                    segmentStart++;
                if (segmentStart == directories.Length)
                    break;

                var segmentEnd = directories[segmentStart..].IndexOf(Path.AltDirectorySeparatorChar);
                if (segmentEnd < 0)
                    segmentEnd = directories.Length;
                else
                    segmentEnd += segmentStart;

                var folderPath = directories[..segmentEnd];
                if (!folderLookup.TryGetValue(folderPath, out var node))
                {
                    var header = directories[segmentStart..segmentEnd].ToString();
                    var normalizedPath = parentNode == null
                        ? header
                        : string.Concat(parentNode.PathAtThisPoint, "/", header);
                    if (!foldersByPath.TryGetValue(normalizedPath, out node))
                    {
                        node = new TreeItem(header, entry, normalizedPath) { Parent = parentNode };
                        foldersByPath.Add(normalizedPath, node);

                        if (parentNode == null)
                            treeItems.Add(node);
                        else
                            parentNode.Folders.AddWithoutNotification(node);
                    }
                }

                parentNode = node;
                segmentStart = segmentEnd + 1;
            }

            if (parentNode == null)
                parentNode = GetOrAddContentFolder(foldersByPath, entry, treeItems);

            parentNode.AssetsList.Add(entry);
            previousFolder = parentNode;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            _foldersByPath = foldersByPath;
            Folders.AddRange(treeItems);

            if (treeItems.Count > 0)
            {
                // Select after publishing the collection. Selecting a detached TreeItem lets WPF
                // auto-select the first root (usually the synthetic "Content" bucket) instead.
                var projectName = ApplicationService.ApplicationView.CUE4Parse.Provider.ProjectName;
                (treeItems.FirstOrDefault(x => x.Header.Equals(projectName, StringComparison.OrdinalIgnoreCase)) ?? treeItems[0]).IsSelected = true;
            }

            ApplicationService.ApplicationView.CUE4Parse.SearchVm.ChangeCollection(entries);
        });
    }

    private static TreeItem GetOrAddContentFolder(Dictionary<string, TreeItem> foldersByPath, GameFile entry,
        List<TreeItem> roots)
    {
        const string content = "Content";
        if (foldersByPath.TryGetValue(content, out var node))
            return node;

        node = new TreeItem(content, entry, content);
        foldersByPath.Add(content, node);
        roots.Add(node);
        return node;
    }
}
