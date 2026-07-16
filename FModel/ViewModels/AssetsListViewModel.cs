using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using CUE4Parse.FileProvider.Objects;
using FModel.Framework;

namespace FModel.ViewModels;

public class AssetsListViewModel
{
    private List<GameFile> _pendingAssets;
    private RangeObservableCollection<GameFileViewModel> _assets;
    public RangeObservableCollection<GameFileViewModel> Assets
    {
        get
        {
            if (_assets != null)
                return _assets;

            _assets = [];
            if (_pendingAssets == null)
                return _assets;

            foreach (var asset in _pendingAssets)
                _assets.AddWithoutNotification(new GameFileViewModel(asset));

            _pendingAssets = null;
            return _assets;
        }
    }

    public int Count => _assets?.Count ?? _pendingAssets?.Count ?? 0;

    private ICollectionView _assetsView;
    private Predicate<object> _filter;
    public ICollectionView AssetsView
    {
        get
        {
            _assetsView ??= new ListCollectionView(Assets)
            {
                SortDescriptions = { new SortDescription("Asset.Path", ListSortDirection.Ascending) },
                Filter = _filter
            };
            return _assetsView;
        }
    }

    public void Add(GameFile gameFile)
    {
        if (_assets == null)
            (_pendingAssets ??= []).Add(gameFile);
        else
            _assets.Add(new GameFileViewModel(gameFile));
    }

    public void SetFilter(Predicate<object> filter)
    {
        _filter = filter;
        if (_assetsView != null)
            _assetsView.Filter = filter;
    }

    public void RefreshView() => _assetsView?.Refresh();
}
