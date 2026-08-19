using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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

    private int _resultsCount;
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

    public RangeObservableCollection<GameFile> SearchResults { get; }
    private List<GameFile> _allEntries = new();
    private CancellationTokenSource _updateCts = new();

    public SearchViewModel()
    {
        SearchResults = new RangeObservableCollection<GameFile>();
    }

    public void ChangeCollection(IEnumerable<GameFile> files, GameFile refFile = null)
    {
        _allEntries = files?.ToList() ?? new List<GameFile>();
        RefFile = refFile;
        _ = UpdateResultsAsync();
    }

    public async Task RefreshFilter()
    {
        await UpdateResultsAsync();
    }

    public async Task CycleSortSizeMode()
    {
        CurrentSortSizeMode = CurrentSortSizeMode switch
        {
            ESortSizeMode.None => ESortSizeMode.Descending,
            ESortSizeMode.Descending => ESortSizeMode.Ascending,
            _ => ESortSizeMode.None
        };
        await UpdateResultsAsync();
    }

    private async Task UpdateResultsAsync()
    {
        _updateCts.Cancel();
        var cts = new CancellationTokenSource();
        _updateCts = cts;
        var token = cts.Token;

        string filterText = FilterText;
        bool regex = HasRegexEnabled;
        bool matchCase = HasMatchCaseEnabled;
        ESortSizeMode sortMode = CurrentSortSizeMode;
        List<GameFile> allEntries = _allEntries;

        try
        {
            var filtered = await Task.Run(() =>
            {
                return FilterAndSort(allEntries, filterText, regex, matchCase, sortMode, token);
            }, token).ConfigureAwait(false);

            if (cts != _updateCts)
                return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SearchResults.Clear();
                SearchResults.AddRange(filtered);
                ResultsCount = SearchResults.Count;
            });
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }

    private static List<GameFile> FilterAndSort(
        List<GameFile> entries,
        string filterText,
        bool regex,
        bool matchCase,
        ESortSizeMode sortMode,
        CancellationToken token)
    {
        if (entries.Count == 0)
            return new List<GameFile>();

        IEnumerable<GameFile> filtered = entries;
        if (!string.IsNullOrWhiteSpace(filterText))
        {
            if (regex)
            {
                var options = RegexOptions.None;
                if (!matchCase) options |= RegexOptions.IgnoreCase;
                var regexObj = new Regex(filterText, options | RegexOptions.Compiled);
                filtered = entries.Where(f => regexObj.IsMatch(f.Path));
            }
            else
            {
                var filters = filterText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                filtered = entries.Where(f => filters.All(x => f.Path.Contains(x, comparison)));
            }
        }

        var sortedList = filtered.ToList();
        if (token.IsCancellationRequested)
            return sortedList;

        var archiveDict = sortedList
            .OfType<VfsEntry>()
            .Select(f => f.Vfs.Name)
            .Distinct()
            .Select((name, idx) => (name, idx))
            .ToDictionary(x => x.name, x => x.idx);

        int GetArchiveIndex(GameFile f) =>
            f is VfsEntry ve && archiveDict.TryGetValue(ve.Vfs.Name, out var idx) ? idx : -1;

        switch (sortMode)
        {
            case ESortSizeMode.Ascending:
                sortedList = sortedList
                    .OrderBy(f => f.Size)
                    .ThenBy(f => GetArchiveIndex(f))
                    .ToList();
                break;
            case ESortSizeMode.Descending:
                sortedList = sortedList
                    .OrderByDescending(f => f.Size)
                    .ThenBy(f => GetArchiveIndex(f))
                    .ToList();
                break;
            default:
                sortedList = sortedList
                    .OrderBy(f => GetArchiveIndex(f))
                    .ThenBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                break;
        }

        return sortedList;
    }
}
