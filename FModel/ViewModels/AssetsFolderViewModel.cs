using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Vfs;
using FModel.Framework;
using FModel.Services;

namespace FModel.ViewModels;

public class TreeItem : ViewModel
{
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
        set => SetProperty(ref _isSelected, value);
    }

    private string _package;
    public string Package
    {
        get => _package;
        private set => SetProperty(ref _package, value);
    }

    private string _mountPoint;
    public string MountPoint
    {
        get => _mountPoint;
        private set => SetProperty(ref _mountPoint, value);
    }

    private int _version;
    public int Version
    {
        get => _version;
        private set => SetProperty(ref _version, value);
    }

    public string PathAtThisPoint { get; }
    public AssetsListViewModel AssetsList { get; }
    public RangeObservableCollection<TreeItem> Folders { get; }
    public ICollectionView FoldersView { get; }

    public TreeItem(string header, string package, string mountPoint, int version, string pathHere)
    {
        Header = header;
        Package = package;
        MountPoint = mountPoint;
        Version = version;
        PathAtThisPoint = pathHere;
        AssetsList = new AssetsListViewModel();
        Folders = new RangeObservableCollection<TreeItem>();
        FoldersView = new ListCollectionView(Folders) { SortDescriptions = { new SortDescription("Header", ListSortDirection.Ascending) } };
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

    public void BulkPopulate(IReadOnlyCollection<VfsEntry> entries)
    {
        if (entries == null || entries.Count == 0)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var treeItems = new RangeObservableCollection<TreeItem>();
            treeItems.SetSuppressionState(true);
            var items = new List<AssetItem>(entries.Count);

            foreach (var entry in entries)
            {
                var item = new AssetItem(entry.Path, entry.IsEncrypted, entry.Offset, entry.Size, entry.Vfs.Name, entry.CompressionMethod);
                items.Add(item);

                {
                    TreeItem lastNode = null;
                    var folders = item.FullPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
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
                            lastNode = new TreeItem(folder, item.Package, entry.Vfs.MountPoint, entry.Vfs.Ver.Value, nodePath[..^1]);
                            lastNode.Folders.SetSuppressionState(true);
                            lastNode.AssetsList.Assets.SetSuppressionState(true);
                            parentNode.Add(lastNode);
                        }

                        parentNode = lastNode.Folders;
                    }

                    lastNode?.AssetsList.Assets.Add(item);
                }
            }

            Folders.AddRange(treeItems);
            ApplicationService.ApplicationView.CUE4Parse.SearchVm.SearchResults.AddRange(items);

            foreach (var folder in Folders)
                InvokeOnCollectionChanged(folder);

            static void InvokeOnCollectionChanged(TreeItem item)
            {
                item.Folders.SetSuppressionState(false);
                item.AssetsList.Assets.SetSuppressionState(false);

                if (item.Folders.Count != 0)
                {
                    item.Folders.SetSuppressionState(false);
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