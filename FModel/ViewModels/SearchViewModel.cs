using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using FModel.Framework;

namespace FModel.ViewModels
{
    public class SearchViewModel : ViewModel
    {
        private string _filterText;
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

        public int ResultsCount => SearchResults?.Count ?? 0;
        public RangeObservableCollection<AssetItem> SearchResults { get; }
        public ICollectionView SearchResultsView { get; }

        public SearchViewModel()
        {
            SearchResults = new RangeObservableCollection<AssetItem>();
            SearchResultsView = new ListCollectionView(SearchResults);
        }

        public void RefreshFilter()
        {
            if (SearchResultsView.Filter == null)
                SearchResultsView.Filter = e => ItemFilter(e, FilterText.Trim().Split(' '));
            else
                SearchResultsView.Refresh();
        }

        private bool ItemFilter(object item, IEnumerable<string> filters)
        {
            if (item is not AssetItem assetItem)
                return true;

            if (!HasRegexEnabled)
                return filters.All(x => assetItem.FullPath.IndexOf(x,
                    HasMatchCaseEnabled ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0);

            var o = RegexOptions.None;
            if (HasMatchCaseEnabled) o |= RegexOptions.IgnoreCase;
            return new Regex(FilterText, o).Match(assetItem.FullPath).Success;
        }
    }
}