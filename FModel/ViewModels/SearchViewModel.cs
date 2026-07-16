using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.VirtualFileSystem;
using FModel.Framework;

namespace FModel.ViewModels;

public class SearchViewModel : ViewModel
{
    public enum ESortSizeMode
    {
        None,
        Ascending,
        Descending
    }

    private string _filterText = string.Empty;
    public string FilterText
    {
        get => _filterText;
        set => SetProperty(ref _filterText, value);
    }

    private bool _hasRegexEnabled;
    public bool HasRegexEnabled
    {
        get => _hasRegexEnabled;
        set => SetProperty(ref _hasRegexEnabled, value);
    }

    private bool _hasMatchCaseEnabled;
    public bool HasMatchCaseEnabled
    {
        get => _hasMatchCaseEnabled;
        set => SetProperty(ref _hasMatchCaseEnabled, value);
    }

    private ESortSizeMode _currentSortSizeMode = ESortSizeMode.None;
    public ESortSizeMode CurrentSortSizeMode
    {
        get => _currentSortSizeMode;
        set => SetProperty(ref _currentSortSizeMode, value);
    }

    private int _resultsCount = 0;
    public int ResultsCount
    {
        get => _resultsCount;
        private set => SetProperty(ref _resultsCount, value);
    }

    private GameFile _refFile;
    public GameFile RefFile
    {
        get => _refFile;
        private set => SetProperty(ref _refFile, value);
    }

    private List<GameFile> _searchResults = [];
    public List<GameFile> SearchResults
    {
        get => _searchResults;
        private set => SetProperty(ref _searchResults, value);
    }
    private ListCollectionView _searchResultsView;
    private string[] _filters = [];
    private Regex _filterRegex;
    private bool _isRegexValid = true;

    public ListCollectionView SearchResultsView
    {
        get
        {
            if (_searchResultsView != null)
                return _searchResultsView;

            PrepareFilter();
            _searchResultsView = new ListCollectionView(SearchResults)
            {
                Filter = ItemFilter,
            };
            ResultsCount = _searchResultsView.Count;
            return _searchResultsView;
        }
    }

    public SearchViewModel()
    {
        ResultsCount = 0;
    }

    public void RefreshFilter()
    {
        PrepareFilter();
        SearchResultsView.Refresh();
        ResultsCount = SearchResultsView.Count;
    }

    public void ChangeCollection(IEnumerable<GameFile> files, GameFile refFile = null)
    {
        var results = files as List<GameFile> ?? files.ToList();
        _searchResultsView = null;
        SearchResults = results;
        RaisePropertyChanged(nameof(SearchResultsView));
        RefFile = refFile;
        ResultsCount = results.Count;
    }

    public void Clear() => ChangeCollection([]);

    public async Task CycleSortSizeMode()
    {
        CurrentSortSizeMode = CurrentSortSizeMode switch
        {
            ESortSizeMode.None => ESortSizeMode.Descending,
            ESortSizeMode.Descending => ESortSizeMode.Ascending,
            _ => ESortSizeMode.None
        };

        var sorted = await Task.Run(() =>
        {
            var archiveDict = SearchResults
                .OfType<VfsEntry>()
                .Select(f => f.Vfs.Name)
                .Distinct()
                .Select((name, idx) => (name, idx))
                .ToDictionary(x => x.name, x => x.idx);

            var keyed = SearchResults.Select(f =>
            {
                int archiveKey = f is VfsEntry ve && archiveDict.TryGetValue(ve.Vfs.Name, out var key) ? key : -1;
                return (File: f, f.Size, ArchiveKey: archiveKey);
            });

            return CurrentSortSizeMode switch
            {
                ESortSizeMode.Ascending => keyed
                    .OrderBy(x => x.Size).ThenBy(x => x.ArchiveKey)
                    .Select(x => x.File).ToList(),
                ESortSizeMode.Descending => keyed
                    .OrderByDescending(x => x.Size).ThenBy(x => x.ArchiveKey)
                    .Select(x => x.File).ToList(),
                _ => keyed
                    .OrderBy(x => x.ArchiveKey).ThenBy(x => x.File.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.File).ToList()
            };
        });

        ChangeCollection(sorted, RefFile);
    }

    private void PrepareFilter()
    {
        _filters = FilterText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        _filterRegex = null;
        _isRegexValid = true;

        if (!HasRegexEnabled)
            return;

        var options = RegexOptions.Compiled;
        if (!HasMatchCaseEnabled)
            options |= RegexOptions.IgnoreCase;

        try
        {
            _filterRegex = new Regex(FilterText, options);
        }
        catch (ArgumentException)
        {
            _isRegexValid = false;
        }
    }

    private bool ItemFilter(object item)
    {
        if (item is not GameFile entry)
            return true;

        if (!HasRegexEnabled)
            return _filters.All(x => entry.Path.Contains(x,
                HasMatchCaseEnabled ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));

        return _isRegexValid && _filterRegex.IsMatch(entry.Path);
    }
}
