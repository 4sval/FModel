using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using FModel.Framework;

namespace FModel.ViewModels;

public class SearchViewModelProp : ViewModel
{
    private string _filterText;
    public string FilterText
    {
        get => _filterText;
        set => SetProperty(ref _filterText, value);
    }

    private List<string> assets { get; set; } = new List<string>();

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

    public int ResultsCount => SearchResults?.Count ?? 0;
    public RangeObservableCollection<AssetItem> SearchResults { get; }
    public ICollectionView SearchResultsView { get; }

    public SearchViewModelProp()
    {
        SearchResults = new RangeObservableCollection<AssetItem>();
        SearchResultsView = new ListCollectionView(SearchResults);
    }

    public void CheckProp(string text, string assetPath)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (!HasRegexEnabled)
        {
            if (text.Contains(FilterText, HasMatchCaseEnabled ? StringComparison.Ordinal: StringComparison.OrdinalIgnoreCase))
            {
                assets.Add(assetPath);

                //RefreshFilter();
            }

            return;
        }

        RegexOptions o = RegexOptions.None;

        if (!HasMatchCaseEnabled) { o |= RegexOptions.IgnoreCase; }

        if (new Regex(FilterText, o).Match(text).Success)
        {
            assets.Add(assetPath);

            RefreshFilter();
        }
    }

    public void RefreshFilter()
    {
        if (SearchResultsView.Filter == null)
            SearchResultsView.Filter = e => ItemFilter(e, assets);
        else
            SearchResultsView.Refresh();
    }

    private bool ItemFilter(object item, IEnumerable<string> filters)
    {
        if (item is not AssetItem assetItem)
            return true;

        return filters.Any(x => assetItem.FullPath == x);
    }
}
