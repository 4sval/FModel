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
using Serilog;

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
        set
        {
            if (SetProperty(ref _filterText, value))
                CancelPendingUpdate();
        }
    }

    private bool _hasRegexEnabled;
    public bool HasRegexEnabled
    {
        get => _hasRegexEnabled;
        set
        {
            if (SetProperty(ref _hasRegexEnabled, value))
                CancelPendingUpdate();
        }
    }

    private bool _hasMatchCaseEnabled;
    public bool HasMatchCaseEnabled
    {
        get => _hasMatchCaseEnabled;
        set
        {
            if (SetProperty(ref _hasMatchCaseEnabled, value))
                CancelPendingUpdate();
        }
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
    private List<GameFile> _allEntries = [];
    private List<GameFile> _sortedEntries;
    private ESortSizeMode _sortedSizeMode;
    private CancellationTokenSource _updateCts;
    private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromMilliseconds(250);

    private string _filterError;
    public string FilterError
    {
        get => _filterError;
        private set => SetProperty(ref _filterError, value);
    }

    public SearchViewModel()
    {
        SearchResults = new RangeObservableCollection<GameFile>();
    }

    public void ChangeCollection(IEnumerable<GameFile> files, GameFile refFile = null)
    {
        Clear();
        _allEntries = files?.ToList() ?? [];
        RefFile = refFile;
        _ = UpdateResultsAsync();
    }

    public void Clear()
    {
        CancelPendingUpdate();
        _allEntries = [];
        _sortedEntries = null;
        RefFile = null;
        FilterError = null;
        SearchResults.ReplaceRange(Array.Empty<GameFile>());
        ResultsCount = 0;
    }

    private void CancelPendingUpdate()
    {
        var cts = _updateCts;
        _updateCts = null;
        cts?.Cancel();
        cts?.Dispose();
    }

    public Task RefreshFilter() => UpdateResultsAsync();

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
        CancelPendingUpdate();
        var cts = new CancellationTokenSource();
        _updateCts = cts;
        var token = cts.Token;

        string filterText = FilterText;
        bool regex = HasRegexEnabled;
        bool matchCase = HasMatchCaseEnabled;
        ESortSizeMode sortMode = CurrentSortSizeMode;
        List<GameFile> allEntries = _allEntries;
        List<GameFile> sortedEntries = _sortedSizeMode == sortMode ? _sortedEntries : null;

        try
        {
            var result = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var pattern = regex && !string.IsNullOrWhiteSpace(filterText)
                    ? new Regex(filterText, RegexOptions.Compiled | (matchCase ? RegexOptions.None : RegexOptions.IgnoreCase), RegexMatchTimeout)
                    : null;
                sortedEntries ??= SortEntries(allEntries, sortMode, token);
                return FilterEntries(sortedEntries, filterText, pattern, matchCase, token);
            }, token).ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (cts != _updateCts || token.IsCancellationRequested)
                    return;

                _updateCts = null;
                cts.Dispose();
                _sortedEntries = sortedEntries;
                _sortedSizeMode = sortMode;
                FilterError = null;
                SearchResults.ReplaceRange(result);
                ResultsCount = SearchResults.Count;
            });
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (ArgumentException e)
        {
            await SetFilterErrorAsync(cts, $"Invalid regular expression: {e.Message}");
        }
        catch (RegexMatchTimeoutException)
        {
            await SetFilterErrorAsync(cts, "The regular expression took too long. Try a simpler pattern.");
        }
        catch (Exception e)
        {
            Log.Error(e, "Could not update package search results");
            await SetFilterErrorAsync(cts, "Package search failed. See the log for details.");
        }
    }

    private Task SetFilterErrorAsync(CancellationTokenSource cts, string message) =>
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (cts != _updateCts)
                return;

            _updateCts = null;
            cts.Dispose();
            FilterError = message;
        }).Task;

    private static List<GameFile> FilterEntries(
        List<GameFile> entries,
        string filterText,
        Regex pattern,
        bool matchCase,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(filterText))
            return entries;

        var filters = pattern == null ? filterText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries) : [];
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var results = new List<GameFile>();
        foreach (var entry in entries)
        {
            token.ThrowIfCancellationRequested();
            if (pattern != null ? pattern.IsMatch(entry.Path) : filters.All(x => entry.Path.Contains(x, comparison)))
                results.Add(entry);
        }
        token.ThrowIfCancellationRequested();
        return results;
    }

    private static List<GameFile> SortEntries(List<GameFile> entries, ESortSizeMode sortMode, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var archives = new Dictionary<string, int>();
        var keyed = new (GameFile File, int ArchiveIndex, int OriginalIndex)[entries.Count];
        for (var i = 0; i < entries.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            var file = entries[i];
            var archiveIndex = -1;
            if (file is VfsEntry entry && !archives.TryGetValue(entry.Vfs.Name, out archiveIndex))
            {
                archiveIndex = archives.Count;
                archives.Add(entry.Vfs.Name, archiveIndex);
            }
            keyed[i] = (file, archiveIndex, i);
        }

        try
        {
            Array.Sort(keyed, (left, right) =>
            {
                token.ThrowIfCancellationRequested();
                var comparison = sortMode switch
                {
                    ESortSizeMode.Ascending => left.File.Size.CompareTo(right.File.Size),
                    ESortSizeMode.Descending => right.File.Size.CompareTo(left.File.Size),
                    _ => 0
                };
                if (comparison == 0)
                    comparison = left.ArchiveIndex.CompareTo(right.ArchiveIndex);
                if (comparison == 0 && sortMode == ESortSizeMode.None)
                    comparison = StringComparer.OrdinalIgnoreCase.Compare(left.File.Path, right.File.Path);
                return comparison == 0 ? left.OriginalIndex.CompareTo(right.OriginalIndex) : comparison;
            });
        }
        catch (InvalidOperationException) when (token.IsCancellationRequested)
        {
            token.ThrowIfCancellationRequested();
            throw;
        }

        var sorted = new List<GameFile>(keyed.Length);
        foreach (var item in keyed)
        {
            token.ThrowIfCancellationRequested();
            sorted.Add(item.File);
        }
        return sorted;
    }
}
